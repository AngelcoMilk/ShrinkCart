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
    /// 车内物品缩小：物品进车（trigger 或原版扫描看到）时缩小；真正离车满防抖时间、被装备或目标销毁时恢复/放弃；
    /// 缩放被 ScalerCore 自行解除（如碰撞恢复）时放弃跟踪并进入重缩冷却，冷却后若仍在车内会再次缩小。
    /// 离车判定见 <see cref="PresenceTracker"/>，到期即恢复；其余周期检查的节奏由「性能」分组配置控制（<see cref="ScanCadence"/>）。
    /// </summary>
    internal sealed class ItemShrinkFeature : IFeature
    {
        private readonly CartRegistry _carts;
        private readonly ObjectClassifier _classifier;
        private readonly ScalerCoreGateway _scaling;
        private readonly PresenceTracker _presence = new PresenceTracker();
        private readonly RetryCooldown _reshrinkCooldown = new RetryCooldown();
        private readonly Dictionary<int, TrackedItem> _items = new Dictionary<int, TrackedItem>();
        private readonly List<int> _ids = new List<int>(16);
        private readonly List<int> _due = new List<int>(16);
        private float _nextMaintenance;
        private bool _active;

        internal ItemShrinkFeature(CartRegistry carts, ObjectClassifier classifier, ScalerCoreGateway scaling)
        {
            _carts = carts;
            _classifier = classifier;
            _scaling = scaling;
        }

        public FeatureId Id => FeatureId.ItemShrink;

        public bool IsConfiguredEnabled => ModConfig.CartShrinkingEnabled.Value;

        internal int TrackedCount => _items.Count;

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
                RestoreAll(now);
            }

            _reshrinkCooldown.Clear();
            _active = false;
        }

        public void ResetForLevel(float now)
        {
            RestoreAll(now);
            _reshrinkCooldown.Clear();
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

        /// <summary>未被跟踪的候选物品出现在车内（trigger 或扫描）。</summary>
        internal void OnCandidateInCart(PhysGrabObject item, ObjectInfo info, int cartId, float now)
        {
            if (!_active || !_classifier.TryGetShrinkFactor(info, out float factor))
            {
                return;
            }

            if (item.dead ||
                (info.Equippable != null && info.Equippable.IsEquipped()) ||
                (item.rb != null && item.rb.isKinematic) ||
                _reshrinkCooldown.IsCooling(info.Id, now))
            {
                return;
            }

            GameObject target = item.gameObject;
            if (_scaling.IsScaled(target))
            {
                // 其他来源（缩小枪、其他 mod）的缩放：不接管、不恢复。
                return;
            }

            bool valuableBox = info.Category == ShrinkCategory.ValuableBox;
            if (valuableBox && !ValuableBoxSupport.EnsureController(target))
            {
                ModLog.Debug("Token/cosmetic box controller not ready, retrying on next observation: " + target.name + " kind=" + SpecialBoxDetector.Describe(item));
                return;
            }

            ScaleOptions options = ScaleOptionsFactory.ForItem(factor, valuableBox);
            if (!_scaling.TryApply(target, options, valuableBox ? ScaleKind.ValuableBox : ScaleKind.Item))
            {
                if (valuableBox)
                {
                    ModLog.Debug("ScalerCore rejected token/cosmetic box shrink: " + target.name + " kind=" + SpecialBoxDetector.Describe(item));
                }

                return;
            }

            bool wasEmpty = _items.Count == 0;
            _items[info.Id] = new TrackedItem(target, info.Equippable);
            _presence.Track(info.Id, cartId, now);
            if (wasEmpty)
            {
                // 空闲时维护间隔可能是最大值；有新物品后尽快按新节奏检查。
                _nextMaintenance = Math.Min(_nextMaintenance, now + CurrentInterval());
            }

            ModLog.Debug("Shrunk " + target.name + " as " + info.Category + " factor=" + factor.ToString("0.###"));
        }

        public void Tick(float now)
        {
            // 离车恢复在"最后一次确认在车内 + 离车防抖延迟"时准时触发（与 0.2.x 手感一致），不等维护间隔。
            float debounce = ModConfig.SafeCartLeaveDebounceSeconds();
            if (_presence.HasDue(now, debounce))
            {
                _presence.CollectDue(now, debounce, _due);
                for (int i = 0; i < _due.Count; i++)
                {
                    Restore(_due[i], now);
                }
            }

            // 其余检查（目标销毁、被外部恢复、装备前恢复）按「性能」分组的维护间隔进行。
            if (now < _nextMaintenance)
            {
                return;
            }

            _nextMaintenance = now + CurrentInterval();
            _reshrinkCooldown.Prune(now);
            if (_items.Count == 0)
            {
                return;
            }

            _ids.Clear();
            foreach (int id in _items.Keys)
            {
                _ids.Add(id);
            }

            for (int i = 0; i < _ids.Count; i++)
            {
                int id = _ids[i];
                TrackedItem tracked = _items[id];
                if (tracked.Target == null)
                {
                    Drop(id);
                    continue;
                }

                if (!_scaling.IsOurs(tracked.Target))
                {
                    ModLog.Debug("Scale was released outside ShrinkCart, re-shrink allowed after cooldown: " + tracked.Target.name);
                    Drop(id);
                    _reshrinkCooldown.Start(id, now, ModConfig.SafeReshrinkCooldownSeconds());
                    continue;
                }

                if (tracked.Equippable != null && tracked.Equippable.IsEquipped())
                {
                    ModLog.Debug("Restoring tracked equippable before equipped use: " + tracked.Target.name);
                    Restore(id, now);
                }
            }
        }

        private float CurrentInterval()
        {
            return ScanCadence.Interval(
                ModConfig.DynamicItemScanEnabledValue(),
                _items.Count,
                ModConfig.SafeMinimumItemScanIntervalSeconds(),
                ModConfig.SafeMaximumItemScanIntervalSeconds());
        }

        private void Restore(int id, float now)
        {
            _presence.Remove(id);
            if (!_items.TryGetValue(id, out TrackedItem tracked))
            {
                return;
            }

            _items.Remove(id);
            _reshrinkCooldown.Start(id, now, ModConfig.SafeReshrinkCooldownSeconds());
            if (tracked.Target == null)
            {
                _scaling.Forget(id);
                return;
            }

            if (_scaling.Restore(tracked.Target, ModConfig.SafeRestoreScaleSpeed()) == RestoreResult.Restored)
            {
                ModLog.Debug("Restored " + tracked.Target.name + " speed=" + ModConfig.SafeRestoreScaleSpeed().ToString("0.###"));
            }
        }

        private void Drop(int id)
        {
            _items.Remove(id);
            _presence.Remove(id);
            _scaling.Forget(id);
        }

        private void RestoreAll(float now)
        {
            _ids.Clear();
            foreach (int id in _items.Keys)
            {
                _ids.Add(id);
            }

            for (int i = 0; i < _ids.Count; i++)
            {
                Restore(_ids[i], now);
            }

            _items.Clear();
            _presence.Clear();
        }

        private void ForgetAll()
        {
            foreach (int id in _items.Keys)
            {
                _scaling.Forget(id);
            }

            _items.Clear();
            _presence.Clear();
        }

        private void OnCartRemoved(int cartId, float now)
        {
            _presence.OnCartRemoved(cartId, now);
        }

        private sealed class TrackedItem
        {
            internal TrackedItem(GameObject target, ItemEquippable equippable)
            {
                Target = target;
                Equippable = equippable;
            }

            internal GameObject Target { get; }

            internal ItemEquippable Equippable { get; }
        }
    }
}
