using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShrinkCart.BuildTools
{
    /// <summary>枚举一个方法或类型在签名、局部变量与 IL 指令中引用到的类型和成员。</summary>
    internal static class IlWalker
    {
        internal static void WalkMethod(MethodDefinition method, Action<TypeReference> onType, Action<MemberReference> onMember)
        {
            Types(method.ReturnType, onType);
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                Types(parameter.ParameterType, onType);
            }

            if (!method.HasBody)
            {
                return;
            }

            foreach (VariableDefinition variable in method.Body.Variables)
            {
                Types(variable.VariableType, onType);
            }

            foreach (Instruction instruction in method.Body.Instructions)
            {
                switch (instruction.Operand)
                {
                    case FieldReference field:
                        Types(field.DeclaringType, onType);
                        onMember(field);
                        break;
                    case MethodReference callee:
                        Types(callee.DeclaringType, onType);
                        if (callee is GenericInstanceMethod generic)
                        {
                            foreach (TypeReference argument in generic.GenericArguments)
                            {
                                Types(argument, onType);
                            }
                        }

                        onMember(callee);
                        break;
                    case TypeReference type:
                        Types(type, onType);
                        break;
                }
            }
        }

        /// <summary>基类、接口、字段、属性与事件类型。</summary>
        internal static void WalkTypeSignature(TypeDefinition type, Action<TypeReference> onType)
        {
            Types(type.BaseType, onType);
            foreach (InterfaceImplementation implementation in type.Interfaces)
            {
                Types(implementation.InterfaceType, onType);
            }

            foreach (FieldDefinition field in type.Fields)
            {
                Types(field.FieldType, onType);
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                Types(property.PropertyType, onType);
            }

            foreach (EventDefinition @event in type.Events)
            {
                Types(@event.EventType, onType);
            }
        }

        /// <summary>类型引用所属的程序集名（本 mod 自身的类型返回本程序集名）。</summary>
        internal static string AssemblyOf(TypeReference type)
        {
            IMetadataScope scope = type.GetElementType().Scope;
            switch (scope)
            {
                case ModuleDefinition module:
                    return module.Assembly.Name.Name;
                case AssemblyNameReference assembly:
                    return assembly.Name;
                default:
                    return scope == null ? string.Empty : scope.Name;
            }
        }

        private static void Types(TypeReference type, Action<TypeReference> onType)
        {
            foreach (TypeReference inner in AssemblyModel.Flatten(type))
            {
                onType(inner);
            }
        }
    }
}
