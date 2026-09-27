using System;
using System.Collections.Generic;
using System.Reflection;
using ShrinkCart.Core;

namespace ShrinkCart.Game
{
    /// <summary>
    /// 启动时按 <see cref="GameMemberAttribute"/> 声明核对运行中的游戏版本：成员缺失或字段类型变化时，
    /// 把声明中的功能标记为不可用并记录 Warning。只在启动时反射一次，不进入任何高频路径。
    /// </summary>
    internal static class GameAccessSelfCheck
    {
        private const BindingFlags AllInstanceAndStatic =
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        /// <returns>缺失或类型不符的成员数量。</returns>
        internal static int Run(FeatureAvailability availability)
        {
            HashSet<string> problems = new HashSet<string>();
            MethodInfo[] methods = typeof(GameAccess).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                object[] members = methods[i].GetCustomAttributes(typeof(GameMemberAttribute), false);
                for (int k = 0; k < members.Length; k++)
                {
                    GameMemberAttribute member = (GameMemberAttribute)members[k];
                    string problem = Check(member);
                    if (problem == null)
                    {
                        continue;
                    }

                    for (int f = 0; f < member.Features.Length; f++)
                    {
                        availability.MarkUnavailable(member.Features[f], problem);
                    }

                    if (problems.Add(problem))
                    {
                        ModLog.Warn(problem);
                    }
                }
            }

            return problems.Count;
        }

        private static string Check(GameMemberAttribute member)
        {
            string description = member.Owner.Name + "." + member.Name;
            try
            {
                FieldInfo field = member.Owner.GetField(member.Name, AllInstanceAndStatic);
                if (field != null)
                {
                    return member.MemberType == null || field.FieldType == member.MemberType
                        ? null
                        : "Game member type changed: " + description + " is " + field.FieldType.Name + ", expected " + member.MemberType.Name;
                }

                if (member.Owner.GetProperty(member.Name, AllInstanceAndStatic) != null)
                {
                    return null;
                }

                foreach (MethodInfo method in member.Owner.GetMethods(AllInstanceAndStatic))
                {
                    if (method.Name == member.Name)
                    {
                        return null;
                    }
                }

                return "Game member missing: " + description;
            }
            catch (Exception ex)
            {
                return "Game member check failed for " + description + ": " + ex.Message;
            }
        }
    }
}
