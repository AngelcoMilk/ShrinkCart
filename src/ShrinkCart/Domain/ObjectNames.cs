using System;

namespace ShrinkCart.Domain
{
    /// <summary>游戏对象名称的规范化与解析（敌人球按名称区分体型）。</summary>
    internal static class ObjectNames
    {
        /// <summary>去掉 "Valuable " 前缀与 "(Clone)" 后缀，例如 "Valuable Enemy - Small(Clone)" → "Enemy - Small"。</summary>
        internal static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            return name.Replace("Valuable ", string.Empty).Replace("(Clone)", string.Empty).Trim();
        }

        /// <summary>解析敌人球名称 "Enemy - Small/Medium/Big/Berserker"（大小写不敏感）。</summary>
        internal static bool TryParseEnemyOrb(string cleanName, out ShrinkCategory category)
        {
            category = ShrinkCategory.Fallback;
            if (string.IsNullOrEmpty(cleanName) || !cleanName.StartsWith("Enemy", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string[] parts = cleanName.Split('-');
            if (parts.Length < 2)
            {
                return false;
            }

            string size = parts[1].Trim();
            if (size.Equals("Small", StringComparison.OrdinalIgnoreCase))
            {
                category = ShrinkCategory.EnemyOrbSmall;
                return true;
            }

            if (size.Equals("Medium", StringComparison.OrdinalIgnoreCase))
            {
                category = ShrinkCategory.EnemyOrbMedium;
                return true;
            }

            if (size.Equals("Big", StringComparison.OrdinalIgnoreCase))
            {
                category = ShrinkCategory.EnemyOrbBig;
                return true;
            }

            if (size.Equals("Berserker", StringComparison.OrdinalIgnoreCase))
            {
                category = ShrinkCategory.EnemyOrbBerserker;
                return true;
            }

            return false;
        }
    }
}
