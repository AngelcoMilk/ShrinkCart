using HarmonyLib;

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

    [HarmonyPatch(typeof(ItemVehicle), "Update")]
    internal static class ItemVehicleUpdatePatch
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

    [HarmonyPatch(typeof(HurtCollider), "EnemyHurt")]
    internal static class HurtColliderEnemyHurtPatch
    {
        private static void Prefix(HurtCollider __instance, out VehicleCrushController.TemporaryHurtState __state)
        {
            __state = VehicleCrushController.BeforeEnemyHurt(__instance);
        }

        private static void Postfix(HurtCollider __instance, VehicleCrushController.TemporaryHurtState __state)
        {
            VehicleCrushController.AfterHurt(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
    internal static class RunManagerChangeLevelPatch
    {
        private static void Prefix()
        {
            ShrinkerCartController.RestoreAll();
            VehicleCrushController.RestoreAll();
        }
    }
}
