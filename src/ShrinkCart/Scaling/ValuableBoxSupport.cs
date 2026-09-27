using System;
using ScalerCore;
using ScalerCore.Handlers;
using ShrinkCart.Core;
using ShrinkCart.Game;
using UnityEngine;

namespace ShrinkCart.Scaling
{
    /// <summary>
    /// 让 ScalerCore 能缩放 v0.4.x 的代币箱（ItemValuableBox）。ScalerCore 不会为代币箱创建控制器，
    /// 因此这里在物体生成时为其挂上 ScaleController，并注册一个空的 IScaleHandler：它只负责让 ScalerCore 认领该目标
    /// （优先级 -10，仅匹配代币箱本体），缩放行为本身仍由 ScalerCore 通用逻辑处理。
    /// 外观箱（CosmeticWorldObject）由 ScalerCore 自己的 CosmeticHandler 处理，这里只等待其控制器就绪。
    /// 所有端都运行：客户端也需要控制器才能接收 ScalerCore 的缩放同步（未安装 ShrinkCart 的客户端看不到代币箱缩放）。
    /// </summary>
    internal static class ValuableBoxSupport
    {
        private const int HandlerPriority = -10;

        private static bool _registered;

        internal static void RegisterHandler(FeatureAvailability availability)
        {
            if (_registered)
            {
                return;
            }

            try
            {
                ScaleHandlerRegistry.Register(new ValuableBoxHandler(), IsValuableBoxScaleTarget, HandlerPriority);
                _registered = true;
            }
            catch (Exception ex)
            {
                string reason = "ScalerCore handler registration failed: " + ex.Message;
                availability.MarkUnavailable(FeatureId.ValuableBoxSupport, reason);
                ModLog.Warn(reason);
            }
        }

        /// <summary>PhysGrabObject.Start 之后调用。非代币箱物体只做一次组件查找链就返回。</summary>
        internal static void OnPhysGrabObjectStarted(PhysGrabObject item)
        {
            if (item == null || SpecialBoxDetector.FindValuableBox(item) == null)
            {
                return;
            }

            EnsureController(item.gameObject);
        }

        /// <summary>ItemValuableBox.Start 之后调用（与上面互为兜底，取决于两个 Start 的先后顺序）。</summary>
        internal static void OnValuableBoxStarted(ItemValuableBox box)
        {
            PhysGrabObject owner = SpecialBoxDetector.FindOwner(box);
            if (owner != null)
            {
                EnsureController(owner.gameObject);
            }
        }

        /// <summary>
        /// 确保目标已有 ScaleController。返回 true 表示控制器已就绪可以缩放；
        /// 本次刚挂上控制器或外观箱控制器尚未由 ScalerCore 创建时返回 false，调用方下次观测再试。
        /// </summary>
        internal static bool EnsureController(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.GetComponent<ScaleController>() != null)
            {
                return true;
            }

            if (target.GetComponent<CosmeticWorldObject>() != null ||
                target.GetComponentInParent<CosmeticWorldObject>() != null ||
                target.GetComponentInChildren<CosmeticWorldObject>(true) != null)
            {
                ModLog.Debug("Cosmetic box is waiting for ScalerCore's cosmetic controller: " + target.name);
                return false;
            }

            if (!IsValuableBoxScaleTarget(target))
            {
                return false;
            }

            target.AddComponent<ScaleController>();
            ModLog.Debug("Attached ScalerCore controller to token box: " + target.name);
            return false;
        }

        private static bool IsValuableBoxScaleTarget(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            PhysGrabObject item = target.GetComponent<PhysGrabObject>();
            if (item == null)
            {
                return false;
            }

            ItemValuableBox box = SpecialBoxDetector.FindValuableBox(item);
            return box != null && SpecialBoxDetector.FindOwner(box) == item;
        }

        private sealed class ValuableBoxHandler : IScaleHandler
        {
            public void Setup(ScaleController ctrl)
            {
            }

            public void OnScale(ScaleController ctrl)
            {
            }

            public void OnRestore(ScaleController ctrl, bool isBonk)
            {
            }

            public void OnUpdate(ScaleController ctrl)
            {
            }

            public void OnLateUpdate(ScaleController ctrl)
            {
            }

            public void OnDestroy(ScaleController ctrl)
            {
            }
        }
    }
}
