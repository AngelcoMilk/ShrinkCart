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
    /// <para>可乘坐的贵重物（SemiMounting）：本地玩家乘坐时模型可能脱离根节点，下车归位时按世界尺寸换算，
    /// 这期间根节点尺寸一变，模型比例就会永久错乱。所以有人坐着不缩小，下车后等模型归位再缩小；
    /// 缩放及恢复动画期间禁止乘坐。主机决定谁能上座，只在主机设置即可。</para>
    /// </summary>
    internal sealed class ItemShrinkFeature : IFeature
    {
        private const float MountSettleSeconds = 1.5f;
        private const float MountHoldMarginSeconds = 0.25f;

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

            if (info.Mounting != null && info.Mounting.AnyoneSeated())
            {
                _reshrinkCooldown.Start(info.Id, now, MountSettleSeconds);
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
            TrackedItem tracked = new TrackedItem(target, info.Equippable, info.Mounting, factor);
            _items[info.Id] = tracked;
            _presence.Track(info.Id, cartId, now);
            if (wasEmpty)
            {
                // 空闲时维护间隔可能是最大值；有新物品后尽快按新节奏检查。
                _nextMaintenance = Math.Min(_nextMaintenance, now + CurrentInterval());
            }

            HoldMountingUntilNextCheck(tracked, now);
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

            // 其余检查（目标销毁、被外部恢复、装备前恢复、禁止乘坐）按「性能」分组的维护间隔进行。
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
                    HoldMountingWhileRestoring(tracked);
                    Drop(id);
                    _reshrinkCooldown.Start(id, now, ModConfig.SafeReshrinkCooldownSeconds());
                    continue;
                }

                if (tracked.Equippable != null && tracked.Equippable.IsEquipped())
                {
                    ModLog.Debug("Restoring tracked equippable before equipped use: " + tracked.Target.name);
                    Restore(id, now);
                    continue;
                }

                HoldMountingUntilNextCheck(tracked, now);
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
                HoldMountingWhileRestoring(tracked);
                ModLog.Debug("Restored " + tracked.Target.name + " speed=" + ModConfig.SafeRestoreScaleSpeed().ToString("0.###"));
            }
        }

        private void HoldMountingUntilNextCheck(TrackedItem tracked, float now)
        {
            if (tracked.Mounting != null)
            {
                tracked.Mounting.OverrideDisableMounting(_nextMaintenance - now + MountHoldMarginSeconds);
            }
        }

        // ScalerCore 的恢复动画时长 = (1 - 倍率) / 恢复速度。
        private static void HoldMountingWhileRestoring(TrackedItem tracked)
        {
            if (tracked.Mounting != null)
            {
                tracked.Mounting.OverrideDisableMounting(
                    (1f - tracked.Factor) / ModConfig.SafeRestoreScaleSpeed() + MountHoldMarginSeconds);
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
            internal TrackedItem(GameObject target, ItemEquippable equippable, SemiMounting mounting, float factor)
            {
                Target = target;
                Equippable = equippable;
                Mounting = mounting;
                Factor = factor;
            }

            internal GameObject Target { get; }

            internal ItemEquippable Equippable { get; }

            internal SemiMounting Mounting { get; }

            internal float Factor { get; }
        }
    }
}
