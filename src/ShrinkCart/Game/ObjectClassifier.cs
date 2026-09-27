using System.Collections.Generic;
using ShrinkCart.Config;
using ShrinkCart.Core;
using ShrinkCart.Domain;

namespace ShrinkCart.Game
{
    internal enum ObjectKind
    {
        /// <summary>不参与任何功能（普通非贵重物等）。</summary>
        Unsupported,

        /// <summary>车类物体：永不缩小，可被车类物品拦截。</summary>
        CartLike,

        /// <summary>玩家死亡头颅：只由死亡头颅功能处理。</summary>
        DeathHead,

        /// <summary>敌人（或敌人刚体）：只由敌人进车秒杀处理。</summary>
        Enemy,

        /// <summary>可能被缩小的物品：是否缩小及倍率取决于当前配置。</summary>
        ShrinkCandidate
    }

    /// <summary>单个物体的分类结果。静态事实只计算一次；缩小倍率随配置版本刷新。</summary>
    internal sealed class ObjectInfo
    {
        internal int Id;
        internal ObjectKind Kind;
        internal string CleanName;
        internal Enemy Enemy;
        internal PlayerDeathHead Head;
        internal ItemEquippable Equippable;
        internal ShrinkCategory Category;
        internal bool IsShopItem;
        internal int ShrinkConfigVersion = -1;
        internal bool CanShrink;
        internal float Factor = 1f;
    }

    /// <summary>
    /// 对进车物体做一次性分类并按实例 ID 缓存（换关时清空）。热路径（trigger 每个物理帧）只做一次字典查找；
    /// 组件查找、名称清洗等开销只在物体第一次出现时发生。会变化的状态（dead、已装备、kinematic、头颅是否触发）
    /// 由各功能在使用时现查。
    /// </summary>
    internal sealed class ObjectClassifier
    {
        private readonly Dictionary<int, ObjectInfo> _cache = new Dictionary<int, ObjectInfo>();

        internal int Count => _cache.Count;

        internal ObjectInfo Get(PhysGrabObject item)
        {
            int id = item.gameObject.GetInstanceID();
            if (_cache.TryGetValue(id, out ObjectInfo info))
            {
                return info;
            }

            info = Classify(item, id);
            _cache[id] = info;
            DebugCounters.Classifications++;
            return info;
        }

        /// <summary>按当前配置判断候选物品是否缩小及倍率（配置版本变化时重算）。</summary>
        internal bool TryGetShrinkFactor(ObjectInfo info, out float factor)
        {
            if (info.Kind != ObjectKind.ShrinkCandidate)
            {
                factor = 1f;
                return false;
            }

            int version = ModConfig.ScalingConfigVersion;
            if (info.ShrinkConfigVersion != version)
            {
                float resolved = 1f;
                info.CanShrink = info.IsShopItem
                    ? ModConfig.ShrinkShopPlayerItems.Value && ModConfig.TryGetScaleFactor(ShrinkCategory.Fallback, out resolved)
                    : ModConfig.TryGetScaleFactor(info.Category, out resolved);
                info.Factor = info.CanShrink ? resolved : 1f;
                info.ShrinkConfigVersion = version;
            }

            factor = info.Factor;
            return info.CanShrink;
        }

        internal void Clear()
        {
            _cache.Clear();
        }

        private static ObjectInfo Classify(PhysGrabObject item, int id)
        {
            ObjectInfo info = new ObjectInfo
            {
                Id = id,
                Kind = ObjectKind.Unsupported,
                CleanName = ObjectNames.Clean(item.name)
            };

            if (CartLikeDetector.Compute(item, info.CleanName))
            {
                info.Kind = ObjectKind.CartLike;
                return info;
            }

            PlayerDeathHead head = FindDeathHead(item);
            if (head != null)
            {
                info.Kind = ObjectKind.DeathHead;
                info.Head = head;
                return info;
            }

            Enemy enemy = FindEnemy(item);
            if (enemy != null)
            {
                info.Kind = ObjectKind.Enemy;
                info.Enemy = enemy;
                return info;
            }

            info.Equippable = item.GetComponent<ItemEquippable>();
            if (TryResolveCategory(item, info.CleanName, out ShrinkCategory category))
            {
                info.Kind = ObjectKind.ShrinkCandidate;
                info.Category = category;
                if (category == ShrinkCategory.ValuableBox)
                {
                    ModLog.Debug("Resolved token/cosmetic box category for " + item.name + " kind=" + SpecialBoxDetector.Describe(item));
                }

                return info;
            }

            if (IsShopItem(item, info.Equippable))
            {
                info.Kind = ObjectKind.ShrinkCandidate;
                info.Category = ShrinkCategory.Fallback;
                info.IsShopItem = true;
            }

            return info;
        }

        private static bool TryResolveCategory(PhysGrabObject item, string cleanName, out ShrinkCategory category)
        {
            category = ShrinkCategory.Fallback;
            if (item.GetComponent<SurplusValuable>() != null)
            {
                category = ShrinkCategory.Surplus;
                return true;
            }

            if (SpecialBoxDetector.IsSpecialBox(item))
            {
                category = ShrinkCategory.ValuableBox;
                return true;
            }

            if (ObjectNames.TryParseEnemyOrb(cleanName, out category))
            {
                return true;
            }

            ValuableObject valuable = item.GetComponent<ValuableObject>();
            if (valuable == null)
            {
                return false;
            }

            category = FromVolumeType(valuable.volumeType);
            return true;
        }

        private static bool IsShopItem(PhysGrabObject item, ItemEquippable equippable)
        {
            return item.GetComponent<ItemAttributes>() != null ||
                   equippable != null ||
                   GameAccess.IsGun(item) ||
                   item.GetComponent<ItemBattery>() != null;
        }

        private static ShrinkCategory FromVolumeType(ValuableVolume.Type volumeType)
        {
            switch (volumeType)
            {
                case ValuableVolume.Type.Tiny:
                    return ShrinkCategory.Tiny;
                case ValuableVolume.Type.Small:
                    return ShrinkCategory.Small;
                case ValuableVolume.Type.Medium:
                    return ShrinkCategory.Medium;
                case ValuableVolume.Type.Big:
                    return ShrinkCategory.Big;
                case ValuableVolume.Type.Wide:
                    return ShrinkCategory.Wide;
                case ValuableVolume.Type.Tall:
                    return ShrinkCategory.Tall;
                case ValuableVolume.Type.VeryTall:
                    return ShrinkCategory.VeryTall;
                default:
                    return ShrinkCategory.Fallback;
            }
        }

        private static PlayerDeathHead FindDeathHead(PhysGrabObject item)
        {
            PlayerDeathHead head = item.GetComponent<PlayerDeathHead>();
            if (head != null)
            {
                return head;
            }

            head = item.GetComponentInParent<PlayerDeathHead>();
            return head != null ? head : item.GetComponentInChildren<PlayerDeathHead>(true);
        }

        private static Enemy FindEnemy(PhysGrabObject item)
        {
            EnemyRigidbody enemyRigidbody = item.GetComponentInParent<EnemyRigidbody>();
            if (enemyRigidbody != null && enemyRigidbody.enemy != null)
            {
                return enemyRigidbody.enemy;
            }

            return item.GetComponentInParent<Enemy>();
        }
    }
}
