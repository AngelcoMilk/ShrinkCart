using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShrinkCart
{
    internal static class EnemyInCartKillController
    {
        private static readonly HashSet<int> ExecutedEnemies = new HashSet<int>();
        private static readonly FieldInfo EnemyHealthField =
            typeof(Enemy).GetField("Health", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo EnemyHealthCurrentField =
            typeof(EnemyHealth).GetField("healthCurrent", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void Reset()
        {
            ExecutedEnemies.Clear();
        }

        internal static bool TryKill(PhysGrabObject item)
        {
            if (!ModConfig.EnemyInCartInstantKill.Value || item == null)
            {
                return false;
            }

            Enemy enemy = FindEnemy(item);
            if (enemy == null)
            {
                return false;
            }

            int id = enemy.GetInstanceID();
            if (ExecutedEnemies.Contains(id))
            {
                return true;
            }

            EnemyHealth health = GetHealth(enemy);
            if (health == null)
            {
                return false;
            }

            int currentHealth = GetCurrentHealth(health);
            if (currentHealth <= 0)
            {
                ExecutedEnemies.Add(id);
                return true;
            }

            ExecutedEnemies.Add(id);

            try
            {
                int damage = Math.Max(currentHealth, health.health) + 1;
                health.Hurt(damage, Vector3.up);
                DebugLog("Instant killed enemy entering cart: " + enemy.name);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to instant kill enemy entering cart: " + ex.Message);
                ExecutedEnemies.Remove(id);
                return false;
            }

            return true;
        }

        private static Enemy FindEnemy(PhysGrabObject item)
        {
            EnemyRigidbody enemyRigidbody = item.GetComponentInParent<EnemyRigidbody>();
            if (enemyRigidbody != null && enemyRigidbody.enemy != null)
            {
                return enemyRigidbody.enemy;
            }

            return item.GetComponentInParent<Enemy>();
        }

        private static EnemyHealth GetHealth(Enemy enemy)
        {
            if (EnemyHealthField == null)
            {
                return null;
            }

            return EnemyHealthField.GetValue(enemy) as EnemyHealth;
        }

        private static int GetCurrentHealth(EnemyHealth health)
        {
            if (EnemyHealthCurrentField == null)
            {
                return health.health;
            }

            object value = EnemyHealthCurrentField.GetValue(health);
            if (value is int)
            {
                return (int)value;
            }

            return health.health;
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
