using System;
using System.Collections.Generic;
using ShrinkCart.Config;
using ShrinkCart.Core;
using ShrinkCart.Game;

namespace ShrinkCart.Features
{
    /// <summary>
    /// 车类物品拦截：车类物体（购物车、小推车、载具、车载炮/激光）不进入另一辆车的 trigger 车内列表，
    /// 也从原版扫描结果 itemsInCart 中移除，并按差值修正 itemsInCartCount / haulCurrent 与价格屏。
    /// 差值按原版规则计算（(int)dollarValueCurrent），不再复制原版整套统计逻辑。
    /// </summary>
    internal sealed class CartNestingGuardFeature : IFeature
    {
        private readonly ObjectClassifier _classifier;
        private bool _active;

        internal CartNestingGuardFeature(ObjectClassifier classifier)
        {
            _classifier = classifier;
        }

        public FeatureId Id => FeatureId.CartNestingGuard;

        public bool IsConfiguredEnabled => ModConfig.CartNestingGuardEnabledValue();

        internal bool IsActive => _active;

        public void Activate(float now)
        {
            _active = true;
        }

        public void Deactivate(DeactivateReason reason, float now)
        {
            _active = false;
        }

        public void ResetForLevel(float now)
        {
        }

        public void Tick(float now)
        {
        }

        /// <summary>PhysGrabInCart.Add 的前置判断：返回 true 表示应跳过原版 Add。</summary>
        internal bool ShouldBlock(PhysGrabObject item)
        {
            return _active && item != null && _classifier.Get(item).Kind == ObjectKind.CartLike;
        }

        /// <summary>原版完成一次真实扫描后调用：移除车类物体并修正计数与总价。</summary>
        internal void CleanScanResult(PhysGrabCart cart, List<PhysGrabObject> items)
        {
            if (!_active)
            {
                return;
            }

            int removed = 0;
            int removedHaul = 0;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                PhysGrabObject item = items[i];
                if (item == null || _classifier.Get(item).Kind != ObjectKind.CartLike)
                {
                    continue;
                }

                ValuableObject valuable = item.GetComponentInParent<ValuableObject>();
                if (valuable != null)
                {
                    removedHaul += (int)GameAccess.DollarValueCurrent(valuable);
                }

                items.RemoveAt(i);
                removed++;
            }

            if (removed == 0)
            {
                return;
            }

            GameAccess.SetItemsInCartCount(cart, Math.Max(0, GameAccess.ItemsInCartCount(cart) - removed));
            int haul = Math.Max(0, GameAccess.HaulCurrent(cart) - removedHaul);
            GameAccess.SetHaulCurrent(cart, haul);
            if (cart.valueScreen != null)
            {
                cart.valueScreen.UpdateValue(haul);
            }

            ModLog.Debug("Removed " + removed + " cart-like object(s) from cart contents: " + cart.name);
        }
    }
}
