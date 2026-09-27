using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace ShrinkCart.BuildTools
{
    /// <summary>
    /// 分层依赖规则（从 IL 推导，见 docs/ARCHITECTURE.md「分层与依赖」）：
    /// <list type="bullet">
    /// <item>层次自上而下为 Patches → Bootstrap → Features → Scaling → Game → Config → Core → Domain，
    /// 依赖只能指向同层或更低层；新增命名空间必须先在这里登记层次。</item>
    /// <item>Domain：只允许 BCL（纯逻辑，可单元测试）。</item>
    /// <item>Config：外部只允许 BCL 与 BepInEx（不接触 Unity、游戏与 ScalerCore）。</item>
    /// <item>ScalerCore.ScaleManager：只允许 Scaling.ScalerCoreGateway 调用（统一异常处理与所有权记录）。</item>
    /// </list>
    /// </summary>
    internal static class LayerRules
    {
        private const string ModAssembly = "ShrinkCart";
        private const string GatewayType = "ShrinkCart.Scaling.ScalerCoreGateway";
        private const string ScaleManagerType = "ScalerCore.ScaleManager";

        /// <summary>自上而下的层次；根命名空间 ShrinkCart（BepInEx 入口 Plugin 与生成的 MyPluginInfo）属于 Bootstrap 层。</summary>
        internal static readonly string[] Layers =
        {
            "ShrinkCart.Patches",
            "ShrinkCart.Bootstrap",
            "ShrinkCart.Features",
            "ShrinkCart.Scaling",
            "ShrinkCart.Game",
            "ShrinkCart.Config",
            "ShrinkCart.Core",
            "ShrinkCart.Domain"
        };

        private static readonly Dictionary<string, string[]> AllowedExternalAssemblies = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "ShrinkCart.Domain", new string[0] },
            { "ShrinkCart.Config", new[] { "BepInEx" } }
        };

        internal static void Check(AssemblyModel model, Report report)
        {
            int failuresBefore = report.FailureCount;
            foreach (TypeDefinition type in model.Mod.GetTypes())
            {
                TypeDefinition outer = AssemblyModel.Outermost(type);
                if (IsCompilerSupport(outer.Namespace))
                {
                    continue;
                }

                int layer = LayerOf(outer.Namespace);
                if (layer < 0)
                {
                    if (type == outer)
                    {
                        report.Fail(type.FullName + " is in namespace '" + outer.Namespace + "', which is not assigned to a layer (BuildTools LayerRules.Layers).");
                    }

                    continue;
                }

                CheckReferences(type, outer.Namespace, layer, report);
                if (outer.FullName != GatewayType)
                {
                    CheckScaleManager(type, report);
                }
            }

            if (report.FailureCount == failuresBefore)
            {
                report.Pass(
                    "Layer rules hold: dependencies point downward (" + string.Join(" > ", Layers.Select(l => l.Substring(ModAssembly.Length + 1))) +
                    "), Domain uses only the BCL, Config stays off Unity/game/ScalerCore, only ScalerCoreGateway calls ScaleManager.");
            }
        }

        private static void CheckReferences(TypeDefinition type, string ownNamespace, int ownLayer, Report report)
        {
            AllowedExternalAssemblies.TryGetValue(ownNamespace, out string[] allowedExternal);
            HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
            void OnType(TypeReference reference)
            {
                string assembly = IlWalker.AssemblyOf(reference);
                string violation = null;
                if (assembly == ModAssembly)
                {
                    TypeDefinition definition = AssemblyModel.Resolve(reference);
                    string referencedNamespace = definition == null ? reference.Namespace : AssemblyModel.Outermost(definition).Namespace;
                    int referencedLayer = LayerOf(referencedNamespace);
                    if (!IsCompilerSupport(referencedNamespace) && referencedLayer >= 0 && referencedLayer < ownLayer)
                    {
                        violation = reference.FullName + " (upper layer " + referencedNamespace + ")";
                    }
                }
                else if (allowedExternal != null && !IsBcl(assembly) && !allowedExternal.Contains(assembly))
                {
                    violation = reference.FullName + " (" + assembly + ")";
                }

                if (violation != null && reported.Add(violation))
                {
                    report.Fail(type.FullName + " (layer " + ownNamespace + ") references " + violation + ".");
                }
            }

            IlWalker.WalkTypeSignature(type, OnType);
            foreach (MethodDefinition method in type.Methods)
            {
                IlWalker.WalkMethod(method, OnType, member => { });
            }
        }

        private static void CheckScaleManager(TypeDefinition type, Report report)
        {
            foreach (MethodDefinition method in type.Methods)
            {
                HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
                IlWalker.WalkMethod(
                    method,
                    reference => { },
                    member =>
                    {
                        if (member.DeclaringType.GetElementType().FullName == ScaleManagerType && reported.Add(member.Name))
                        {
                            report.Fail(type.FullName + "." + method.Name + " calls " + ScaleManagerType + "." + member.Name + "; only ScalerCoreGateway may call ScaleManager.");
                        }
                    });
            }
        }

        private static int LayerOf(string ns)
        {
            return ns == ModAssembly ? Array.IndexOf(Layers, "ShrinkCart.Bootstrap") : Array.IndexOf(Layers, ns);
        }

        private static bool IsBcl(string assembly)
        {
            return assembly == "netstandard" ||
                   assembly == "mscorlib" ||
                   assembly == "System" ||
                   assembly.StartsWith("System.", StringComparison.Ordinal);
        }

        // 编译器生成的类型（<PrivateImplementationDetails>、嵌入特性等）不属于任何层。
        private static bool IsCompilerSupport(string ns)
        {
            return ns.Length == 0 ||
                   ns.StartsWith("System", StringComparison.Ordinal) ||
                   ns.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal);
        }
    }
}
