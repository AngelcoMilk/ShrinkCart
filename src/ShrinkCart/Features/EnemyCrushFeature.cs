using System;
using ShrinkCart.Config;
using ShrinkCart.Core;
using ShrinkCart.Domain;
using ShrinkCart.Game;
using UnityEngine;

namespace ShrinkCart.Features
{
    /// <summary>
    /// 敌人进车秒杀。敌人死亡后由 EnemyParent 复用同一实例重生（EnemyHealth.OnSpawn 重置 healthCurrent/dead），
    /// 因此只用 1 秒的重试冷却防止同一次死亡过程中重复造成伤害，重生后再次进车照样会被秒杀。
    /// EnemyHealth.Hurt 是游戏自己的联机伤害入口，由主机调用。
    /// </summary>
    internal sealed class EnemyCrushFeature : IFeature
    {
        private const float KillRetryCooldownSeconds = 1.0f;
        private const float PruneIntervalSeconds = 5.0f;

        private readonly RetryCooldown _retry = new RetryCooldown();
        private float _nextPrune;
        private bool _active;

        public FeatureId Id => FeatureId.EnemyCrush;

        public bool IsConfiguredEnabled => ModConfig.EnemyInCartInstantKill.Value;

        public void Activate(float now)
        {
            _active = true;
        }

        public void Deactivate(DeactivateReason reason, float now)
        {
            _active = false;
            _retry.Clear();
        }

        public void ResetForLevel(float now)
        {
            _retry.Clear();
        }

        public void Tick(float now)
        {
            if (now < _nextPrune)
            {
                return;
            }

            _nextPrune = now + PruneIntervalSeconds;
            _retry.Prune(now);
        }

        internal void OnEnemyInCart(ObjectInfo info, float now)
        {
            if (!_active)
            {
                return;
            }

            Enemy enemy = info.Enemy;
            if (enemy == null)
            {
                return;
            }

            int id = enemy.GetInstanceID();
            if (_retry.IsCooling(id, now))
            {
                return;
            }

            EnemyHealth health = GameAccess.HealthOf(enemy);
            if (health == null)
            {
                return;
            }

            int currentHealth = GameAccess.CurrentHealth(health);
            if (GameAccess.IsDead(health) || currentHealth <= 0)
            {
                return;
            }

            _retry.Start(id, now, KillRetryCooldownSeconds);
            try
            {
                health.Hurt(Math.Max(currentHealth, health.health) + 1, Vector3.up);
                ModLog.Debug("Instant killed enemy entering cart: " + enemy.name);
            }
            catch (Exception ex)
            {
                _retry.Cancel(id);
                ModLog.WarnOnce("enemy-crush", "Failed to instant kill enemy entering cart: " + ex.Message);
            }
        }
    }
}
