using ScalerCore;
using ScalerCore.Handlers;
using UnityEngine;

namespace ShrinkCart
{
    internal static class ValuableBoxScaleAdapter
    {
        private const int HandlerPriority = -10;

        private static readonly System.Collections.Generic.HashSet<int> ValuableBoxObjectIds =
            new System.Collections.Generic.HashSet<int>();

        private static readonly System.Collections.Generic.HashSet<int> NonValuableBoxObjectIds =
            new System.Collections.Generic.HashSet<int>();

        private static bool _registered;

        internal static void Reset()
        {
            ValuableBoxObjectIds.Clear();
            NonValuableBoxObjectIds.Clear();
        }

        internal static void RegisterHandler()
        {
            if (_registered)
            {
                return;
            }

            ScaleHandlerRegistry.Register(new ValuableBoxHandler(), IsValuableBoxScaleTarget, HandlerPriority);
            _registered = true;
        }

        internal static bool IsValuableBox(PhysGrabObject item)
        {
            if (item == null || item.gameObject == null)
            {
                return false;
            }

            int id = item.gameObject.GetInstanceID();
            if (ValuableBoxObjectIds.Contains(id))
            {
                return true;
            }

            if (NonValuableBoxObjectIds.Contains(id))
            {
                return false;
            }

            bool isValuableBox = FindCosmeticWorldObject(item) != null || FindValuableBox(item) != null;
            if (isValuableBox)
            {
                ValuableBoxObjectIds.Add(id);
            }
            else
            {
                NonValuableBoxObjectIds.Add(id);
            }

            return isValuableBox;
        }

        internal static bool EnsureController(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.GetComponent<ScaleController>() != null)
            {
                return true;
            }

            if (target.GetComponent<CosmeticWorldObject>() != null ||
                target.GetComponentInParent<CosmeticWorldObject>() != null ||
                target.GetComponentInChildren<CosmeticWorldObject>(true) != null)
            {
                DebugLog("Token/cosmetic box is waiting for ScalerCore cosmetic controller: " + target.name);
                return false;
            }

            if (!IsValuableBoxScaleTarget(target))
            {
                return false;
            }

            target.AddComponent<ScaleController>();
            DebugLog("Attached ScalerCore controller to ItemValuableBox token box: " + target.name);
            return false;
        }

        internal static void EnsureController(ItemValuableBox box)
        {
            PhysGrabObject owner = FindOwnerPhysGrabObject(box);
            if (owner != null)
            {
                EnsureController(owner.gameObject);
            }
        }

        private static bool IsValuableBoxScaleTarget(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            PhysGrabObject item = target.GetComponent<PhysGrabObject>();
            if (item == null)
            {
                return false;
            }

            ItemValuableBox box = FindValuableBox(item);
            return box != null && FindOwnerPhysGrabObject(box) == item;
        }

        internal static string DescribeSpecialBox(PhysGrabObject item)
        {
            if (FindCosmeticWorldObject(item) != null)
            {
                return "CosmeticWorldObject";
            }

            if (FindValuableBox(item) != null)
            {
                return "ItemValuableBox";
            }

            return "none";
        }

        private static CosmeticWorldObject FindCosmeticWorldObject(PhysGrabObject item)
        {
            if (item == null)
            {
                return null;
            }

            CosmeticWorldObject cosmetic = item.GetComponent<CosmeticWorldObject>();
            if (cosmetic != null)
            {
                return cosmetic;
            }

            cosmetic = item.GetComponentInParent<CosmeticWorldObject>();
            if (cosmetic != null)
            {
                return cosmetic;
            }

            return item.GetComponentInChildren<CosmeticWorldObject>(true);
        }

        private static ItemValuableBox FindValuableBox(PhysGrabObject item)
        {
            if (item == null)
            {
                return null;
            }

            ItemValuableBox box = item.GetComponent<ItemValuableBox>();
            if (box != null)
            {
                return box;
            }

            box = item.GetComponentInParent<ItemValuableBox>();
            if (box != null)
            {
                return box;
            }

            return item.GetComponentInChildren<ItemValuableBox>(true);
        }

        private static PhysGrabObject FindOwnerPhysGrabObject(ItemValuableBox box)
        {
            if (box == null)
            {
                return null;
            }

            PhysGrabObject item = box.GetComponent<PhysGrabObject>();
            if (item != null)
            {
                return item;
            }

            item = box.GetComponentInParent<PhysGrabObject>();
            if (item != null)
            {
                return item;
            }

            return box.GetComponentInChildren<PhysGrabObject>(true);
        }

        private static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging != null && ModConfig.DebugLogging.Value)
            {
                Plugin.Log.LogInfo(message);
            }
        }

        private sealed class ValuableBoxHandler : IScaleHandler
        {
            public void Setup(ScaleController ctrl)
            {
            }

            public void OnScale(ScaleController ctrl)
            {
            }

            public void OnRestore(ScaleController ctrl, bool isBonk)
            {
            }

            public void OnUpdate(ScaleController ctrl)
            {
            }

            public void OnLateUpdate(ScaleController ctrl)
            {
            }

            public void OnDestroy(ScaleController ctrl)
            {
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(ItemValuableBox), "Start")]
    internal static class ItemValuableBoxStartPatch
    {
        private static void Postfix(ItemValuableBox __instance)
        {
            if (__instance != null)
            {
                ValuableBoxScaleAdapter.EnsureController(__instance);
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(PhysGrabObject), "Start")]
    internal static class PhysGrabObjectStartValuableBoxPatch
    {
        private static void Postfix(PhysGrabObject __instance)
        {
            if (__instance != null)
            {
                ValuableBoxScaleAdapter.EnsureController(__instance.gameObject);
            }
        }
    }
}
