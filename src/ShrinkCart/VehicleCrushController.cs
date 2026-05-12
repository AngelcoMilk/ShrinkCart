namespace ShrinkCart
{
    internal static class VehicleCrushController
    {
        internal sealed class TemporaryHurtState
        {
            internal bool PlayerLogic;
            internal bool PlayerKill;
            internal int PlayerDamage;
        }

        internal static TemporaryHurtState BeforePlayerHurt(HurtCollider collider)
        {
            if (collider == null || !ModConfig.VehicleCrushInstantKill.Value || !IsVehicleCollider(collider))
            {
                return null;
            }

            TemporaryHurtState state = Capture(collider);
            collider.playerLogic = true;
            collider.playerKill = true;
            DebugLog("Vehicle crush player kill armed.");
            return state;
        }

        internal static void AfterHurt(HurtCollider collider, TemporaryHurtState state)
        {
            if (collider == null || state == null)
            {
                return;
            }

            collider.playerLogic = state.PlayerLogic;
            collider.playerKill = state.PlayerKill;
            collider.playerDamage = state.PlayerDamage;
        }

        internal static void RestoreAll()
        {
        }

        private static bool IsVehicleCollider(HurtCollider collider)
        {
            return collider.GetComponentInParent<ItemVehicle>() != null ||
                   collider.GetComponentInParent<PhysGrabCart>() != null;
        }

        private static TemporaryHurtState Capture(HurtCollider collider)
        {
            return new TemporaryHurtState
            {
                PlayerLogic = collider.playerLogic,
                PlayerKill = collider.playerKill,
                PlayerDamage = collider.playerDamage
            };
        }

        private static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging.Value)
            {
                Plugin.Log.LogInfo(message);
            }
        }
    }
}
