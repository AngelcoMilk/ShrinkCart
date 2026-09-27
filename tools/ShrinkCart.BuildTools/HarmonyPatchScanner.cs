using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace ShrinkCart.BuildTools
{
    /// <summary>一个 Harmony patch 类的文档信息（docs/INTERFACES.md 的 Hook 表）。</summary>
    internal sealed class PatchInfo
    {
        internal string PatchClass;
        internal string Target;
        internal string Kinds;
        internal string Feature;
        internal int FeatureOrder;
        internal string Frequency;
        internal string Side;
    }

    /// <summary>
    /// 校验 ShrinkCart 中的 Harmony patch 类（按 Harmony 自身的解析规则，在原版 Assembly-CSharp 中）：
    /// <list type="bullet">
    /// <item>patch 类只能位于 ShrinkCart.Patches，且必须带 [PatchFeature]（含 Frequency 与 Side）。</item>
    /// <item>目标方法存在且唯一（有重载时必须在 [HarmonyPatch] 中写明参数类型）。</item>
    /// <item>补丁方法的参数能被 Harmony 注入：普通参数按名称对应目标参数（游戏改了参数名会在这里暴露）、
    /// __instance / __result / ___field / __N 与目标一致、__state 在 Prefix 中是 out/ref 且前后类型一致。</item>
    /// </list>
    /// </summary>
    internal static class HarmonyPatchScanner
    {
        private const string HarmonyPatchAttribute = "HarmonyLib.HarmonyPatch";
        private const string PatchFeatureAttribute = "ShrinkCart.Core.PatchFeatureAttribute";
        private const string PatchNamespace = "ShrinkCart.Patches";

        private const int MethodTypeNormal = 0;
        private const int MethodTypeGetter = 1;
        private const int MethodTypeSetter = 2;
        private const int MethodTypeConstructor = 3;
        private const int MethodTypeStaticConstructor = 4;

        private static readonly string[] KindOrder = { "Prefix", "Postfix", "Finalizer", "Transpiler" };

        private static readonly Dictionary<string, string> KindAttributes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "HarmonyLib.HarmonyPrefix", "Prefix" },
            { "HarmonyLib.HarmonyPostfix", "Postfix" },
            { "HarmonyLib.HarmonyFinalizer", "Finalizer" },
            { "HarmonyLib.HarmonyTranspiler", "Transpiler" }
        };

        private static readonly HashSet<string> PassThroughInjections = new HashSet<string>(StringComparer.Ordinal)
        {
            "__state", "__exception", "__originalMethod", "__args", "__runOriginal"
        };

        private static readonly Regex PositionalInjection = new Regex(@"^__(\d+)$", RegexOptions.CultureInvariant);

        internal static List<PatchInfo> Scan(AssemblyModel model, Report report)
        {
            List<PatchInfo> patches = new List<PatchInfo>();
            int failuresBefore = report.FailureCount;
            foreach (TypeDefinition type in model.Mod.GetTypes())
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method.CustomAttributes.Any(a => a.AttributeType.FullName == HarmonyPatchAttribute))
                    {
                        report.Fail(type.FullName + "." + method.Name + ": method-level [HarmonyPatch] is not supported; declare the target on the patch class.");
                    }
                }

                List<CustomAttribute> harmony = type.CustomAttributes.Where(a => a.AttributeType.FullName == HarmonyPatchAttribute).ToList();
                bool inPatchNamespace = AssemblyModel.Outermost(type).Namespace == PatchNamespace;
                if (harmony.Count == 0)
                {
                    if (inPatchNamespace && type.DeclaringType == null && !IsCompilerGenerated(type))
                    {
                        report.Fail(type.FullName + " has no [HarmonyPatch]; ShrinkCart.Patches only holds patch classes.");
                    }

                    continue;
                }

                if (!inPatchNamespace || type.DeclaringType != null)
                {
                    report.Fail(type.FullName + ": Harmony patch classes must be top-level classes in namespace " + PatchNamespace + ".");
                }

                PatchInfo info = new PatchInfo { PatchClass = type.Name, Feature = "?", FeatureOrder = int.MaxValue };
                ReadPatchFeature(type, info, report);

                MethodDefinition target = ResolveTarget(model, type, harmony, report);
                if (target == null)
                {
                    continue;
                }

                info.Target = Describe(target);
                info.Kinds = CheckPatchMethods(type, target, report);
                patches.Add(info);
            }

            if (patches.Count == 0)
            {
                report.Fail("No Harmony patch classes found in " + model.Mod.Name + ".");
            }
            else if (report.FailureCount == failuresBefore)
            {
                report.Pass(patches.Count + " Harmony patch classes resolve to unique targets in the original " + AssemblyModel.GameAssemblyName + " with injectable parameters.");
            }

            return patches;
        }

        private static void ReadPatchFeature(TypeDefinition type, PatchInfo info, Report report)
        {
            CustomAttribute feature = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == PatchFeatureAttribute);
            if (feature == null || feature.ConstructorArguments.Count == 0)
            {
                report.Fail(type.FullName + " has no [PatchFeature]; every patch class must name the feature it belongs to.");
                return;
            }

            CustomAttributeArgument owner = feature.ConstructorArguments[0];
            info.Feature = AssemblyModel.EnumName(owner.Type, owner.Value);
            info.FeatureOrder = Convert.ToInt32(owner.Value, System.Globalization.CultureInfo.InvariantCulture);
            info.Frequency = NamedString(feature, "Frequency");
            info.Side = NamedString(feature, "Side");
            if (string.IsNullOrWhiteSpace(info.Frequency) || string.IsNullOrWhiteSpace(info.Side))
            {
                report.Fail(type.FullName + ": [PatchFeature] must set Frequency and Side (documented call frequency and executing side).");
            }
        }

        private static string NamedString(CustomAttribute attribute, string name)
        {
            foreach (CustomAttributeNamedArgument property in attribute.Properties)
            {
                if (property.Name == name)
                {
                    return property.Argument.Value as string;
                }
            }

            return null;
        }

        private static MethodDefinition ResolveTarget(AssemblyModel model, TypeDefinition patchClass, List<CustomAttribute> attributes, Report report)
        {
            TypeReference declaringReference = null;
            string typeName = null;
            string methodName = null;
            int methodType = MethodTypeNormal;
            List<TypeReference> argumentTypes = null;
            List<int> variations = null;

            foreach (CustomAttribute attribute in attributes)
            {
                List<string> strings = new List<string>();
                foreach (CustomAttributeArgument argument in attribute.ConstructorArguments)
                {
                    string argumentType = argument.Type.FullName;
                    if (argumentType == "System.Type")
                    {
                        declaringReference = (TypeReference)argument.Value;
                    }
                    else if (argumentType == "System.String")
                    {
                        strings.Add((string)argument.Value);
                    }
                    else if (argumentType == "System.Type[]")
                    {
                        argumentTypes = ((CustomAttributeArgument[])argument.Value).Select(a => (TypeReference)a.Value).ToList();
                    }
                    else if (argumentType == "HarmonyLib.MethodType")
                    {
                        methodType = Convert.ToInt32(argument.Value, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else if (argumentType == "HarmonyLib.ArgumentType[]")
                    {
                        variations = ((CustomAttributeArgument[])argument.Value)
                            .Select(a => Convert.ToInt32(a.Value, System.Globalization.CultureInfo.InvariantCulture))
                            .ToList();
                    }
                    else
                    {
                        report.Fail(patchClass.FullName + ": unsupported [HarmonyPatch] argument of type " + argumentType + " (extend BuildTools if intentional).");
                        return null;
                    }
                }

                if (strings.Count == 2)
                {
                    typeName = strings[0];
                    methodName = strings[1];
                }
                else if (strings.Count == 1)
                {
                    methodName = strings[0];
                }
            }

            TypeDefinition declaring = null;
            if (declaringReference != null)
            {
                declaring = AssemblyModel.Resolve(declaringReference);
            }
            else if (typeName != null)
            {
                declaring = model.Game.GetType(typeName);
            }

            if (declaring == null)
            {
                report.Fail(patchClass.FullName + ": [HarmonyPatch] target type " + (declaringReference != null ? declaringReference.FullName : typeName ?? "(none)") + " not found.");
                return null;
            }

            List<MethodDefinition> candidates;
            string wanted;
            switch (methodType)
            {
                case MethodTypeNormal:
                    if (string.IsNullOrEmpty(methodName))
                    {
                        report.Fail(patchClass.FullName + ": [HarmonyPatch] has no method name.");
                        return null;
                    }

                    // Harmony 使用 AccessTools.DeclaredMethod：只在声明类型本身查找，不含基类。
                    candidates = declaring.Methods.Where(m => m.Name == methodName && !m.IsConstructor).ToList();
                    wanted = methodName;
                    break;
                case MethodTypeGetter:
                case MethodTypeSetter:
                    PropertyDefinition property = declaring.Properties.FirstOrDefault(p => p.Name == methodName);
                    MethodDefinition accessor = property == null ? null : methodType == MethodTypeGetter ? property.GetMethod : property.SetMethod;
                    candidates = accessor == null ? new List<MethodDefinition>() : new List<MethodDefinition> { accessor };
                    wanted = (methodType == MethodTypeGetter ? "get_" : "set_") + methodName;
                    break;
                case MethodTypeConstructor:
                    candidates = declaring.Methods.Where(m => m.IsConstructor && !m.IsStatic).ToList();
                    wanted = ".ctor";
                    break;
                case MethodTypeStaticConstructor:
                    candidates = declaring.Methods.Where(m => m.IsConstructor && m.IsStatic).ToList();
                    wanted = ".cctor";
                    break;
                default:
                    report.Fail(patchClass.FullName + ": MethodType " + methodType + " is not supported by BuildTools (extend it if intentional).");
                    return null;
            }

            if (argumentTypes != null)
            {
                candidates = candidates.Where(m => ParametersMatch(m, argumentTypes, variations)).ToList();
            }

            string signature = AssemblyModel.Display(declaring) + "." + wanted +
                               (argumentTypes != null ? "(" + string.Join(", ", argumentTypes.Select(AssemblyModel.Display)) + ")" : string.Empty);
            if (candidates.Count == 0)
            {
                report.Fail(patchClass.FullName + ": patch target " + signature + " not found in the original " + declaring.Module.Assembly.Name.Name + ".");
                return null;
            }

            if (candidates.Count > 1)
            {
                report.Fail(patchClass.FullName + ": patch target " + signature + " is ambiguous (" + candidates.Count + " overloads); add argument types to [HarmonyPatch].");
                return null;
            }

            return candidates[0];
        }

        private static bool ParametersMatch(MethodDefinition method, List<TypeReference> argumentTypes, List<int> variations)
        {
            if (method.Parameters.Count != argumentTypes.Count)
            {
                return false;
            }

            for (int i = 0; i < argumentTypes.Count; i++)
            {
                string expected = argumentTypes[i].FullName;
                int variation = variations != null && i < variations.Count ? variations[i] : 0;
                if (variation == 1 || variation == 2)
                {
                    expected += "&";
                }
                else if (variation == 3)
                {
                    expected += "*";
                }

                if (method.Parameters[i].ParameterType.FullName != expected)
                {
                    return false;
                }
            }

            return true;
        }

        private static string CheckPatchMethods(TypeDefinition patchClass, MethodDefinition target, Report report)
        {
            Dictionary<string, MethodDefinition> byKind = new Dictionary<string, MethodDefinition>(StringComparer.Ordinal);
            foreach (MethodDefinition method in patchClass.Methods)
            {
                string kind = KindOf(method);
                if (kind == null)
                {
                    continue;
                }

                if (!method.IsStatic)
                {
                    report.Fail(Location(patchClass, method) + ": Harmony patch methods must be static.");
                }

                if (byKind.ContainsKey(kind))
                {
                    report.Fail(Location(patchClass, method) + ": more than one " + kind + " in one patch class.");
                    continue;
                }

                byKind[kind] = method;
            }

            if (byKind.Count == 0)
            {
                report.Fail(patchClass.FullName + " declares no Prefix/Postfix/Finalizer/Transpiler.");
                return string.Empty;
            }

            byKind.TryGetValue("Prefix", out MethodDefinition prefix);
            byKind.TryGetValue("Postfix", out MethodDefinition postfix);
            if (prefix != null && prefix.ReturnType.FullName != "System.Void" && prefix.ReturnType.FullName != "System.Boolean")
            {
                report.Fail(Location(patchClass, prefix) + ": Prefix must return void or bool.");
            }

            if (postfix != null && postfix.ReturnType.FullName != "System.Void")
            {
                report.Fail(Location(patchClass, postfix) + ": pass-through Postfix is not supported by BuildTools (extend it if intentional).");
            }

            foreach (KeyValuePair<string, MethodDefinition> pair in byKind)
            {
                if (pair.Key != "Transpiler")
                {
                    CheckParameters(patchClass, pair.Key, pair.Value, target, report);
                }
            }

            CheckState(patchClass, byKind, report);
            return string.Join(" + ", KindOrder.Where(byKind.ContainsKey));
        }

        private static void CheckParameters(TypeDefinition patchClass, string kind, MethodDefinition method, MethodDefinition target, Report report)
        {
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                string name = parameter.Name;
                TypeReference type = StripReference(parameter.ParameterType);
                string where = Location(patchClass, method) + " parameter '" + name + "'";

                if (name == "__instance")
                {
                    if (target.IsStatic)
                    {
                        report.Fail(where + ": target " + Describe(target) + " is static.");
                    }
                    else if (!IsAssignableFrom(type, target.DeclaringType))
                    {
                        report.Fail(where + ": type " + AssemblyModel.Display(type) + " does not match " + AssemblyModel.Display(target.DeclaringType) + ".");
                    }
                }
                else if (name == "__result")
                {
                    if (target.ReturnType.FullName == "System.Void")
                    {
                        report.Fail(where + ": target " + Describe(target) + " returns void.");
                    }
                    else if (!SameOrObject(type, StripReference(target.ReturnType)))
                    {
                        report.Fail(where + ": type " + AssemblyModel.Display(type) + " does not match return type " + AssemblyModel.Display(target.ReturnType) + ".");
                    }
                }
                else if (name.StartsWith("___", StringComparison.Ordinal))
                {
                    FieldDefinition field = FindField(target.DeclaringType, name.Substring(3));
                    if (field == null)
                    {
                        report.Fail(where + ": field " + name.Substring(3) + " not found on " + AssemblyModel.Display(target.DeclaringType) + ".");
                    }
                    else if (!SameOrObject(type, field.FieldType))
                    {
                        report.Fail(where + ": type " + AssemblyModel.Display(type) + " does not match field type " + AssemblyModel.Display(field.FieldType) + ".");
                    }
                }
                else if (PassThroughInjections.Contains(name))
                {
                    if (name == "__exception" && kind != "Finalizer" && kind != "Postfix")
                    {
                        report.Fail(where + ": __exception is only available to Postfix/Finalizer.");
                    }
                }
                else if (PositionalInjection.IsMatch(name))
                {
                    int index = int.Parse(PositionalInjection.Match(name).Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
                    if (index >= target.Parameters.Count)
                    {
                        report.Fail(where + ": target " + Describe(target) + " has only " + target.Parameters.Count + " parameters.");
                    }
                }
                else if (name.StartsWith("__", StringComparison.Ordinal))
                {
                    report.Fail(where + ": unknown Harmony injection name.");
                }
                else
                {
                    // Harmony 按名称绑定原方法参数：游戏更新改了参数名，patch 会在安装时失败。
                    ParameterDefinition original = target.Parameters.FirstOrDefault(p => p.Name == name);
                    if (original == null)
                    {
                        report.Fail(where + ": no parameter with this name on " + Describe(target) +
                                    " (parameters: " + string.Join(", ", target.Parameters.Select(p => p.Name)) + ").");
                    }
                    else if (!SameOrObject(type, StripReference(original.ParameterType)))
                    {
                        report.Fail(where + ": type " + AssemblyModel.Display(type) + " does not match " + AssemblyModel.Display(original.ParameterType) + ".");
                    }
                }
            }
        }

        private static void CheckState(TypeDefinition patchClass, Dictionary<string, MethodDefinition> byKind, Report report)
        {
            byKind.TryGetValue("Prefix", out MethodDefinition prefix);
            ParameterDefinition prefixState = prefix?.Parameters.FirstOrDefault(p => p.Name == "__state");
            if (prefixState != null && !prefixState.ParameterType.IsByReference)
            {
                report.Fail(Location(patchClass, prefix) + ": __state must be declared 'out' (or 'ref') in the Prefix.");
            }

            foreach (string kind in new[] { "Postfix", "Finalizer" })
            {
                if (!byKind.TryGetValue(kind, out MethodDefinition method))
                {
                    continue;
                }

                ParameterDefinition state = method.Parameters.FirstOrDefault(p => p.Name == "__state");
                if (state == null)
                {
                    continue;
                }

                if (prefixState == null)
                {
                    report.Fail(Location(patchClass, method) + ": __state is read but the Prefix does not declare it.");
                }
                else if (StripReference(state.ParameterType).FullName != StripReference(prefixState.ParameterType).FullName)
                {
                    report.Fail(Location(patchClass, method) + ": __state type differs from the Prefix.");
                }
            }
        }

        private static string KindOf(MethodDefinition method)
        {
            foreach (CustomAttribute attribute in method.CustomAttributes)
            {
                if (KindAttributes.TryGetValue(attribute.AttributeType.FullName, out string kind))
                {
                    return kind;
                }
            }

            return Array.IndexOf(KindOrder, method.Name) >= 0 ? method.Name : null;
        }

        private static bool IsAssignableFrom(TypeReference parameterType, TypeDefinition targetType)
        {
            if (parameterType.FullName == "System.Object")
            {
                return true;
            }

            for (TypeDefinition current = targetType; current != null; current = current.BaseType == null ? null : AssemblyModel.Resolve(current.BaseType))
            {
                if (current.FullName == parameterType.FullName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SameOrObject(TypeReference parameterType, TypeReference expected)
        {
            return parameterType.FullName == "System.Object" || parameterType.FullName == expected.FullName;
        }

        private static FieldDefinition FindField(TypeDefinition type, string name)
        {
            for (TypeDefinition current = type; current != null; current = current.BaseType == null ? null : AssemblyModel.Resolve(current.BaseType))
            {
                FieldDefinition field = current.Fields.FirstOrDefault(f => f.Name == name);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static TypeReference StripReference(TypeReference type)
        {
            return type is ByReferenceType byReference ? byReference.ElementType : type;
        }

        private static bool IsCompilerGenerated(TypeDefinition type)
        {
            return type.Name.StartsWith("<", StringComparison.Ordinal) ||
                   type.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        }

        private static string Location(TypeDefinition type, MethodDefinition method)
        {
            return type.Name + "." + method.Name;
        }

        internal static string Describe(MethodDefinition method)
        {
            return AssemblyModel.Display(method.DeclaringType) + "." + method.Name +
                   "(" + string.Join(", ", method.Parameters.Select(p => AssemblyModel.Display(p.ParameterType))) + ")";
        }
    }
}
