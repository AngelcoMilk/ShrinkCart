using ScalerCore;
using ShrinkCart.Config;

namespace ShrinkCart.Scaling
{
    /// <summary>四类缩放的 ScaleOptions 统一在这里构建（与 0.2.x 各控制器中的取值一致）。</summary>
    internal static class ScaleOptionsFactory
    {
        internal static ScaleOptions ForItem(float factor, bool valuableBox)
        {
            ScaleOptions options = ScaleOptions.Default;
            options.Factor = factor;
            options.Speed = ModConfig.SafeScaleSpeed();
            options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
            options.Duration = 0.0f;
            options.AllowedTargets = valuableBox
                ? ScaleTargets.All
                : ScaleTargets.Valuables | ScaleTargets.Items;
            options.SuppressValueDropExpand = ModConfig.SuppressValuableDamageRestore.Value;
            options.PreserveMass = ModConfig.ShouldPreserveMass();
            options.SuppressImpactFlash = true;
            options.SuppressCameraShake = true;
            return options;
        }

        internal static ScaleOptions ForDeadHead()
        {
            ScaleOptions options = ScaleOptions.Default;
            options.Factor = ModConfig.SafeDeadHeadScaleFactor();
            options.Speed = ModConfig.SafeScaleSpeed();
            options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
            options.Duration = 0.0f;
            options.AllowedTargets = ScaleTargets.All;
            options.SuppressValueDropExpand = true;
            options.PreserveMass = ModConfig.ShouldPreserveMass();
            options.SuppressImpactFlash = true;
            options.SuppressCameraShake = true;
            options.RejectExternalApply = false;
            return options;
        }

        internal static ScaleOptions ForPlayer(float factor)
        {
            ScaleOptions options = ScaleOptions.Default;
            options.Factor = factor;
            options.Speed = ModConfig.SafeScaleSpeed();
            options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
            options.Duration = 0.0f;
            options.AllowedTargets = ScaleTargets.Players;
            options.SuppressImpactFlash = true;
            options.SuppressCameraShake = true;
            options.IgnoreBonkExpand = !ModConfig.RestorePlayerOnDamage.Value;
            options.RejectExternalApply = false;
            return options;
        }
    }
}
