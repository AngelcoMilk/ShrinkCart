using System;
using System.Collections.Generic;
using System.Reflection;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class ShrinkerCartController
    {
        private sealed class TrackedObject
        {
            internal GameObject Target;
            internal float LastSeenTime;
            internal ShrinkCategory Category;
        }

        private static readonly Dictionary<int, TrackedObject> TrackedObjects =
            new Dictionary<int, TrackedObject>();

        private static readonly FieldInfo ScaleOptionsField =
            typeof(ScaleController).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Reset()
        {
            TrackedObjects.Clear();
        }

        internal static void ProcessCartObject(PhysGrabInCart inCart, PhysGrabObject item)
        {
            if (!IsHostOrSingleplayer())
            {
                return;
            }

            if (!ModConfig.CartShrinkingEnabled.Value)
            {
                return;
            }

            if (inCart == null || inCart.cart == null || item == null)
            {
                return;
            }

            ShrinkCategory category;
            float factor;
            if (IsShrinkCandidate(item, out category, out factor))
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

            if (!ModConfig.CartShrinkingEnabled.Value)
            {
                RestoreAll();
                return;
            }

            if (TrackedObjects.Count == 0)
            {
                return;
            }

            float now = Time.time;
            float grace = ModConfig.SafeRestoreGraceSeconds();
            List<int> restoreIds = null;

            foreach (KeyValuePair<int, TrackedObject> pair in TrackedObjects)
            {
                TrackedObject tracked = pair.Value;
                if (tracked.Target == null || now - tracked.LastSeenTime > grace)
                {
                    if (restoreIds == null)
                    {
                        restoreIds = new List<int>();
                    }

                    restoreIds.Add(pair.Key);
                }
            }

            if (restoreIds == null)
            {
                return;
            }

            for (int i = 0; i < restoreIds.Count; i++)
            {
                int id = restoreIds[i];
                TrackedObject tracked;
                if (!TrackedObjects.TryGetValue(id, out tracked))
                {
                    continue;
                }

                RestoreTrackedObject(tracked.Target);
                TrackedObjects.Remove(id);
            }
        }

        internal static void RestoreAll()
        {
            if (TrackedObjects.Count == 0)
            {
                return;
            }

            List<GameObject> targets = new List<GameObject>();
            foreach (TrackedObject tracked in TrackedObjects.Values)
            {
                if (tracked.Target != null)
                {
                    targets.Add(tracked.Target);
                }
            }

            TrackedObjects.Clear();

            for (int i = 0; i < targets.Count; i++)
            {
                RestoreTrackedObject(targets[i]);
            }
        }

        private static void TrackOrShrink(PhysGrabObject item, ShrinkCategory category, float factor)
        {
            GameObject target = item == null ? null : item.gameObject;
            if (target == null)
            {
                return;
            }

            int id = target.GetInstanceID();
            TrackedObject tracked;
            if (TrackedObjects.TryGetValue(id, out tracked))
            {
                tracked.LastSeenTime = Time.time;
                tracked.Category = category;
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
                if (!ScaleManager.ApplyIfNotScaled(target, options))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to shrink " + target.name + ": " + ex.Message);
                return;
            }

            TrackedObjects[id] = new TrackedObject
            {
                Target = target,
                LastSeenTime = Time.time,
                Category = category
            };

            DebugLog("Shrunk " + target.name + " as " + category + " factor=" + factor.ToString("0.###"));
        }

        private static void RestoreTrackedObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                if (!ScaleManager.IsScaled(target))
                {
                    return;
                }

                ApplyRestoreSpeed(target);
                ScaleManager.Restore(target);
                DebugLog("Restored " + target.name + " speed=" + ModConfig.SafeRestoreScaleSpeed().ToString("0.###"));
            }
            catch (Exception ex)
            {
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

        private static bool IsShrinkCandidate(PhysGrabObject item, out ShrinkCategory category, out float factor)
        {
            category = ShrinkCategory.Fallback;
            factor = 1.0f;

            if (item == null || item.gameObject == null)
            {
                return false;
            }

            if (item.dead)
            {
                return false;
            }

            if (item.GetComponent<PhysGrabCart>() != null)
            {
                return false;
            }

            if (item.GetComponent<ItemVehicle>() != null)
            {
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

            string cleanName = CleanName(item.name);
            if (cleanName == "Item Cart Cannon" || cleanName == "Item Cart Laser")
            {
                return false;
            }

            if (!TryResolveCategory(item, cleanName, out category))
            {
                if (!ModConfig.ShrinkNonValuableItems.Value)
                {
                    return false;
                }

                category = ShrinkCategory.Fallback;
            }

            return ModConfig.TryGetScaleFactor(category, out factor);
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
