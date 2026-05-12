using System.Collections.Generic;

namespace ShrinkCart
{
    internal static class VehicleCrushController
    {
        internal sealed class TemporaryHurtState
        {
            internal bool PlayerLogic;
            internal bool PlayerKill;
            internal int PlayerDamage;
            internal bool EnemyLogic;
            internal bool EnemyKill;
            internal int EnemyDamage;
        }

        private sealed class HurtColliderState
        {
            internal HurtCollider Collider;
            internal bool PlayerLogic;
            internal bool PlayerKill;
            internal int PlayerDamage;
            internal bool EnemyLogic;
            internal bool EnemyKill;
            internal int EnemyDamage;
        }

        private static readonly Dictionary<int, HurtColliderState> States =
            new Dictionary<int, HurtColliderState>();

        internal static void Configure(ItemVehicle vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            ConfigureCollider(vehicle.hurtColliderSmall);
            ConfigureCollider(vehicle.hurtColliderMedium);
            ConfigureCollider(vehicle.hurtColliderBig);
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

        internal static TemporaryHurtState BeforeEnemyHurt(HurtCollider collider)
        {
            if (collider == null || !ModConfig.VehicleCrushKillEnemies.Value || !IsVehicleCollider(collider))
            {
                return null;
            }

            TemporaryHurtState state = Capture(collider);
            collider.enemyLogic = true;
            collider.enemyKill = true;
            DebugLog("Vehicle crush enemy kill armed.");
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
            collider.enemyLogic = state.EnemyLogic;
            collider.enemyKill = state.EnemyKill;
            collider.enemyDamage = state.EnemyDamage;
        }

        internal static void RestoreAll()
        {
            foreach (HurtColliderState state in States.Values)
            {
                RestoreAllFields(state);
            }

            States.Clear();
        }

        private static void ConfigureCollider(HurtCollider collider)
        {
            if (collider == null)
            {
                return;
            }

            HurtColliderState state = GetOrCreateState(collider);

            if (ModConfig.VehicleCrushInstantKill.Value)
            {
                collider.playerLogic = true;
                collider.playerKill = true;
            }
            else
            {
                RestorePlayerFields(state);
            }

            if (ModConfig.VehicleCrushKillEnemies.Value)
            {
                collider.enemyLogic = true;
                collider.enemyKill = true;
            }
            else
            {
                RestoreEnemyFields(state);
            }
        }

        private static bool IsVehicleCollider(HurtCollider collider)
        {
            return States.ContainsKey(collider.GetInstanceID());
        }

        private static HurtColliderState GetOrCreateState(HurtCollider collider)
        {
            int id = collider.GetInstanceID();
            HurtColliderState state;
            if (States.TryGetValue(id, out state))
            {
                return state;
            }

            state = new HurtColliderState
            {
                Collider = collider,
                PlayerLogic = collider.playerLogic,
                PlayerKill = collider.playerKill,
                PlayerDamage = collider.playerDamage,
                EnemyLogic = collider.enemyLogic,
                EnemyKill = collider.enemyKill,
                EnemyDamage = collider.enemyDamage
            };

            States[id] = state;
            return state;
        }

        private static TemporaryHurtState Capture(HurtCollider collider)
        {
            return new TemporaryHurtState
            {
                PlayerLogic = collider.playerLogic,
                PlayerKill = collider.playerKill,
                PlayerDamage = collider.playerDamage,
                EnemyLogic = collider.enemyLogic,
                EnemyKill = collider.enemyKill,
                EnemyDamage = collider.enemyDamage
            };
        }

        private static void RestoreAllFields(HurtColliderState state)
        {
            RestorePlayerFields(state);
            RestoreEnemyFields(state);
        }

        private static void RestorePlayerFields(HurtColliderState state)
        {
            if (state == null || state.Collider == null)
            {
                return;
            }

            state.Collider.playerLogic = state.PlayerLogic;
            state.Collider.playerKill = state.PlayerKill;
            state.Collider.playerDamage = state.PlayerDamage;
        }

        private static void RestoreEnemyFields(HurtColliderState state)
        {
            if (state == null || state.Collider == null)
            {
                return;
            }

            state.Collider.enemyLogic = state.EnemyLogic;
            state.Collider.enemyKill = state.EnemyKill;
            state.Collider.enemyDamage = state.EnemyDamage;
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
