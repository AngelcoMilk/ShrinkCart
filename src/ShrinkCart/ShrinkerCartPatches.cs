using HarmonyLib;
using ScalerCore;

namespace ShrinkCart
{
    [HarmonyPatch(typeof(PhysGrabInCart), "Add")]
    internal static class PhysGrabInCartAddPatch
    {
        private static void Postfix(PhysGrabInCart __instance, PhysGrabObject _physGrabObject)
        {
            ShrinkerCartController.ProcessCartObject(__instance, _physGrabObject);
        }
    }

    [HarmonyPatch(typeof(ItemVehicle), "Start")]
    internal static class ItemVehicleStartPatch
    {
        private static void Postfix(ItemVehicle __instance)
        {
            VehicleCrushController.Configure(__instance);
        }
    }

    [HarmonyPatch(typeof(HurtCollider), "PlayerHurt")]
    internal static class HurtColliderPlayerHurtPatch
    {
        private static void Prefix(HurtCollider __instance, out VehicleCrushController.TemporaryHurtState __state)
        {
            __state = VehicleCrushController.BeforePlayerHurt(__instance);
        }

        private static void Postfix(HurtCollider __instance, VehicleCrushController.TemporaryHurtState __state)
        {
            VehicleCrushController.AfterHurt(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(ScaleController), "DispatchShrink")]
    internal static class ScaleControllerDispatchShrinkPatch
    {
        private static void Prefix(ScaleController __instance, out bool __state)
        {
            __state = CartScaleVisualEffects.BeginScalerCoreDispatch(__instance);
        }

        private static void Postfix(bool __state)
        {
            CartScaleVisualEffects.EndScalerCoreDispatch(__state);
        }
    }

    [HarmonyPatch(typeof(ScaleController), "DispatchExpand")]
    internal static class ScaleControllerDispatchExpandPatch
    {
        private static void Prefix(ScaleController __instance, out bool __state)
        {
            __state = CartScaleVisualEffects.BeginScalerCoreDispatch(__instance);
        }

        private static void Postfix(ScaleController __instance, bool __state)
        {
            CartScaleVisualEffects.EndScalerCoreExpandDispatch(__state, __instance);
        }
    }

    [HarmonyPatch(typeof(ScaleController), "DispatchExpandNow")]
    internal static class ScaleControllerDispatchExpandNowPatch
    {
        private static void Prefix(ScaleController __instance, out bool __state)
        {
            __state = CartScaleVisualEffects.BeginScalerCoreDispatch(__instance);
        }

        private static void Postfix(ScaleController __instance, bool __state)
        {
            CartScaleVisualEffects.EndScalerCoreExpandDispatch(__state, __instance);
        }
    }

    [HarmonyPatch(typeof(AssetManager), "PhysImpactEffect")]
    internal static class AssetManagerPhysImpactEffectPatch
    {
        private static bool Prefix()
        {
            return !CartScaleVisualEffects.ShouldSuppressPhysImpactEffect();
        }
    }

    [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
    internal static class RunManagerChangeLevelPatch
    {
        private static void Prefix()
        {
            ShrinkerCartController.RestoreAll();
            CartScaleVisualEffects.Reset();
            VehicleCrushController.RestoreAll();
            EnemyInCartKillController.Reset();
        }
    }
}
