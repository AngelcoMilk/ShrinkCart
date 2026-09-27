using System;
using HarmonyLib;
using ShrinkCart.Bootstrap;
using ShrinkCart.Core;
using ShrinkCart.Scaling;

namespace ShrinkCart.Patches
{
    [PatchFeature(
        FeatureId.ValuableBoxSupport,
        Frequency = "每个 PhysGrabObject 生成时一次（非代币箱只做一次组件查找链）",
        Side = "所有端（客户端也需要控制器接收 ScalerCore 同步）")]
    [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.Start))]
    internal static class PhysGrabObjectStartPatch
    {
        private static void Postfix(PhysGrabObject __instance)
        {
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (runtime == null || !runtime.Lifecycle.IsAvailable(FeatureId.ValuableBoxSupport))
            {
                return;
            }

            try
            {
                ValuableBoxSupport.OnPhysGrabObjectStarted(__instance);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.ValuableBoxSupport, "PhysGrabObject.Start postfix", ex);
            }
        }
    }

    [PatchFeature(
        FeatureId.ValuableBoxSupport,
        Frequency = "每个代币箱生成时一次",
        Side = "所有端（与 PhysGrabObject.Start 互为兜底，取决于两者 Start 的先后顺序）")]
    [HarmonyPatch(typeof(ItemValuableBox), nameof(ItemValuableBox.Start))]
    internal static class ItemValuableBoxStartPatch
    {
        private static void Postfix(ItemValuableBox __instance)
        {
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (runtime == null || !runtime.Lifecycle.IsAvailable(FeatureId.ValuableBoxSupport))
            {
                return;
            }

            try
            {
                ValuableBoxSupport.OnValuableBoxStarted(__instance);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.ValuableBoxSupport, "ItemValuableBox.Start postfix", ex);
            }
        }
    }
}
