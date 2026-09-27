using System;
using System.Collections.Generic;
using ScalerCore;
using ShrinkCart.Config;
using ShrinkCart.Core;
using ShrinkCart.Domain;
using ShrinkCart.Game;
using ShrinkCart.Scaling;
using UnityEngine;

namespace ShrinkCart.Features
{
    /// <summary>
    /// 死亡头颅进车缩小。已触发的头颅在原版扫描中出现时缩小；离车满防抖时间、进入提取点、进入卡车、
    /// 不再处于触发状态或复活前恢复。缩放时通过网关临时放行 ScalerCore 的 AllowDeadHeads，不长期改动全局开关。
    /// </summary>
    internal sealed class DeadHeadShrinkFeature : IFeature
    {
        private readonly CartRegistry _carts;
        private readonly ScalerCoreGateway _scaling;
        private readonly PresenceTracker _presence = new PresenceTracker();
        private readonly Dictionary<int, TrackedHead> _heads = new Dictionary<int, TrackedHead>();
        private readonly List<int> _ids = new List<int>(8);
        private readonly List<int> _due = new List<int>(8);
        private float _nextMaintenance;
        private bool _active;

        internal DeadHeadShrinkFeature(CartRegistry carts, ScalerCoreGateway scaling)
        {
            _carts = carts;
            _scaling = scaling;
        }

        public FeatureId Id => FeatureId.DeadHeadShrink;

        public bool IsConfiguredEnabled => ModConfig.DeadHeadScalingEnabledValue();

        public void Activate(float now)
        {
            _active = true;
            _nextMaintenance = now;
            _carts.CartRemoved += OnCartRemoved;
        }

        public void Deactivate(DeactivateReason reason, float now)
        {
            _carts.CartRemoved -= OnCartRemoved;
            if (reason == DeactivateReason.AuthorityLost)
            {
                ForgetAll();
            }
            else
            {
                RestoreAll("deactivate " + reason);
            }

            _active = false;
        }

        public void ResetForLevel(float now)
        {
            RestoreAll("level change");
        }

        internal bool TryMarkSeen(int id, int cartId, float now)
        {
            return _active && _presence.TryMarkSeen(id, cartId, now);
        }

        internal void BeginScan(int cartId)
        {
            if (_active)
            {
                _presence.BeginScan(cartId);
            }
        }

        internal void EndScan(int cartId, float now)
        {
            if (_active)
            {
                _presence.EndScan(cartId, now);
            }
        }

        internal void OnHeadInCart(PhysGrabObject item, ObjectInfo info, int cartId, float now)
        {
            if (!_active)
            {
                return;
            }

            PlayerDeathHead head = info.Head;
            if (head == null || !GameAccess.IsHeadTriggered(head) || GameAccess.HeadPhysGrabObject(head) != item)
            {
                return;
            }

            GameObject target = item.gameObject;
            if (_scaling.IsScaled(target))
            {
                ModLog.Debug("Skipped death head cart shrink because another scale session is active: " + target.name);
                return;
            }

            ScaleOptions options = ScaleOptionsFactory.ForDeadHead();
            if (!_scaling.TryApply(target, options, ScaleKind.DeadHead))
            {
                ModLog.Debug("ScalerCore rejected death head cart shrink: " + target.name);
                return;
            }

            bool wasEmpty = _heads.Count == 0;
            _heads[info.Id] = new TrackedHead(target, head);
            _presence.Track(info.Id, cartId, now);
            if (wasEmpty)
            {
                _nextMaintenance = Math.Min(_nextMaintenance, now + CurrentInterval());
            }

            ModLog.Debug("Shrunk death head in cart: " + target.name + " factor=" + options.Factor.ToString("0.###"));
        }

        /// <summary>PlayerDeathHead.Revive 之前调用：复活前先恢复原尺寸。</summary>
        internal void RestoreBeforeRevive(PlayerDeathHead head)
        {
            if (!_active || head == null)
            {
                return;
            }

            PhysGrabObject item = GameAccess.HeadPhysGrabObject(head);
            if (item == null)
            {
                return;
            }

            int id = item.gameObject.GetInstanceID();
            if (_heads.ContainsKey(id))
            {
                Restore(id, "before revive");
            }
        }

        public void Tick(float now)
        {
            // 离车恢复到期即触发（与 0.2.x 手感一致），不等维护间隔。
            float debounce = ModConfig.SafeCartLeaveDebounceSeconds();
            if (_presence.HasDue(now, debounce))
            {
                _presence.CollectDue(now, debounce, _due);
                for (int i = 0; i < _due.Count; i++)
                {
                    Restore(_due[i], "left cart");
                }
            }

            // 其余检查（目标销毁、被外部恢复、提取点/卡车/未触发）按「性能」分组的维护间隔进行。
            if (now < _nextMaintenance)
            {
                return;
            }

            _nextMaintenance = now + CurrentInterval();
            if (_heads.Count == 0)
            {
                return;
            }

            _ids.Clear();
            foreach (int id in _heads.Keys)
            {
                _ids.Add(id);
            }

            for (int i = 0; i < _ids.Count; i++)
            {
                int id = _ids[i];
                TrackedHead tracked = _heads[id];
                if (tracked.Target == null || tracked.Head == null)
                {
                    Drop(id);
                }
                else if (!_scaling.IsOurs(tracked.Target))
                {
                    ModLog.Debug("Death head scale was released outside ShrinkCart: " + tracked.Target.name);
                    Drop(id);
                }
                else if (!GameAccess.IsHeadTriggered(tracked.Head) ||
                         GameAccess.IsHeadInExtractionPoint(tracked.Head) ||
                         GameAccess.IsHeadInTruck(tracked.Head))
                {
                    Restore(id, "extraction/truck/untriggered");
                }
            }
        }

        private float CurrentInterval()
        {
            return ScanCadence.Interval(
                ModConfig.DynamicItemScanEnabledValue(),
                _heads.Count,
                ModConfig.SafeMinimumItemScanIntervalSeconds(),
                ModConfig.SafeMaximumItemScanIntervalSeconds());
        }

        private void Restore(int id, string reason)
        {
            _presence.Remove(id);
            if (!_heads.TryGetValue(id, out TrackedHead tracked))
            {
                return;
            }

            _heads.Remove(id);
            if (tracked.Target == null)
            {
                _scaling.Forget(id);
                return;
            }

            if (_scaling.Restore(tracked.Target, ModConfig.SafeRestoreScaleSpeed()) == RestoreResult.Restored)
            {
                ModLog.Debug("Restored death head " + tracked.Target.name + " reason=" + reason);
            }
        }

        private void Drop(int id)
        {
            _heads.Remove(id);
            _presence.Remove(id);
            _scaling.Forget(id);
        }

        private void RestoreAll(string reason)
        {
            _ids.Clear();
            foreach (int id in _heads.Keys)
            {
                _ids.Add(id);
            }

            for (int i = 0; i < _ids.Count; i++)
            {
                Restore(_ids[i], reason);
            }

            _heads.Clear();
            _presence.Clear();
        }

        private void ForgetAll()
        {
            foreach (int id in _heads.Keys)
            {
                _scaling.Forget(id);
            }

            _heads.Clear();
            _presence.Clear();
        }

        private void OnCartRemoved(int cartId, float now)
        {
            _presence.OnCartRemoved(cartId, now);
        }

        private sealed class TrackedHead
        {
            internal TrackedHead(GameObject target, PlayerDeathHead head)
            {
                Target = target;
                Head = head;
            }

            internal GameObject Target { get; }

            internal PlayerDeathHead Head { get; }
        }
    }
}
