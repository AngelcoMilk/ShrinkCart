using System;
using System.Collections.Generic;
using System.Reflection;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class DeadHeadCartScaleController
    {
        private sealed class TrackedHead
        {
            internal GameObject Target;
            internal PlayerDeathHead Head;
            internal float RestoreCheckDueTime;
            internal int LastSeenCartId;
        }

        private static readonly Dictionary<int, TrackedHead> TrackedHeads =
            new Dictionary<int, TrackedHead>();

        private static readonly HashSet<int> NonHeadObjectIds = new HashSet<int>();
        private static readonly List<int> RestoreIds = new List<int>(8);
        private static readonly List<GameObject> RestoreTargets = new List<GameObject>(8);

        private static readonly FieldInfo PlayerDeathHeadPhysGrabObjectField =
            typeof(PlayerDeathHead).GetField("physGrabObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerDeathHeadTriggeredField =
            typeof(PlayerDeathHead).GetField("triggered", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerDeathHeadInExtractionPointField =
            typeof(PlayerDeathHead).GetField("inExtractionPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerDeathHeadInTruckField =
            typeof(PlayerDeathHead).GetField("inTruck", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static float _nextRestoreCheckTime = float.PositiveInfinity;
        private static bool _allowDeadHeadsOwned;
        private static bool _previousAllowDeadHeadsValue;

        internal static void Reset()
        {
            TrackedHeads.Clear();
            NonHeadObjectIds.Clear();
            RestoreIds.Clear();
            RestoreTargets.Clear();
            _nextRestoreCheckTime = float.PositiveInfinity;
            SetAllowDeadHeads(false);
        }

        internal static void Tick()
        {
            bool enabled = Authority.IsHostOrSingleplayer() && ModConfig.DeadHeadScalingEnabledValue();
            SetAllowDeadHeads(enabled);

            if (!Authority.IsHostOrSingleplayer())
            {
                return;
            }

            if (!enabled)
            {
                RestoreAll();
                return;
            }

            float now = Time.time;
            if (TrackedHeads.Count == 0 || now < _nextRestoreCheckTime)
            {
                return;
            }

            RestoreIds.Clear();
            float nextRestoreCheck = float.PositiveInfinity;
            foreach (KeyValuePair<int, TrackedHead> pair in TrackedHeads)
            {
                TrackedHead tracked = pair.Value;
                if (tracked == null ||
                    tracked.Target == null ||
                    tracked.Head == null ||
                    ShouldRestoreImmediately(tracked.Head) ||
                    now >= tracked.RestoreCheckDueTime)
                {
                    RestoreIds.Add(pair.Key);
                }
                else if (tracked.RestoreCheckDueTime < nextRestoreCheck)
                {
                    nextRestoreCheck = tracked.RestoreCheckDueTime;
                }
            }

            for (int i = 0; i < RestoreIds.Count; i++)
            {
                int id = RestoreIds[i];
                TrackedHead tracked;
                if (!TrackedHeads.TryGetValue(id, out tracked))
                {
                    continue;
                }

                RestoreTrackedHead(id, tracked, "leave/debounce");
                TrackedHeads.Remove(id);
            }

            RestoreIds.Clear();
            _nextRestoreCheckTime = TrackedHeads.Count == 0 ? float.PositiveInfinity : nextRestoreCheck;
        }

        internal static void MarkObjectsSeenInCart(PhysGrabCart cart, List<PhysGrabObject> items)
        {
            if (!Authority.IsHostOrSingleplayer() || !ModConfig.DeadHeadScalingEnabledValue() || items == null)
            {
                return;
            }

            SetAllowDeadHeads(true);

            float now = Time.time;
            int cartId = cart == null ? 0 : cart.GetInstanceID();
            for (int i = 0; i < items.Count; i++)
            {
                PhysGrabObject item = items[i];
                PlayerDeathHead head;
                if (!TryGetTriggeredDeathHead(item, out head))
                {
                    continue;
                }

                GameObject target = item.gameObject;
                int id = target.GetInstanceID();
                TrackedHead tracked;
                if (TrackedHeads.TryGetValue(id, out tracked))
                {
                    tracked.RestoreCheckDueTime = now + ModConfig.SafeCartLeaveDebounceSeconds();
                    tracked.LastSeenCartId = cartId;
                    tracked.Head = head;
                    ScheduleRestoreCheck(tracked.RestoreCheckDueTime);
                    continue;
                }

                if (ScaleManager.IsScaled(target))
                {
                    if (TryTrackExistingHeadScale(id, target, head, cartId, now))
                    {
                        continue;
                    }

                    DebugLog("Skipped death head cart shrink because another scale session is active: " + target.name);
                    continue;
                }

                ScaleOptions options = ScaleOptions.Default;
                options.Factor = ModConfig.SafeDeadHeadScaleFactor();
                options.Speed = ModConfig.SafeScaleSpeed();
                options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
                options.Duration = 0.0f;
                options.AllowedTargets = ScaleTargets.All;
                options.SuppressValueDropExpand = true;
                options.PreserveMass = ModConfig.ShouldPreserveMass();
                options.SuppressImpactFlash = true;
                options.SuppressCameraShake = true;
                options.RejectExternalApply = false;

                try
                {
                    if (!ScaleManager.ApplyIfNotScaled(target, options))
                    {
                        DebugLog("ScalerCore rejected death head cart shrink: " + target.name);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning("Failed to shrink death head in cart: " + ex.Message);
                    continue;
                }

                float restoreDueTime = now + ModConfig.SafeCartLeaveDebounceSeconds();
                TrackedHeads[id] = new TrackedHead
                {
                    Target = target,
                    Head = head,
                    RestoreCheckDueTime = restoreDueTime,
                    LastSeenCartId = cartId
                };
                ScheduleRestoreCheck(restoreDueTime);
                DebugLog("Shrunk death head in cart: " + target.name + " factor=" + ModConfig.SafeDeadHeadScaleFactor().ToString("0.###"));
            }
        }

        internal static void RestoreBeforeRevive(PlayerDeathHead head)
        {
            if (head == null)
            {
                return;
            }

            PhysGrabObject physGrabObject = GetHeadPhysGrabObject(head);
            GameObject target = physGrabObject == null ? null : physGrabObject.gameObject;
            if (target == null)
            {
                return;
            }

            int id = target.GetInstanceID();
            TrackedHead tracked;
            if (!TrackedHeads.TryGetValue(id, out tracked))
            {
                return;
            }

            RestoreTrackedHead(id, tracked, "before revive");
            TrackedHeads.Remove(id);
            _nextRestoreCheckTime = 0.0f;
        }

        internal static void RestoreAll()
        {
            if (TrackedHeads.Count == 0)
            {
                return;
            }

            RestoreTargets.Clear();
            foreach (TrackedHead tracked in TrackedHeads.Values)
            {
                if (tracked != null && tracked.Target != null)
                {
                    RestoreTargets.Add(tracked.Target);
                }
            }

            TrackedHeads.Clear();
            _nextRestoreCheckTime = float.PositiveInfinity;

            for (int i = 0; i < RestoreTargets.Count; i++)
            {
                RestoreTarget(RestoreTargets[i], "restore all");
            }

            RestoreTargets.Clear();
        }

        private static bool TryTrackExistingHeadScale(int id, GameObject target, PlayerDeathHead head, int cartId, float now)
        {
            ScaleController controller = ScaleManager.GetController(target);
            if (controller == null || !controller.IsScaled)
            {
                return false;
            }

            ScaleOptions options = controller.CurrentOptions;
            if (!LooksLikeShrinkCartHeadOptions(options))
            {
                return false;
            }

            float restoreDueTime = now + ModConfig.SafeCartLeaveDebounceSeconds();
            TrackedHeads[id] = new TrackedHead
            {
                Target = target,
                Head = head,
                RestoreCheckDueTime = restoreDueTime,
                LastSeenCartId = cartId
            };
            ScheduleRestoreCheck(restoreDueTime);
            DebugLog("Adopted existing ShrinkCart-like death head scale: " + target.name);
            return true;
        }

        private static bool LooksLikeShrinkCartHeadOptions(ScaleOptions options)
        {
            return Mathf.Approximately(options.Factor, ModConfig.SafeDeadHeadScaleFactor()) &&
                   options.AllowedTargets == ScaleTargets.All &&
                   options.SuppressImpactFlash &&
                   options.SuppressCameraShake &&
                   options.SuppressValueDropExpand &&
                   options.PreserveMass == ModConfig.ShouldPreserveMass() &&
                   !options.RejectExternalApply;
        }

        private static void RestoreTrackedHead(int id, TrackedHead tracked, string reason)
        {
            if (tracked == null)
            {
                return;
            }

            RestoreTarget(tracked.Target, reason);
        }

        private static void RestoreTarget(GameObject target, string reason)
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

                ScaleController controller = ScaleManager.GetController(target);
                if (controller != null && controller.IsScaled)
                {
                    ScaleOptions options = controller.CurrentOptions;
                    options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
                    options.SuppressImpactFlash = true;
                    options.SuppressCameraShake = true;
                    ScaleManager.ForceUpdateOptions(target, options);
                }

                ScaleManager.ForceRestore(target);
                DebugLog("Restored death head " + target.name + " reason=" + reason);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to restore death head from cart scale: " + ex.Message);
            }
        }

        private static bool TryGetTriggeredDeathHead(PhysGrabObject item, out PlayerDeathHead head)
        {
            head = null;
            if (item == null || item.gameObject == null)
            {
                return false;
            }

            int id = item.gameObject.GetInstanceID();
            if (NonHeadObjectIds.Contains(id))
            {
                return false;
            }

            head = item.GetComponent<PlayerDeathHead>();
            if (head == null)
            {
                head = item.GetComponentInParent<PlayerDeathHead>();
            }

            if (head == null)
            {
                head = item.GetComponentInChildren<PlayerDeathHead>(true);
            }

            if (head == null)
            {
                NonHeadObjectIds.Add(id);
                head = null;
                return false;
            }

            if (!IsHeadTriggered(head) || GetHeadPhysGrabObject(head) != item)
            {
                head = null;
                return false;
            }

            return true;
        }

        private static bool ShouldRestoreImmediately(PlayerDeathHead head)
        {
            return head == null || !IsHeadTriggered(head) || IsHeadInExtractionPoint(head) || IsHeadInTruck(head);
        }

        private static PhysGrabObject GetHeadPhysGrabObject(PlayerDeathHead head)
        {
            return head == null || PlayerDeathHeadPhysGrabObjectField == null
                ? null
                : PlayerDeathHeadPhysGrabObjectField.GetValue(head) as PhysGrabObject;
        }

        private static bool IsHeadTriggered(PlayerDeathHead head)
        {
            return GetBoolField(head, PlayerDeathHeadTriggeredField);
        }

        private static bool IsHeadInExtractionPoint(PlayerDeathHead head)
        {
            return GetBoolField(head, PlayerDeathHeadInExtractionPointField);
        }

        private static bool IsHeadInTruck(PlayerDeathHead head)
        {
            return GetBoolField(head, PlayerDeathHeadInTruckField);
        }

        private static bool GetBoolField(PlayerDeathHead head, FieldInfo field)
        {
            if (head == null || field == null)
            {
                return false;
            }

            object value = field.GetValue(head);
            return value is bool && (bool)value;
        }

        private static void ScheduleRestoreCheck(float dueTime)
        {
            if (dueTime < _nextRestoreCheckTime)
            {
                _nextRestoreCheckTime = dueTime;
            }
        }

        private static void SetAllowDeadHeads(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    if (!_allowDeadHeadsOwned)
                    {
                        _previousAllowDeadHeadsValue = ScaleManager.AllowDeadHeads;
                        _allowDeadHeadsOwned = true;
                    }

                    if (!ScaleManager.AllowDeadHeads)
                    {
                        ScaleManager.AllowDeadHeads = true;
                    }

                    return;
                }

                if (!_allowDeadHeadsOwned)
                {
                    return;
                }

                ScaleManager.AllowDeadHeads = _previousAllowDeadHeadsValue;
                _allowDeadHeadsOwned = false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to update ScalerCore dead head scaling permission: " + ex.Message);
            }
        }

        private static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging != null && ModConfig.DebugLogging.Value)
            {
                Plugin.Log.LogInfo(message);
            }
        }
    }
}
