using System;
using ScalerCore;
using ShrinkCart.Core;
using ShrinkCart.Domain;
using UnityEngine;

namespace ShrinkCart.Scaling
{
    internal enum RestoreResult
    {
        Restored,

        /// <summary>不是本 mod 施加的缩放，或已被外部恢复/接管：未做任何操作，所有权已释放。</summary>
        NotOwned,

        Failed
    }

    /// <summary>
    /// ShrinkCart 中唯一调用 ScalerCore.ScaleManager 的地方。统一 try/catch 与去重告警，并维护 <see cref="ScaleOwnership"/>。
    /// <para>ScalerCore 1.1.1 行为约定（IL 已核实）：ApplyIfNotScaled 在同一调用栈内调用 DispatchShrink；
    /// DispatchShrink 只在主机执行，且只在入口读取全局开关 AllowDeadHeads；恢复路径（DispatchExpand）不读取该开关；
    /// 全局开关 NativeSync 由 NativeRespawn.Decide 读取，派发时读到 false 就走 RPC 且清除延迟重生标记，之后 TickDeferredRespawn 不会再处理这个物体。</para>
    /// <para>恢复语义沿用 0.2.x 的实测选择：物品/代币箱用 UpdateOptions + Restore；玩家/死亡头颅用 ForceUpdateOptions + ForceRestore。</para>
    /// </summary>
    internal sealed class ScalerCoreGateway
    {
        private readonly ScaleOwnership _ownership;

        internal ScalerCoreGateway(ScaleOwnership ownership)
        {
            _ownership = ownership;
        }

        internal bool IsScaled(GameObject target)
        {
            try
            {
                return ScaleManager.IsScaled(target);
            }
            catch (Exception ex)
            {
                ModLog.WarnOnce("scalercore:isScaled", "ScalerCore IsScaled failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>目标当前的缩放是否仍是本 mod 施加、且未被外部恢复或替换。</summary>
        internal bool IsOurs(GameObject target)
        {
            if (target == null || !_ownership.TryGet(target.GetInstanceID(), out ScaleOwnership.OwnedScale owned))
            {
                return false;
            }

            return MatchesOwned(target, owned);
        }

        internal bool TryApply(GameObject target, ScaleOptions options, ScaleKind kind)
        {
            try
            {
                bool applied = kind == ScaleKind.DeadHead
                    ? ApplyWithDeadHeadsAllowed(target, options)
                    : ApplyOnRpcPath(target, options);
                if (applied)
                {
                    _ownership.Register(target, kind, options.Factor);
                }

                return applied;
            }
            catch (Exception ex)
            {
                ModLog.WarnOnce("scalercore:apply:" + kind, "Failed to apply " + kind + " scale to " + target.name + ": " + ex.Message);
                return false;
            }
        }

        internal RestoreResult Restore(GameObject target, float restoreSpeed)
        {
            if (target == null)
            {
                return RestoreResult.NotOwned;
            }

            int id = target.GetInstanceID();
            if (!_ownership.TryGet(id, out ScaleOwnership.OwnedScale owned))
            {
                return RestoreResult.NotOwned;
            }

            _ownership.Release(id);
            try
            {
                if (!MatchesOwned(target, owned))
                {
                    return RestoreResult.NotOwned;
                }

                ScaleOptions options = ScaleManager.GetController(target).CurrentOptions;
                options.RestoreSpeed = restoreSpeed;
                options.SuppressImpactFlash = true;
                options.SuppressCameraShake = true;

                RestoreOnRpcPath(target, owned.Kind, options);
                return RestoreResult.Restored;
            }
            catch (Exception ex)
            {
                ModLog.WarnOnce("scalercore:restore:" + owned.Kind, "Failed to restore " + owned.Kind + " " + target.name + ": " + ex.Message);
                return RestoreResult.Failed;
            }
        }

        /// <summary>放弃所有权但不恢复（目标已销毁、失去主机权限等）。</summary>
        internal void Forget(int id)
        {
            _ownership.Release(id);
        }

        private static bool MatchesOwned(GameObject target, ScaleOwnership.OwnedScale owned)
        {
            try
            {
                ScaleController controller = ScaleManager.GetController(target);
                return controller != null &&
                       controller.IsScaled &&
                       MathUtil.Approximately(controller.CurrentOptions.Factor, owned.Factor);
            }
            catch (Exception ex)
            {
                ModLog.WarnOnce("scalercore:controller", "ScalerCore controller query failed: " + ex.Message);
                return false;
            }
        }

        // 全局开关只在本次调用期间放行，调用后立即还原，不影响缩小枪等其他 ScalerCore 使用者。
        private static bool ApplyWithDeadHeadsAllowed(GameObject target, ScaleOptions options)
        {
            bool previous = ScaleManager.AllowDeadHeads;
            try
            {
                ScaleManager.AllowDeadHeads = true;
                return ApplyOnRpcPath(target, options);
            }
            finally
            {
                ScaleManager.AllowDeadHeads = previous;
                ModLog.Debug("ScalerCore AllowDeadHeads restored to " + ScaleManager.AllowDeadHeads + " after death head apply.");
            }
        }

        // 原生重生（NativeSync）会销毁目标并生成新物体，而 ShrinkCart 按实例 ID 跟踪，所以自己施加和恢复的缩放全程走 RPC。
        // 全局开关只在本次调用期间关闭，调用后立即还原，不影响玩家的实验开关和其他 ScalerCore 使用者。
        private static bool ApplyOnRpcPath(GameObject target, ScaleOptions options)
        {
            bool previous = ScaleManager.NativeSync;
            try
            {
                ScaleManager.NativeSync = false;
                return ScaleManager.ApplyIfNotScaled(target, options);
            }
            finally
            {
                ScaleManager.NativeSync = previous;
            }
        }

        private static void RestoreOnRpcPath(GameObject target, ScaleKind kind, ScaleOptions options)
        {
            bool previous = ScaleManager.NativeSync;
            try
            {
                ScaleManager.NativeSync = false;
                if (kind == ScaleKind.Item || kind == ScaleKind.ValuableBox)
                {
                    ScaleManager.UpdateOptions(target, options);
                    ScaleManager.Restore(target);
                }
                else
                {
                    ScaleManager.ForceUpdateOptions(target, options);
                    ScaleManager.ForceRestore(target);
                }
            }
            finally
            {
                ScaleManager.NativeSync = previous;
            }
        }
    }
}
