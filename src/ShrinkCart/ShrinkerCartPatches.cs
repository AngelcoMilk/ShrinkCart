using HarmonyLib;

namespace ShrinkCart
{
    [HarmonyPatch(typeof(PhysGrabInCart), "Add")]
    internal static class PhysGrabInCartAddPatch
    {
        private static bool Prefix(PhysGrabInCart __instance, PhysGrabObject _physGrabObject)
        {
            if (CartObjectGuard.ShouldBlockCartInCart(__instance, _physGrabObject))
            {
                CartCollisionGuard.HandleBlockedCartInCart(__instance, _physGrabObject);
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
            PlayerCartScaleController.RegisterCart(__instance);
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

    [HarmonyPatch(typeof(RunManager), "ChangeLevel")]
    internal static class RunManagerChangeLevelPatch
    {
        private static void Prefix()
        {
            ShrinkerCartController.RestoreAll();
            PlayerCartScaleController.RestoreAll();
            PlayerCartScaleController.Reset();
            CartCollisionGuard.Reset();
            VehicleCrushController.RestoreAll();
            EnemyInCartKillController.Reset();
            HostConfigSync.Reset();
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "PlayerDeath")]
    internal static class PlayerAvatarPlayerDeathPatch
    {
        private static void Prefix(PlayerAvatar __instance)
        {
            PlayerScaleDeathSafety.RestoreBeforeDeath(__instance, "PlayerDeath");
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "PlayerDeathRPC")]
    internal static class PlayerAvatarPlayerDeathRpcPatch
    {
        private static void Prefix(PlayerAvatar __instance)
        {
            PlayerScaleDeathSafety.RestoreBeforeDeath(__instance, "PlayerDeathRPC");
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "Revive")]
    internal static class PlayerAvatarRevivePatch
    {
        private static void Prefix(PlayerAvatar __instance)
        {
            PlayerScaleDeathSafety.ClearBeforeRevive(__instance, "Revive");
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), "ReviveRPC")]
    internal static class PlayerAvatarReviveRpcPatch
    {
        private static void Prefix(PlayerAvatar __instance)
        {
            PlayerScaleDeathSafety.ClearBeforeRevive(__instance, "ReviveRPC");
        }
    }
}
