using System.Collections.Generic;

namespace ShrinkCart
{
    internal static class VehicleCrushController
    {
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

        internal static void RestoreAll()
        {
            foreach (HurtColliderState state in States.Values)
            {
                Restore(state);
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
            if (!ModConfig.VehicleCrushInstantKill.Value)
            {
                Restore(state);
                return;
            }

            collider.playerLogic = true;
            collider.playerKill = true;

            if (ModConfig.VehicleCrushKillEnemies.Value)
            {
                collider.enemyLogic = true;
                collider.enemyKill = true;
            }
            else
            {
                collider.enemyLogic = state.EnemyLogic;
                collider.enemyKill = state.EnemyKill;
                collider.enemyDamage = state.EnemyDamage;
            }
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

        private static void Restore(HurtColliderState state)
        {
            if (state == null || state.Collider == null)
            {
                return;
            }

            state.Collider.playerLogic = state.PlayerLogic;
            state.Collider.playerKill = state.PlayerKill;
            state.Collider.playerDamage = state.PlayerDamage;
            state.Collider.enemyLogic = state.EnemyLogic;
            state.Collider.enemyKill = state.EnemyKill;
            state.Collider.enemyDamage = state.EnemyDamage;
        }
    }
}
