using System.Collections.Generic;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class CartScaleVisualEffects
    {
        private const float PendingMarkSeconds = 5.0f;

        private static readonly Dictionary<int, float> PendingCartScaleTargetUntil =
            new Dictionary<int, float>();

        private static readonly HashSet<int> ActiveCartScaleTargetIds = new HashSet<int>();
        private static readonly List<int> ExpiredPendingIds = new List<int>(16);

        [System.ThreadStatic]
        private static int _suppressPhysImpactDepth;

        internal static void Reset()
        {
            PendingCartScaleTargetUntil.Clear();
            ActiveCartScaleTargetIds.Clear();
            ExpiredPendingIds.Clear();
            _suppressPhysImpactDepth = 0;
        }

        internal static void ClearExpiredPending(float now)
        {
            if (PendingCartScaleTargetUntil.Count == 0)
            {
                return;
            }

            ExpiredPendingIds.Clear();
            foreach (KeyValuePair<int, float> pair in PendingCartScaleTargetUntil)
            {
                if (now >= pair.Value)
                {
                    ExpiredPendingIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < ExpiredPendingIds.Count; i++)
            {
                PendingCartScaleTargetUntil.Remove(ExpiredPendingIds[i]);
            }

            ExpiredPendingIds.Clear();
        }

        internal static void MarkCartObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            PendingCartScaleTargetUntil[target.GetInstanceID()] = Time.time + PendingMarkSeconds;
        }

        internal static void UnmarkCartObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            int id = target.GetInstanceID();
            PendingCartScaleTargetUntil.Remove(id);
            ActiveCartScaleTargetIds.Remove(id);
        }

        internal static bool BeginScalerCoreDispatch(ScaleController controller)
        {
            if (!HostConfigSync.EffectiveHideScaleFlash() || controller == null)
            {
                return false;
            }

            GameObject target = controller.gameObject;
            if (target == null)
            {
                return false;
            }

            int id = target.GetInstanceID();
            if (!ActiveCartScaleTargetIds.Contains(id))
            {
                float until;
                if (!PendingCartScaleTargetUntil.TryGetValue(id, out until))
                {
                    return false;
                }

                if (Time.time > until)
                {
                    PendingCartScaleTargetUntil.Remove(id);
                    return false;
                }

                PendingCartScaleTargetUntil.Remove(id);
                ActiveCartScaleTargetIds.Add(id);
            }

            _suppressPhysImpactDepth++;
            return true;
        }

        internal static void EndScalerCoreDispatch(bool suppressing)
        {
            if (!suppressing)
            {
                return;
            }

            _suppressPhysImpactDepth--;
            if (_suppressPhysImpactDepth < 0)
            {
                _suppressPhysImpactDepth = 0;
            }
        }

        internal static void EndScalerCoreExpandDispatch(bool suppressing, ScaleController controller)
        {
            EndScalerCoreDispatch(suppressing);

            if (suppressing && controller != null)
            {
                UnmarkCartObject(controller.gameObject);
            }
        }

        internal static bool ShouldSuppressPhysImpactEffect()
        {
            return HostConfigSync.EffectiveHideScaleFlash() && _suppressPhysImpactDepth > 0;
        }
    }
}
