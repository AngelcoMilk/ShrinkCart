using System.Collections.Generic;
using ShrinkCart.Config;
using ShrinkCart.Core;
using ShrinkCart.Domain;
using ShrinkCart.Game;
using ShrinkCart.Scaling;
using UnityEngine;

namespace ShrinkCart.Features
{
    /// <summary>
    /// 玩家站车循环（原样 → 缩小 → 原样 → 增大 → 原样）。主机按「玩家检测间隔」检测每个玩家是否站在
    /// 正式购物车（排除小推车）底面投影的中心区域，状态机见 <see cref="PlayerCycle"/>。
    /// 区域几何与 0.2.x 相同：使用原版扫描所用的 inCart 体积，中心区域为水平方向 45%，并有最小半宽。
    /// </summary>
    internal sealed class PlayerScalingFeature : IFeature
    {
        private const float CenterZoneHorizontalScale = 0.45f;
        private const float FloorProjectionPaddingBelow = 0.25f;
        private const float FloorProjectionStandingHeightAbove = 1.4f;
        private const float MinimumCenterHalfExtent = 0.15f;
        private const float StandPointYOffset = 0.05f;

        private readonly CartRegistry _carts;
        private readonly ScalerCoreGateway _scaling;
        private readonly Dictionary<int, PlayerEntry> _players = new Dictionary<int, PlayerEntry>();
        private readonly List<int> _removeIds = new List<int>(8);
        private float _nextDetection;

        internal PlayerScalingFeature(CartRegistry carts, ScalerCoreGateway scaling)
        {
            _carts = carts;
            _scaling = scaling;
        }

        public FeatureId Id => FeatureId.PlayerScaling;

        public bool IsConfiguredEnabled => ModConfig.PlayerScalingEnabled();

        public void Activate(float now)
        {
            _nextDetection = now;
        }

        public void Deactivate(DeactivateReason reason, float now)
        {
            if (reason == DeactivateReason.AuthorityLost)
            {
                ForgetAll();
            }
            else
            {
                RestoreAll();
            }
        }

        public void ResetForLevel(float now)
        {
            RestoreAll();
        }

        public void Tick(float now)
        {
            if (now < _nextDetection)
            {
                return;
            }

            _nextDetection = now + ModConfig.SafePlayerCartDetectionIntervalSeconds();
            List<PlayerAvatar> players = GetPlayers();
            if (players == null)
            {
                return;
            }

            float exitGrace = ModConfig.SafePlayerCartExitGraceSeconds();
            float standTrigger = ModConfig.SafePlayerCartStandTriggerSeconds();
            for (int i = 0; i < players.Count; i++)
            {
                ProcessPlayer(players[i], now, exitGrace, standTrigger);
            }

            PruneMissingPlayers();
        }

        private void ProcessPlayer(PlayerAvatar player, float now, float exitGrace, float standTrigger)
        {
            if (player == null || !player.gameObject.activeInHierarchy)
            {
                return;
            }

            int id = player.GetInstanceID();
            if (!_players.TryGetValue(id, out PlayerEntry entry))
            {
                entry = new PlayerEntry(player);
                _players[id] = entry;
            }

            GameObject target = player.gameObject;
            if (entry.Cycle.State != PlayerScaleState.Normal && !_scaling.IsOurs(target))
            {
                // 受伤/碰撞恢复或其他来源接管：视为已恢复，循环照常推进。
                entry.Cycle.OnExternalRestore();
                _scaling.Forget(entry.TargetId);
                ModLog.Debug("Player scale was released outside ShrinkCart: " + player.name + " nextIsGrow=" + entry.Cycle.NextIsGrow);
            }

            GetPlayerCartZone(player, out bool inRange, out bool inZone);
            PlayerCycleAction action = entry.Cycle.Step(inRange, inZone, now, exitGrace, standTrigger);
            switch (action)
            {
                case PlayerCycleAction.Restore:
                    RestoreResult result = _scaling.Restore(target, ModConfig.SafeRestoreScaleSpeed());
                    if (result == RestoreResult.Restored)
                    {
                        entry.Cycle.OnRestored();
                        ModLog.Debug("Restored player after standing in cart center: " + player.name + " nextIsGrow=" + entry.Cycle.NextIsGrow);
                    }
                    else if (result == RestoreResult.NotOwned)
                    {
                        entry.Cycle.OnExternalRestore();
                    }

                    break;

                case PlayerCycleAction.ApplyShrink:
                case PlayerCycleAction.ApplyGrow:
                    if (_scaling.IsScaled(target))
                    {
                        ModLog.Debug("Skipped player cart scale because another scale session is active: " + player.name);
                        break;
                    }

                    float factor = action == PlayerCycleAction.ApplyGrow
                        ? ModConfig.SafePlayerCartGrowFactor()
                        : ModConfig.SafePlayerCartScaleFactor();
                    if (_scaling.TryApply(target, ScaleOptionsFactory.ForPlayer(factor), ScaleKind.Player))
                    {
                        entry.Cycle.OnApplied(action);
                        ModLog.Debug("Applied player cart scale: " + player.name + " state=" + entry.Cycle.State + " factor=" + factor.ToString("0.###"));
                    }
                    else
                    {
                        ModLog.Debug("ScalerCore rejected player cart scale: " + player.name + " action=" + action);
                    }

                    break;
            }
        }

