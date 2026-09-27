using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace ShrinkCart.BuildTools
{
    /// <summary>
    /// 用 Mono.Cecil 读取编译产物 ShrinkCart.dll 与游戏原版（未 publicize）的 Assembly-CSharp.dll。
    /// 解析器只搜索游戏 Managed 目录、profile 的 BepInEx/core 与 ScalerCore 目录，
    /// 因此 ShrinkCart 对 Assembly-CSharp 的引用一定解析到原版，而不是 obj/ 中 publicize 后的副本。
    /// </summary>
    internal sealed class AssemblyModel : IDisposable
    {
        internal const string GameAssemblyName = "Assembly-CSharp";

        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.Single", "float" },
            { "System.Double", "double" },
            { "System.String", "string" },
            { "System.Object", "object" },
            { "System.Void", "void" }
        };

        private readonly DefaultAssemblyResolver _resolver;

        private AssemblyModel(DefaultAssemblyResolver resolver, ModuleDefinition mod, ModuleDefinition game)
        {
            _resolver = resolver;
            Mod = mod;
            Game = game;
        }

        /// <summary>编译产物 ShrinkCart.dll。</summary>
        internal ModuleDefinition Mod { get; }

        /// <summary>游戏原版 Assembly-CSharp.dll。</summary>
        internal ModuleDefinition Game { get; }

        internal static AssemblyModel Load(BuildContext context)
        {
            if (!File.Exists(context.TargetPath))
            {
                throw new BuildToolException(
                    "Build output not found: " + context.TargetPath + ". Run 'dotnet build -c " + context.Configuration + "' first.");
            }

            string gamePath = Path.Combine(context.ManagedDir, GameAssemblyName + ".dll");
            if (!File.Exists(gamePath))
            {
                throw new BuildToolException("Original game assembly not found: " + gamePath + " (check REPOGameDir in ShrinkCart.local.props).");
            }

            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
            foreach (string directory in resolver.GetSearchDirectories())
            {
                resolver.RemoveSearchDirectory(directory);
            }

            resolver.AddSearchDirectory(context.ManagedDir);
            AddIfExists(resolver, Path.Combine(context.ProfileDir, "BepInEx", "core"));
            AddIfExists(resolver, context.ScalerCoreDir);

            ReaderParameters parameters = new ReaderParameters { AssemblyResolver = resolver, InMemory = true };
            ModuleDefinition mod = ModuleDefinition.ReadModule(context.TargetPath, parameters);

            // 用 ShrinkCart 自己的引用名解析，保证与成员引用解析得到的是同一个 AssemblyDefinition 实例。
            AssemblyNameReference gameReference = mod.AssemblyReferences.FirstOrDefault(r => r.Name == GameAssemblyName);
            if (gameReference == null)
            {
                throw new BuildToolException("ShrinkCart.dll does not reference " + GameAssemblyName + ".");
            }

            AssemblyDefinition game = resolver.Resolve(gameReference);
            if (!string.Equals(Path.GetFullPath(game.MainModule.FileName), Path.GetFullPath(gamePath), StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildToolException("Resolved " + GameAssemblyName + " from an unexpected location: " + game.MainModule.FileName);
            }

            return new AssemblyModel(resolver, mod, game.MainModule);
        }

        public void Dispose()
        {
            Mod.Dispose();
            _resolver.Dispose();
        }

        /// <summary>解析到游戏原版中的类型定义；不属于 Assembly-CSharp（或是泛型参数）时返回 null。</summary>
        internal TypeDefinition ResolveGameType(TypeReference type)
        {
            TypeReference element = type.GetElementType();
            if (element is GenericParameter)
            {
                return null;
            }

            TypeDefinition definition = Resolve(element);
            return definition != null && definition.Module == Game ? definition : null;
        }

        internal static TypeDefinition Resolve(TypeReference type)
        {
            try
            {
                return type.Resolve();
            }
            catch (AssemblyResolutionException)
            {
                return null;
            }
        }

        internal static IMemberDefinition Resolve(MemberReference member)
        {
            try
            {
                return member switch
                {
                    FieldReference field => field.Resolve(),
                    MethodReference method => method.Resolve(),
                    TypeReference type => type.Resolve(),
                    _ => null
                };
            }
            catch (AssemblyResolutionException)
            {
                return null;
            }
        }

        /// <summary>展开泛型实参、数组、引用等，返回其中出现的每个具体类型引用。</summary>
        internal static IEnumerable<TypeReference> Flatten(TypeReference type)
        {
            if (type == null || type is GenericParameter)
            {
                yield break;
            }

            if (type is GenericInstanceType generic)
            {
                yield return generic.ElementType;
                foreach (TypeReference argument in generic.GenericArguments)
                {
                    foreach (TypeReference inner in Flatten(argument))
                    {
                        yield return inner;
                    }
                }

                yield break;
            }

            if (type is TypeSpecification specification)
            {
                foreach (TypeReference inner in Flatten(specification.ElementType))
                {
                    yield return inner;
                }

                yield break;
            }

            yield return type;
        }

        internal static bool IsPublicApi(TypeDefinition type)
        {
            return type.IsNested ? type.IsNestedPublic && IsPublicApi(type.DeclaringType) : type.IsPublic;
        }

        internal static bool IsPublicApi(IMemberDefinition member)
        {
            return member switch
            {
                FieldDefinition field => field.IsPublic && IsPublicApi(field.DeclaringType),
                MethodDefinition method => method.IsPublic && IsPublicApi(method.DeclaringType),
                TypeDefinition type => IsPublicApi(type),
                _ => true
            };
        }

        internal static string Visibility(IMemberDefinition member)
        {
            string own = member switch
            {
                FieldDefinition field => field.IsPublic ? "public"
                    : field.IsAssembly ? "internal"
                    : field.IsFamily ? "protected"
                    : field.IsFamilyOrAssembly ? "protected internal"
                    : field.IsFamilyAndAssembly ? "private protected"
                    : "private",
                MethodDefinition method => method.IsPublic ? "public"
                    : method.IsAssembly ? "internal"
                    : method.IsFamily ? "protected"
                    : method.IsFamilyOrAssembly ? "protected internal"
                    : method.IsFamilyAndAssembly ? "private protected"
                    : "private",
                TypeDefinition type => IsPublicApi(type) ? "public" : "non-public",
                _ => "?"
            };

            TypeDefinition owner = member.DeclaringType;
            return owner != null && !IsPublicApi(owner) ? own + " (non-public type)" : own;
        }

        /// <summary>文档与消息用的 C# 风格短类型名（不含命名空间）。</summary>
        internal static string Display(TypeReference type)
        {
            switch (type)
            {
                case null:
                    return "?";
                case GenericInstanceType generic:
                    string name = Display(generic.ElementType);
                    int tick = name.IndexOf('`');
                    return (tick >= 0 ? name.Substring(0, tick) : name) + "<" + string.Join(", ", generic.GenericArguments.Select(Display)) + ">";
                case ArrayType array:
                    return Display(array.ElementType) + "[]";
                case ByReferenceType byReference:
                    return "ref " + Display(byReference.ElementType);
                case TypeSpecification specification:
                    return Display(specification.ElementType);
            }

            if (Aliases.TryGetValue(type.FullName, out string alias))
            {
                return alias;
            }

            string simple = type.Name;
            int genericTick = simple.IndexOf('`');
            if (genericTick >= 0)
            {
                simple = simple.Substring(0, genericTick);
            }

            return type.DeclaringType != null ? Display(type.DeclaringType) + "." + simple : simple;
        }

        /// <summary>枚举常量值 → 成员名（找不到时返回数值）。</summary>
        internal static string EnumName(TypeReference enumType, object value)
        {
            TypeDefinition definition = Resolve(enumType);
            if (definition != null && definition.IsEnum)
            {
                long numeric = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
                foreach (FieldDefinition field in definition.Fields)
                {
                    if (field.HasConstant && Convert.ToInt64(field.Constant, System.Globalization.CultureInfo.InvariantCulture) == numeric)
                    {
                        return field.Name;
                    }
                }
            }

            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>最外层声明类型（编译器生成的闭包类归属到其外层类）。</summary>
        internal static TypeDefinition Outermost(TypeDefinition type)
        {
            while (type.DeclaringType != null)
            {
                type = type.DeclaringType;
            }

            return type;
        }

        private static void AddIfExists(DefaultAssemblyResolver resolver, string directory)
        {
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                resolver.AddSearchDirectory(directory);
            }
        }
    }
}
