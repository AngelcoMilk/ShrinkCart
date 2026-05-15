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
            CartRegistry.RegisterCart(__instance);
            PlayerCartScaleController.RegisterCart(__instance);
        }
    }

    [HarmonyPatch(typeof(PhysGrabCart), "ObjectsInCart")]
    internal static class PhysGrabCartObjectsInCartPatch
    {
        private static void Postfix(PhysGrabCart __instance)
        {
            CartCollisionGuard.CleanCartContents(__instance);
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
            PlayerScaleDeathSafety.ClearAll();
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

    [HarmonyPatch(typeof(PlayerHealth), "Death")]
    internal static class PlayerHealthDeathPatch
    {
        private static void Prefix(PlayerHealth __instance)
        {
            PlayerScaleDeathSafety.RestoreBeforeDeath(__instance == null ? null : __instance.GetComponent<PlayerAvatar>(), "PlayerHealth.Death");
        }
    }

    [HarmonyPatch(typeof(PlayerDeathHead), "Trigger")]
    internal static class PlayerDeathHeadTriggerPatch
    {
        private static void Prefix(PlayerDeathHead __instance)
        {
            PlayerScaleDeathSafety.RestoreBeforeDeath(__instance == null ? null : __instance.playerAvatar, "PlayerDeathHead.Trigger");
        }
    }

}