        private void GetPlayerCartZone(PlayerAvatar player, out bool inRange, out bool inZone)
        {
            inRange = false;
            inZone = false;
            Vector3 standPoint = GetPlayerStandPoint(player);
            foreach (CartRegistry.CartInfo cart in _carts.All)
            {
                if (cart.IsSmall || cart.Cart == null)
                {
                    continue;
                }

                Transform volume = cart.InCartVolume;
                if (volume == null)
                {
                    volume = GameAccess.InCartVolume(cart.Cart);
                    cart.InCartVolume = volume;
                    if (volume == null)
                    {
                        continue;
                    }
                }

                if (!IsPointInsideCartFloorProjection(standPoint, volume, 1.0f))
                {
                    continue;
                }

                inRange = true;
                if (IsPointInsideCartFloorProjection(standPoint, volume, CenterZoneHorizontalScale))
                {
                    inZone = true;
                    return;
                }
            }
        }

        private static bool IsPointInsideCartFloorProjection(Vector3 point, Transform volume, float horizontalScale)
        {
            Vector3 local = Quaternion.Inverse(volume.rotation) * (point - volume.position);
            Vector3 half = volume.localScale * 0.5f;
            float centerHalfX = Mathf.Max(Mathf.Abs(half.x) * horizontalScale, MinimumCenterHalfExtent);
            float centerHalfZ = Mathf.Max(Mathf.Abs(half.z) * horizontalScale, MinimumCenterHalfExtent);
            float floorY = -Mathf.Abs(half.y);

            return Mathf.Abs(local.x) <= centerHalfX &&
                   Mathf.Abs(local.z) <= centerHalfZ &&
                   local.y >= floorY - FloorProjectionPaddingBelow &&
                   local.y <= floorY + FloorProjectionStandingHeightAbove;
        }

        private static Vector3 GetPlayerStandPoint(PlayerAvatar player)
        {
            Collider collider = GameAccess.AvatarCollider(player);
            if (collider != null)
            {
                Bounds bounds = collider.bounds;
                return new Vector3(bounds.center.x, bounds.min.y + StandPointYOffset, bounds.center.z);
            }

            Vector3 position = player.playerTransform != null ? player.playerTransform.position : player.transform.position;
            return position + Vector3.up * StandPointYOffset;
        }

        // 场景切换期间 GameDirector 可能暂不可用：跳过本次检测，而不是让功能因异常被停用。
        private static List<PlayerAvatar> GetPlayers()
        {
            try
            {
                if (GameDirector.instance != null && GameDirector.instance.PlayerList != null)
                {
                    return GameDirector.instance.PlayerList;
                }

                return SemiFunc.PlayerGetAll();
            }
            catch (System.Exception ex)
            {
                ModLog.Debug("Player list unavailable this tick: " + ex.Message);
                return null;
            }
        }

        private void RestoreAll()
        {
            foreach (PlayerEntry entry in _players.Values)
            {
                if (entry.Cycle.State != PlayerScaleState.Normal && entry.Player != null)
                {
                    _scaling.Restore(entry.Player.gameObject, ModConfig.SafeRestoreScaleSpeed());
                }
            }

            _players.Clear();
        }

        private void ForgetAll()
        {
            foreach (PlayerEntry entry in _players.Values)
            {
                _scaling.Forget(entry.TargetId);
            }

            _players.Clear();
        }

        // 离开房间的玩家对象已销毁：放弃其所有权记录（用预先记下的实例 ID，销毁后无法再取到）。
        private void PruneMissingPlayers()
        {
            _removeIds.Clear();
            foreach (KeyValuePair<int, PlayerEntry> pair in _players)
            {
                if (pair.Value.Player == null)
                {
                    _removeIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < _removeIds.Count; i++)
            {
                _scaling.Forget(_players[_removeIds[i]].TargetId);
                _players.Remove(_removeIds[i]);
            }
        }

        private sealed class PlayerEntry
        {
            internal PlayerEntry(PlayerAvatar player)
            {
                Player = player;
                TargetId = player.gameObject.GetInstanceID();
            }

            internal PlayerAvatar Player { get; }

            /// <summary>缩放目标（玩家 GameObject）的实例 ID，即所有权记录的键。</summary>
            internal int TargetId { get; }

            internal PlayerCycle Cycle { get; } = new PlayerCycle();
        }
    }
}
