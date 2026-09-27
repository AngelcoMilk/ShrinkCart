using System;
using HarmonyLib;
using ShrinkCart.Bootstrap;
using ShrinkCart.Core;

namespace ShrinkCart.Patches
{
    // Patch 约定：只做转发；ShrinkCartRuntime.Current 为 null（初始化失败或已卸载）时直接放行；
    // 异常只上报给生命周期（停用对应功能），绝不抛回游戏。

    [PatchFeature(
        FeatureId.CartObservation,
        Frequency = "每个物理帧 × 车 trigger 内每个未休眠物体（OnTriggerStay）",
        Side = "仅主机（原版在 OnTriggerStay 中判定 IsMasterClient）")]
    [HarmonyPatch(typeof(PhysGrabInCart), nameof(PhysGrabInCart.Add))]
    internal static class PhysGrabInCartAddPatch
    {
        private static bool Prefix(PhysGrabInCart __instance, PhysGrabObject _physGrabObject, out bool __state)
        {
            __state = false;
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (runtime == null)
            {
                return true;
            }

            try
            {
                __state = runtime.Router.ShouldBlockTriggerAdd(__instance, _physGrabObject);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.CartNestingGuard, "PhysGrabInCart.Add prefix", ex);
            }

            return !__state;
        }

        // Prefix 返回 false 时 Harmony 仍会执行 Postfix，因此用 __state 区分被拦截的调用。
        private static void Postfix(PhysGrabInCart __instance, PhysGrabObject _physGrabObject, bool __state)
        {
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (__state || runtime == null)
            {
                return;
            }

            try
            {
                runtime.Router.OnTriggerPresence(__instance, _physGrabObject);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.CartObservation, "PhysGrabInCart.Add postfix", ex);
            }
        }
    }

    [PatchFeature(
        FeatureId.CartObservation,
        Frequency = "每辆车生成时一次",
        Side = "所有端（登记表是被动的，只有主机侧功能使用）")]
    [HarmonyPatch(typeof(PhysGrabCart), nameof(PhysGrabCart.Start))]
    internal static class PhysGrabCartStartPatch
    {
        private static void Postfix(PhysGrabCart __instance)
        {
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (runtime == null)
            {
                return;
            }

            try
            {
                runtime.Carts.GetOrRegister(__instance);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.CartObservation, "PhysGrabCart.Start postfix", ex);
            }
        }
    }

    [PatchFeature(
        FeatureId.CartObservation,
        Frequency = "每帧 × 每辆车（原版仅在 12 m 内有玩家且计时器到期时每 0.5 秒真正扫描；ShrinkCart 只处理真实扫描）",
        Side = "所有端调用，仅主机处理")]
    [HarmonyPatch(typeof(PhysGrabCart), nameof(PhysGrabCart.ObjectsInCart))]
    internal static class PhysGrabCartObjectsInCartPatch
    {
        private static void Prefix(PhysGrabCart __instance, out bool __state)
        {
            __state = false;
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (runtime == null)
            {
                return;
            }

            try
            {
                __state = runtime.Router.ShouldObserveScan(__instance);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.CartObservation, "PhysGrabCart.ObjectsInCart prefix", ex);
            }
        }

        private static void Postfix(PhysGrabCart __instance, bool __state)
        {
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (!__state || runtime == null)
            {
                return;
            }

            try
            {
                runtime.Router.OnScanMaybeCompleted(__instance);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.CartObservation, "PhysGrabCart.ObjectsInCart postfix", ex);
            }
        }
    }
}
