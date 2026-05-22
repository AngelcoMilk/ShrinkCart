using HarmonyLib;

namespace ShrinkCart
{
    [HarmonyPatch(typeof(PhysGrabInCart), "Add")]
    internal static class PhysGrabInCartAddPatch
    {
        private static bool Prefix(PhysGrabInCart __instance, PhysGrabObject _physGrabObject)
        {
            if (!Authority.IsHostOrSingleplayer())
            {
                return true;
            }

            if (CartObjectGuard.ShouldBlockCartInCart(__instance, _physGrabObject))
            {
                CartRegistry.HandleBlockedCartInCart(__instance, _physGrabObject);
                return false;
            }

            return true;
        }

        private static void Postfix(PhysGrabInCart __instance, PhysGrabObject _physGrabObject)
        {
            ShrinkerCartController.ProcessCartObject(__instance, _physGrabObject);
        }
    }

    [HarmonyPatch(typeof(PhysGrabCart), "Start")]
    internal static class PhysGrabCartStartPatch
    {
        private static void Postfix(PhysGrabCart __instance)
        {
            CartRegistry.RegisterCart(__instance);
            if (ModConfig.PlayerScalingEnabled())
            {
                PlayerCartScaleController.RegisterCart(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(PhysGrabCart), "ObjectsInCart")]
    internal static class PhysGrabCartObjectsInCartPatch
    {
        private static void Postfix(PhysGrabCart __instance)
        {
            CartRegistry.CleanCartContents(__instance);
        }
    }

    [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
    internal static class RunManagerChangeLevelPatch
    {
        private static void Prefix()
        {
            ShrinkerCartController.RestoreAll();
            PlayerCartScaleController.RestoreAll();
            PlayerCartScaleController.Reset();
            CartRegistry.Reset();
            ValuableBoxScaleAdapter.Reset();
            EnemyInCartKillController.Reset();
            HostConfigSync.Reset();
        }
    }

}
