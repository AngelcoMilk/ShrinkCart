using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace ShrinkCart.BuildTools
{
    /// <summary>一个非公开游戏成员的文档信息（docs/INTERFACES.md 的 GameMember 表）。</summary>
    internal sealed class GameMemberInfo
    {
        internal string Member;
        internal string Kind;
        internal string MemberType;
        internal string Visibility;
        internal readonly SortedSet<string> Accessors = new SortedSet<string>(StringComparer.Ordinal);
        internal readonly SortedDictionary<int, string> Features = new SortedDictionary<int, string>();

        internal int FeatureOrder => Features.Count == 0 ? int.MaxValue : Features.Keys.First();
    }

    /// <summary>
    /// 访问阶梯的构建期执行：
    /// <list type="bullet">
    /// <item>对原版 Assembly-CSharp 非公开成员（按原版而非 publicize 后的可见性判断）的 IL 引用只能出现在 GameAccess。</item>
    /// <item>GameAccess 每个方法的 [GameMember] 声明必须与 IL 实际引用的非公开成员完全一致，字段类型一致，且方法标记 NoInlining。</item>
    /// </list>
    /// </summary>
    internal static class GameMemberScanner
    {
        internal const string GameAccessTypeName = "ShrinkCart.Game.GameAccess";
        private const string GameMemberAttributeName = "ShrinkCart.Game.GameMemberAttribute";

        internal static List<GameMemberInfo> Scan(AssemblyModel model, Report report)
        {
            int failuresBefore = report.FailureCount;
            Dictionary<MethodDefinition, List<Access>> byMethod = new Dictionary<MethodDefinition, List<Access>>();
            foreach (TypeDefinition type in model.Mod.GetTypes())
            {
                bool inGameAccess = IsGameAccess(type);
                HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
                IlWalker.WalkTypeSignature(type, reference =>
                {
                    TypeDefinition gameType = model.ResolveGameType(reference);
                    if (!inGameAccess && gameType != null && !AssemblyModel.IsPublicApi(gameType) && reported.Add(gameType.FullName))
                    {
                        report.Fail(type.FullName + " uses non-public game type " + AssemblyModel.Display(gameType) + " in its signature.");
                    }
                });

                foreach (MethodDefinition method in type.Methods)
                {
                    List<Access> accesses = Collect(model, method, report);
                    if (accesses.Count > 0)
                    {
                        byMethod[method] = accesses;
                    }
                }
            }

            foreach (KeyValuePair<MethodDefinition, List<Access>> pair in byMethod)
            {
                if (IsGameAccess(pair.Key.DeclaringType))
                {
                    continue;
                }

                foreach (Access access in pair.Value.GroupBy(a => a.Key).Select(g => g.First()))
                {
                    report.Fail(
                        Where(pair.Key) + " accesses non-public game " + access.Kind + " " + access.Display +
                        "; move the access into GameAccess and declare it with [GameMember].");
                }
            }

            TypeDefinition gameAccess = model.Mod.GetType(GameAccessTypeName);
            if (gameAccess == null)
            {
                report.Fail(GameAccessTypeName + " not found in " + model.Mod.Name + ".");
                return new List<GameMemberInfo>();
            }

            Dictionary<string, GameMemberInfo> rows = new Dictionary<string, GameMemberInfo>(StringComparer.Ordinal);
            foreach (MethodDefinition method in gameAccess.Methods)
            {
                if (method.IsConstructor)
                {
                    continue;
                }

                CheckGameAccessMethod(model, method, byMethod, rows, report);
            }

            if (report.FailureCount == failuresBefore)
            {
                report.Pass(
                    "Non-public game members are accessed only in GameAccess; " + rows.Count +
                    " [GameMember] declarations match the IL of " + gameAccess.Methods.Count(m => !m.IsConstructor) + " accessor methods.");
            }

            return rows.Values
                .OrderBy(r => r.FeatureOrder)
                .ThenBy(r => r.Member, StringComparer.Ordinal)
                .ToList();
        }

        private static void CheckGameAccessMethod(
            AssemblyModel model,
            MethodDefinition method,
            Dictionary<MethodDefinition, List<Access>> byMethod,
            Dictionary<string, GameMemberInfo> rows,
            Report report)
        {
            string where = "GameAccess." + method.Name;
            if ((method.ImplAttributes & MethodImplAttributes.NoInlining) == 0)
            {
                report.Fail(where + " must be marked [MethodImpl(MethodImplOptions.NoInlining)].");
            }

            Dictionary<string, Access> accessed = byMethod.TryGetValue(method, out List<Access> list)
                ? list.Where(a => a.Kind != "type").GroupBy(a => a.Key).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal)
                : new Dictionary<string, Access>(StringComparer.Ordinal);
            List<CustomAttribute> declarations = method.CustomAttributes.Where(a => a.AttributeType.FullName == GameMemberAttributeName).ToList();
            if (declarations.Count == 0 && accessed.Count == 0)
            {
                report.Warn(where + " touches no non-public game member; move it out of GameAccess.");
                return;
            }

            HashSet<string> declaredKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (CustomAttribute declaration in declarations)
            {
                TypeReference owner = (TypeReference)declaration.ConstructorArguments[0].Value;
                string name = (string)declaration.ConstructorArguments[1].Value;
                TypeReference declaredType = declaration.ConstructorArguments[2].Value as TypeReference;
                CustomAttributeArgument[] features = declaration.ConstructorArguments[3].Value as CustomAttributeArgument[] ?? new CustomAttributeArgument[0];
                string key = owner.FullName + "::" + name;
                declaredKeys.Add(key);

                if (features.Length == 0)
                {
                    report.Fail(where + ": [GameMember] " + AssemblyModel.Display(owner) + "." + name + " must list at least one FeatureId.");
                }

                if (!accessed.TryGetValue(key, out Access access))
                {
                    report.Fail(where + ": [GameMember] " + AssemblyModel.Display(owner) + "." + name + " " + ExplainNotAccessed(owner, name) + ".");
                    continue;
                }

                if (declaredType != null && access.MemberType != null && access.Kind != "method" &&
                    declaredType.FullName != access.MemberType.FullName)
                {
                    report.Fail(
                        where + ": [GameMember] " + access.Display + " declares type " + AssemblyModel.Display(declaredType) +
                        " but the member is " + AssemblyModel.Display(access.MemberType) + ".");
                }

                if (!rows.TryGetValue(key, out GameMemberInfo row))
                {
                    row = new GameMemberInfo
                    {
                        Member = access.Display,
                        Kind = access.Kind,
                        MemberType = AssemblyModel.Display(access.MemberType),
                        Visibility = AssemblyModel.Visibility(access.Definition)
                    };
                    rows[key] = row;
                }

                row.Accessors.Add(method.Name);
                foreach (CustomAttributeArgument feature in features)
                {
                    int order = Convert.ToInt32(feature.Value, System.Globalization.CultureInfo.InvariantCulture);
                    row.Features[order] = AssemblyModel.EnumName(feature.Type, feature.Value);
                }
            }

            foreach (Access access in accessed.Values)
            {
                if (!declaredKeys.Contains(access.Key))
                {
                    report.Fail(where + " accesses non-public game " + access.Kind + " " + access.Display + " without a matching [GameMember].");
                }
            }
        }

        private static string ExplainNotAccessed(TypeReference ownerReference, string name)
        {
            TypeDefinition owner = AssemblyModel.Resolve(ownerReference);
            if (owner == null)
            {
                return "has an owner type that cannot be resolved";
            }

            IMemberDefinition member =
                (IMemberDefinition)owner.Fields.FirstOrDefault(f => f.Name == name) ??
                (IMemberDefinition)owner.Properties.FirstOrDefault(p => p.Name == name) ??
                owner.Methods.FirstOrDefault(m => m.Name == name);
            if (member == null)
            {
                return "does not exist in the original " + AssemblyModel.GameAssemblyName;
            }

            if (member is PropertyDefinition property)
            {
                MethodDefinition accessor = property.GetMethod ?? property.SetMethod;
                member = accessor ?? member;
            }

            return AssemblyModel.IsPublicApi(member)
                ? "is public in the original " + AssemblyModel.GameAssemblyName + "; access it directly instead of through GameAccess"
                : "is declared but the method body does not access it";
        }

        private static List<Access> Collect(AssemblyModel model, MethodDefinition method, Report report)
        {
            List<Access> accesses = new List<Access>();
            IlWalker.WalkMethod(
                method,
                reference =>
                {
                    TypeDefinition gameType = model.ResolveGameType(reference);
                    if (gameType != null && !AssemblyModel.IsPublicApi(gameType))
                    {
                        accesses.Add(new Access("type:" + gameType.FullName, AssemblyModel.Display(gameType), "type", gameType, null));
                    }
                },
                member =>
                {
                    if (model.ResolveGameType(member.DeclaringType) == null)
                    {
                        return;
                    }

                    IMemberDefinition definition = AssemblyModel.Resolve(member);
                    if (definition == null)
                    {
                        report.Fail(Where(method) + ": " + member.FullName + " cannot be resolved in the original " + AssemblyModel.GameAssemblyName + ".");
                        return;
                    }

                    if (!AssemblyModel.IsPublicApi(definition))
                    {
                        accesses.Add(Access.For(definition));
                    }
                });
            return accesses;
        }

        private static bool IsGameAccess(TypeDefinition type)
        {
            return AssemblyModel.Outermost(type).FullName == GameAccessTypeName;
        }

        private static string Where(MethodDefinition method)
        {
            return method.DeclaringType.FullName + "." + method.Name;
        }

        private sealed class Access
        {
            internal Access(string key, string display, string kind, IMemberDefinition definition, TypeReference memberType)
            {
                Key = key;
                Display = display;
                Kind = kind;
                Definition = definition;
                MemberType = memberType;
            }

            internal string Key { get; }

            internal string Display { get; }

            internal string Kind { get; }

            internal IMemberDefinition Definition { get; }

            internal TypeReference MemberType { get; }

            internal static Access For(IMemberDefinition definition)
            {
                TypeDefinition owner = definition.DeclaringType;
                string ownerName = AssemblyModel.Display(owner);
                switch (definition)
                {
                    case FieldDefinition field:
                        return new Access(owner.FullName + "::" + field.Name, ownerName + "." + field.Name, "field", field, field.FieldType);
                    case MethodDefinition method when method.IsGetter || method.IsSetter:
                        PropertyDefinition property = owner.Properties.FirstOrDefault(p => p.GetMethod == method || p.SetMethod == method);
                        if (property != null)
                        {
                            return new Access(owner.FullName + "::" + property.Name, ownerName + "." + property.Name, "property", method, property.PropertyType);
                        }

                        goto default;
                    case MethodDefinition method:
                        return new Access(owner.FullName + "::" + method.Name, ownerName + "." + method.Name + "()", "method", method, method.ReturnType);
                    default:
                        return new Access(owner.FullName + "::" + definition.Name, ownerName + "." + definition.Name, "member", definition, null);
                }
            }
        }
    }
}
