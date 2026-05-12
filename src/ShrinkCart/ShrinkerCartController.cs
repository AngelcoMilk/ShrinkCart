using System;
using System.Collections.Generic;
using System.Reflection;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class ShrinkerCartController
    {
        private const float TickIntervalSeconds = 0.1f;

        private sealed class TrackedObject
        {
            internal GameObject Target;
            internal float LastSeenInCartTime;
            internal float EarliestRestoreTime;
            internal ShrinkCategory Category;
        }

        private sealed class CachedShrinkData
        {
            internal int ConfigVersion;
            internal bool CanShrink;
            internal ShrinkCategory Category;
            internal float Factor;
        }

        private static readonly Dictionary<int, TrackedObject> TrackedObjects =
            new Dictionary<int, TrackedObject>();

        private static readonly Dictionary<int, CachedShrinkData> ShrinkDataCache =
            new Dictionary<int, CachedShrinkData>();

        private static readonly Dictionary<int, float> ReshrinkCooldownUntil =
            new Dictionary<int, float>();

        private static readonly HashSet<int> PermanentlyExcludedObjectIds = new HashSet<int>();

        private static readonly List<int> RestoreIds = new List<int>(16);
        private static readonly List<int> ExpiredCooldownIds = new List<int>(16);
        private static readonly List<GameObject> RestoreTargets = new List<GameObject>(16);

        private static readonly FieldInfo ScaleOptionsField =
            typeof(ScaleController).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo ItemAttributesItemTypeField =
            typeof(ItemAttributes).GetField("itemType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo PhysGrabObjectIsGunField =
            typeof(PhysGrabObject).GetField("isGun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo PhysGrabCartPhysGrabObjectField =
            typeof(PhysGrabCart).GetField("physGrabObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static float _nextTickTime;

        internal static void Reset()
        {
            TrackedObjects.Clear();
            ShrinkDataCache.Clear();
            ReshrinkCooldownUntil.Clear();
            PermanentlyExcludedObjectIds.Clear();
            RestoreIds.Clear();
            ExpiredCooldownIds.Clear();
            RestoreTargets.Clear();
            CartScaleVisualEffects.Reset();
            _nextTickTime = 0.0f;
        }

        internal static void ProcessCartObject(PhysGrabInCart inCart, PhysGrabObject item)
        {
            if (inCart == null || inCart.cart == null || item == null)
            {
                return;
            }

            if (!IsHostOrSingleplayer())
            {
                if (ShouldMarkPotentialClientCartScale(item))
                {
                    CartScaleVisualEffects.MarkCartObject(item.gameObject);
                }

                return;
            }

            if (EnemyInCartKillController.TryKill(item))
            {
                CartScaleVisualEffects.UnmarkCartObject(item.gameObject);
                return;
            }

            ShrinkCategory category = ShrinkCategory.Fallback;
            float factor = 1.0f;
            bool canShrink = ModConfig.CartShrinkingEnabled.Value && TryGetShrinkData(item, out category, out factor);
            if (canShrink)
            {
                CartScaleVisualEffects.MarkCartObject(item.gameObject);
            }

            if (canShrink)
            {
                TrackOrShrink(item, category, factor);
            }
        }

        internal static void Tick()
        {
            if (!IsHostOrSingleplayer())
            {
                return;
            }

            float now = Time.time;
            if (now < _nextTickTime)
            {
                return;
            }

            _nextTickTime = now + TickIntervalSeconds;
            ClearExpiredCooldowns(now);
            CartScaleVisualEffects.ClearExpiredPending(now);

            if (!ModConfig.CartShrinkingEnabled.Value)
            {
                RestoreAll();
                return;
            }

            if (TrackedObjects.Count == 0)
            {
                return;
            }

            float leaveDebounce = ModConfig.SafeCartLeaveDebounceSeconds();
            RestoreIds.Clear();

            foreach (KeyValuePair<int, TrackedObject> pair in TrackedObjects)
            {
                TrackedObject tracked = pair.Value;
                if (tracked.Target == null ||
                    (now - tracked.LastSeenInCartTime >= leaveDebounce && now >= tracked.EarliestRestoreTime))
                {
                    RestoreIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < RestoreIds.Count; i++)
            {
                int id = RestoreIds[i];
                TrackedObject tracked;
                if (!TrackedObjects.TryGetValue(id, out tracked))
                {
                    continue;
                }

                RestoreTrackedObject(id, tracked.Target);
                TrackedObjects.Remove(id);
            }
        }

        internal static void RestoreAll()
        {
            if (TrackedObjects.Count == 0)
            {
                return;
            }

            RestoreTargets.Clear();
            foreach (TrackedObject tracked in TrackedObjects.Values)
            {
                if (tracked.Target != null)
                {
                    RestoreTargets.Add(tracked.Target);
                }
            }

            TrackedObjects.Clear();

            for (int i = 0; i < RestoreTargets.Count; i++)
            {
                RestoreTrackedObject(RestoreTargets[i].GetInstanceID(), RestoreTargets[i]);
            }

            RestoreTargets.Clear();
        }

        private static void TrackOrShrink(PhysGrabObject item, ShrinkCategory category, float factor)
        {
            GameObject target = item == null ? null : item.gameObject;
            if (target == null)
            {
                return;
            }

            int id = target.GetInstanceID();
            float now = Time.time;
            TrackedObject tracked;
            if (TrackedObjects.TryGetValue(id, out tracked))
            {
                tracked.LastSeenInCartTime = now;
                tracked.EarliestRestoreTime = now + ModConfig.SafeCartLeaveDebounceSeconds();
                tracked.Category = category;
                CartScaleVisualEffects.MarkCartObject(target);
                return;
            }

            if (IsInReshrinkCooldown(id))
            {
                return;
            }

            if (ScaleManager.IsScaled(target))
            {
                return;
            }

            ScaleOptions options = ScaleOptions.Default;
            options.Factor = factor;
            options.Speed = ModConfig.SafeScaleSpeed();
            options.Duration = 0.0f;
            options.AllowedTargets = ScaleTargets.Valuables | ScaleTargets.Items;
            options.SuppressValueDropExpand = ModConfig.SuppressValuableDamageRestore.Value;
            options.PreserveMass = ModConfig.PreserveCartMass.Value;

            try
            {
                CartScaleVisualEffects.MarkCartObject(target);
                if (!ScaleManager.ApplyIfNotScaled(target, options))
                {
                    CartScaleVisualEffects.UnmarkCartObject(target);
                    return;
                }
            }
            catch (Exception ex)
            {
                CartScaleVisualEffects.UnmarkCartObject(target);
                Plugin.Log.LogWarning("Failed to shrink " + target.name + ": " + ex.Message);
                return;
            }

            TrackedObjects[id] = new TrackedObject
            {
                Target = target,
                LastSeenInCartTime = now,
                EarliestRestoreTime = now + ModConfig.SafeCartLeaveDebounceSeconds(),
                Category = category
            };

            DebugLog("Shrunk " + target.name + " as " + category + " factor=" + factor.ToString("0.###"));
        }

        private static void RestoreTrackedObject(int id, GameObject target)
        {
            if (target == null)
            {
                return;
            }

            BeginReshrinkCooldown(id);

            try
            {
                if (!ScaleManager.IsScaled(target))
                {
                    CartScaleVisualEffects.UnmarkCartObject(target);
                    return;
                }

                CartScaleVisualEffects.MarkCartObject(target);
                ApplyRestoreSpeed(target);
                ScaleManager.Restore(target);
                CartScaleVisualEffects.UnmarkCartObject(target);
                DebugLog("Restored " + target.name + " speed=" + ModConfig.SafeRestoreScaleSpeed().ToString("0.###"));
            }
            catch (Exception ex)
            {
                CartScaleVisualEffects.UnmarkCartObject(target);
                Plugin.Log.LogWarning("Failed to restore " + target.name + ": " + ex.Message);
            }
        }

        private static void ApplyRestoreSpeed(GameObject target)
        {
            if (ScaleOptionsField == null)
            {
                return;
            }

            ScaleController controller = ScaleManager.GetController(target);
            if (controller == null)
            {
                return;
            }

            object boxedOptions = ScaleOptionsField.GetValue(controller);
            if (!(boxedOptions is ScaleOptions))
            {
                return;
            }

            ScaleOptions options = (ScaleOptions)boxedOptions;
            options.Speed = ModConfig.SafeRestoreScaleSpeed();
            ScaleOptionsField.SetValue(controller, options);
        }

        private static bool TryGetShrinkData(PhysGrabObject item, out ShrinkCategory category, out float factor)
        {
            category = ShrinkCategory.Fallback;
            factor = 1.0f;

            if (!PassesFastCandidateChecks(item))
            {
                return false;
            }

            int id = item.gameObject.GetInstanceID();
            if (IsInReshrinkCooldown(id))
            {
                return false;
            }

            CachedShrinkData cached;
            if (ShrinkDataCache.TryGetValue(id, out cached) && cached.ConfigVersion == ModConfig.ScalingConfigVersion)
            {
                category = cached.Category;
                factor = cached.Factor;
                return cached.CanShrink;
            }

            bool canShrink = ResolveShrinkData(item, out category, out factor);
            ShrinkDataCache[id] = new CachedShrinkData
            {
                ConfigVersion = ModConfig.ScalingConfigVersion,
                CanShrink = canShrink,
                Category = category,
                Factor = factor
            };

            return canShrink;
        }

        private static bool PassesFastCandidateChecks(PhysGrabObject item)
        {
            if (item == null || item.gameObject == null)
            {
                return false;
            }

            if (item.dead)
            {
                return false;
            }

            int id = item.gameObject.GetInstanceID();
            if (PermanentlyExcludedObjectIds.Contains(id))
            {
                return false;
            }

            string cleanName = CleanName(item.name);

            if (IsPermanentlyExcludedCartItem(item, cleanName))
            {
                PermanentlyExcludedObjectIds.Add(id);
                return false;
            }

            ItemEquippable equippable = item.GetComponent<ItemEquippable>();
            if (equippable != null && equippable.IsEquipped())
            {
                return false;
            }

            if (item.rb != null && item.rb.isKinematic)
            {
                return false;
            }

            return true;
        }

        private static bool ShouldMarkPotentialClientCartScale(PhysGrabObject item)
        {
            if (!PassesFastCandidateChecks(item))
            {
                return false;
            }

            string cleanName = CleanName(item.name);
            ShrinkCategory category;
            if (TryResolveCategory(item, cleanName, out category))
            {
                return HostConfigSync.ShouldMarkCategoryForVisual(category);
            }

            if (IsShopPlayerItem(item))
            {
                return HostConfigSync.ShouldMarkShopPlayerItemForVisual();
            }

            return HostConfigSync.ShouldMarkNonValuableItemForVisual();
        }

        private static bool ResolveShrinkData(PhysGrabObject item, out ShrinkCategory category, out float factor)
        {
            category = ShrinkCategory.Fallback;
            factor = 1.0f;

            string cleanName = CleanName(item.name);
            if (!TryResolveCategory(item, cleanName, out category))
            {
                if (IsShopPlayerItem(item))
                {
                    if (!ModConfig.ShrinkShopPlayerItems.Value)
                    {
                        return false;
                    }

                    category = ShrinkCategory.Fallback;
                    return ModConfig.TryGetScaleFactor(category, out factor);
                }

                if (!ModConfig.ShrinkNonValuableItems.Value)
                {
                    return false;
                }

                category = ShrinkCategory.Fallback;
            }

            return ModConfig.TryGetScaleFactor(category, out factor);
        }

        private static bool IsPermanentlyExcludedCartItem(PhysGrabObject item, string cleanName)
        {
            PhysGrabCart cart = item.GetComponent<PhysGrabCart>();
            if (cart != null)
            {
                return true;
            }

            cart = item.GetComponentInParent<PhysGrabCart>();
            if (cart != null && IsCartRootPhysObject(cart, item))
            {
                return true;
            }

            if (item.GetComponent<ItemVehicle>() != null || item.GetComponentInParent<ItemVehicle>() != null)
            {
                return true;
            }

            if (item.GetComponent<ItemCartCannon>() != null ||
                item.GetComponent<ItemCartCannonMain>() != null ||
                item.GetComponent<ItemCartLaser>() != null ||
                item.GetComponentInParent<ItemCartCannon>() != null ||
                item.GetComponentInParent<ItemCartCannonMain>() != null ||
                item.GetComponentInParent<ItemCartLaser>() != null)
            {
                return true;
            }

            if (cleanName == "Item Cart Cannon" || cleanName == "Item Cart Laser")
            {
                return true;
            }

            ItemAttributes attributes = item.GetComponent<ItemAttributes>();
            if (attributes == null)
            {
                return false;
            }

            SemiFunc.itemType itemType;
            if (!TryGetItemType(attributes, out itemType))
            {
                return false;
            }

            return itemType == SemiFunc.itemType.cart ||
                   itemType == SemiFunc.itemType.vehicle ||
                   itemType == SemiFunc.itemType.pocket_cart;
        }

        private static bool IsCartRootPhysObject(PhysGrabCart cart, PhysGrabObject item)
        {
            if (cart == null || item == null || PhysGrabCartPhysGrabObjectField == null)
            {
                return false;
            }

            return PhysGrabCartPhysGrabObjectField.GetValue(cart) as PhysGrabObject == item;
        }

        private static bool IsShopPlayerItem(PhysGrabObject item)
        {
            ItemAttributes attributes = item.GetComponent<ItemAttributes>();
            if (attributes != null)
            {
                return true;
            }

            ItemEquippable equippable = item.GetComponent<ItemEquippable>();
            if (equippable != null)
            {
                return true;
            }

            if (TryGetIsGun(item))
            {
                return true;
            }

            return item.GetComponent<ItemBattery>() != null;
        }

        private static bool TryGetItemType(ItemAttributes attributes, out SemiFunc.itemType itemType)
        {
            itemType = default(SemiFunc.itemType);
            if (attributes == null || ItemAttributesItemTypeField == null)
            {
                return false;
            }

            object value = ItemAttributesItemTypeField.GetValue(attributes);
            if (!(value is SemiFunc.itemType))
            {
                return false;
            }

            itemType = (SemiFunc.itemType)value;
            return true;
        }

        private static bool TryGetIsGun(PhysGrabObject item)
        {
            if (item == null || PhysGrabObjectIsGunField == null)
            {
                return false;
            }

            object value = PhysGrabObjectIsGunField.GetValue(item);
            return value is bool && (bool)value;
        }

        private static bool TryResolveCategory(PhysGrabObject item, string cleanName, out ShrinkCategory category)
        {
            category = ShrinkCategory.Fallback;

            if (item.GetComponent<SurplusValuable>() != null)
            {
                category = ShrinkCategory.Surplus;
                return true;
            }

            if (TryResolveEnemyOrb(cleanName, out category))
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

        private static bool TryResolveEnemyOrb(string cleanName, out ShrinkCategory category)
        {
            category = ShrinkCategory.Fallback;
            if (string.IsNullOrEmpty(cleanName))
            {
                return false;
            }

            if (!cleanName.StartsWith("Enemy", StringComparison.OrdinalIgnoreCase))
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

        private static void BeginReshrinkCooldown(int id)
        {
            ReshrinkCooldownUntil[id] = Time.time + ModConfig.SafeReshrinkCooldownSeconds();
        }

        private static bool IsInReshrinkCooldown(int id)
        {
            float until;
            if (!ReshrinkCooldownUntil.TryGetValue(id, out until))
            {
                return false;
            }

            if (Time.time < until)
            {
                return true;
            }

            ReshrinkCooldownUntil.Remove(id);
            return false;
        }

        private static void ClearExpiredCooldowns(float now)
        {
            if (ReshrinkCooldownUntil.Count == 0)
            {
                return;
            }

            ExpiredCooldownIds.Clear();
            foreach (KeyValuePair<int, float> pair in ReshrinkCooldownUntil)
            {
                if (now >= pair.Value)
                {
                    ExpiredCooldownIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < ExpiredCooldownIds.Count; i++)
            {
                ReshrinkCooldownUntil.Remove(ExpiredCooldownIds[i]);
            }

            ExpiredCooldownIds.Clear();
        }

        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            return name.Replace("Valuable ", string.Empty).Replace("(Clone)", string.Empty).Trim();
        }

        private static bool IsHostOrSingleplayer()
        {
            try
            {
                return SemiFunc.IsMasterClientOrSingleplayer();
            }
            catch
            {
                return true;
            }
        }

        private static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging.Value)
            {
                Plugin.Log.LogInfo(message);
            }
        }
    }
}
