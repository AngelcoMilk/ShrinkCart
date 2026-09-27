using System;
using System.Collections.Generic;
using ShrinkCart.Core;
using ShrinkCart.Game;
using UnityEngine;

namespace ShrinkCart.Features
{
    /// <summary>
    /// 把游戏的两类车内信号分发给各功能：
    /// <list type="bullet">
    /// <item>trigger 在场信号：PhysGrabInCart.Add，来自 OnTriggerStay（主机、物理帧频率、每个未休眠物体）。
    /// 已跟踪物体只做一次字典查找即返回；新物体按缓存分类分发。</item>
    /// <item>原版扫描完成：PhysGrabCart.ObjectsInCart 每帧被调用，但原版只在 12 m 内有玩家且计时器到期时才每 0.5 秒扫描一次；
    /// 只有真正扫描后才处理结果，并据此判定离车。</item>
    /// </list>
    /// 功能代码抛出的异常只停用该功能。
    /// </summary>
    internal sealed class CartObservationRouter
    {
        private readonly ModLifecycle _lifecycle;
        private readonly CartRegistry _carts;
        private readonly ObjectClassifier _classifier;
        private readonly ItemShrinkFeature _items;
        private readonly CartNestingGuardFeature _nesting;
        private readonly EnemyCrushFeature _enemies;
        private readonly DeadHeadShrinkFeature _heads;

        internal CartObservationRouter(
            ModLifecycle lifecycle,
            CartRegistry carts,
            ObjectClassifier classifier,
            ItemShrinkFeature items,
            CartNestingGuardFeature nesting,
            EnemyCrushFeature enemies,
            DeadHeadShrinkFeature heads)
        {
            _lifecycle = lifecycle;
            _carts = carts;
            _classifier = classifier;
            _items = items;
            _nesting = nesting;
            _enemies = enemies;
            _heads = heads;
        }

        private bool AnyCartFeatureActive =>
            _lifecycle.IsActive(FeatureId.ItemShrink) ||
            _lifecycle.IsActive(FeatureId.CartNestingGuard) ||
            _lifecycle.IsActive(FeatureId.EnemyCrush) ||
            _lifecycle.IsActive(FeatureId.DeadHeadShrink);

        /// <summary>PhysGrabInCart.Add 前置：返回 true 表示拦截（车类物体不进入车内列表）。</summary>
        internal bool ShouldBlockTriggerAdd(PhysGrabInCart inCart, PhysGrabObject item)
        {
            return inCart != null && inCart.cart != null && _nesting.ShouldBlock(item);
        }

        /// <summary>PhysGrabInCart.Add 之后（未被拦截时）调用。</summary>
        internal void OnTriggerPresence(PhysGrabInCart inCart, PhysGrabObject item)
        {
            DebugCounters.TriggerCalls++;
            if (inCart == null || item == null || !AnyCartFeatureActive)
            {
                return;
            }

            PhysGrabCart cart = inCart.cart;
            if (cart == null)
            {
                return;
            }

            float now = Time.time;
            int cartId = cart.GetInstanceID();
            if (_items.TryMarkSeen(item.gameObject.GetInstanceID(), cartId, now))
            {
                DebugCounters.TriggerFastPath++;
                return;
            }

            Dispatch(item, _classifier.Get(item), cartId, now, false);
        }

        /// <summary>ObjectsInCart 前置：计时器已到期，本次调用可能进行真实扫描。</summary>
        internal bool ShouldObserveScan(PhysGrabCart cart)
        {
            return cart != null && AnyCartFeatureActive && GameAccess.CartCheckTimer(cart) <= 0f;
        }

        /// <summary>ObjectsInCart 之后（前置返回 true 时）调用：计时器被重置说明原版刚完成一次扫描。</summary>
        internal void OnScanMaybeCompleted(PhysGrabCart cart)
        {
            if (GameAccess.CartCheckTimer(cart) <= 0f)
            {
                // 12 m 内没有玩家：原版提前返回，没有扫描，车内对象保持冻结。
                return;
            }

            List<PhysGrabObject> items = GameAccess.ItemsInCart(cart);
            if (items == null)
            {
                return;
            }

            DebugCounters.ScansProcessed++;
            float now = Time.time;
            CartRegistry.CartInfo cartInfo = _carts.GetOrRegister(cart);

            try
            {
                _nesting.CleanScanResult(cart, items);
            }
            catch (Exception ex)
            {
                _lifecycle.ReportFailure(FeatureId.CartNestingGuard, "scan cleanup", ex);
            }

            _items.BeginScan(cartInfo.Id);
            _heads.BeginScan(cartInfo.Id);
            for (int i = 0; i < items.Count; i++)
            {
                PhysGrabObject item = items[i];
                if (item == null)
                {
                    continue;
                }

                int id = item.gameObject.GetInstanceID();
                if (_items.TryMarkSeen(id, cartInfo.Id, now) || _heads.TryMarkSeen(id, cartInfo.Id, now))
                {
                    continue;
                }

                Dispatch(item, _classifier.Get(item), cartInfo.Id, now, true);
            }

            _items.EndScan(cartInfo.Id, now);
            _heads.EndScan(cartInfo.Id, now);
        }

        private void Dispatch(PhysGrabObject item, ObjectInfo info, int cartId, float now, bool fromScan)
        {
            switch (info.Kind)
            {
                case ObjectKind.Enemy:
                    try
                    {
                        _enemies.OnEnemyInCart(info, now);
                    }
                    catch (Exception ex)
                    {
                        _lifecycle.ReportFailure(FeatureId.EnemyCrush, "enemy in cart", ex);
                    }

                    break;

                case ObjectKind.DeathHead:
                    // 头颅只按原版扫描结果处理（与 0.2.x 相同）。
                    if (!fromScan)
                    {
                        break;
                    }

                    try
                    {
                        _heads.OnHeadInCart(item, info, cartId, now);
                    }
                    catch (Exception ex)
                    {
                        _lifecycle.ReportFailure(FeatureId.DeadHeadShrink, "death head in cart", ex);
                    }

                    break;

                case ObjectKind.ShrinkCandidate:
                    try
                    {
                        _items.OnCandidateInCart(item, info, cartId, now);
                    }
                    catch (Exception ex)
                    {
                        _lifecycle.ReportFailure(FeatureId.ItemShrink, "item in cart", ex);
                    }

                    break;
            }
        }
    }
}
