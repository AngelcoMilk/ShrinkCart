using System;
using System.Collections.Generic;
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
        }

        private static readonly Dictionary<int, TrackedObject> TrackedObjects =
            new Dictionary<int, TrackedObject>();

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

            if (IsShrinkCandidate(item))
            {
                TrackOrShrink(item.gameObject);
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

        private static void TrackOrShrink(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            int id = target.GetInstanceID();
            TrackedObject tracked;
            if (TrackedObjects.TryGetValue(id, out tracked))
            {
                tracked.LastSeenTime = Time.time;
                return;
            }

            if (ScaleManager.IsScaled(target))
            {
                return;
            }

            ScaleOptions options = ScaleOptions.Default;
            options.Factor = ModConfig.SafeScaleFactor();
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
                LastSeenTime = Time.time
            };

            DebugLog("Shrunk " + target.name);
        }

        private static void RestoreTrackedObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                if (ScaleManager.IsScaled(target))
                {
                    ScaleManager.Restore(target);
                    DebugLog("Restored " + target.name);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to restore " + target.name + ": " + ex.Message);
            }
        }

        private static bool IsShrinkCandidate(PhysGrabObject item)
        {
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

            if (!ModConfig.ShrinkNonValuableItems.Value && item.GetComponent<ValuableObject>() == null)
            {
                return false;
            }

            string cleanName = CleanName(item.name);
            if (cleanName == "Item Cart Cannon" || cleanName == "Item Cart Laser")
            {
                return false;
            }

            return true;
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
