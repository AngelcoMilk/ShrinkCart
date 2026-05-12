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
