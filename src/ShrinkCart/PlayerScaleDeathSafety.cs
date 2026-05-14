using System;
using System.Collections.Generic;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class PlayerScaleDeathSafety
    {
        private static readonly HashSet<int> ShrinkCartScaledPlayers = new HashSet<int>();

        internal static void Mark(PlayerAvatar player)
        {
            if (player != null)
            {
                ShrinkCartScaledPlayers.Add(player.GetInstanceID());
            }
        }

        internal static void Unmark(PlayerAvatar player)
        {
            if (player != null)
            {
                ShrinkCartScaledPlayers.Remove(player.GetInstanceID());
            }
        }

        internal static void ClearAll()
        {
            ShrinkCartScaledPlayers.Clear();
        }

        internal static void RestoreBeforeDeath(PlayerAvatar player, string reason)
        {
            if (player == null || !ShrinkCartScaledPlayers.Contains(player.GetInstanceID()))
            {
                return;
            }

            if (PlayerCartScaleController.RestoreIfShrinkCartScaled(player, reason))
            {
                return;
            }

            ForceRestoreMarkedPlayer(player, reason);
        }

        internal static void ClearBeforeRevive(PlayerAvatar player, string reason)
        {
            if (player == null)
            {
                return;
            }

            if (ShrinkCartScaledPlayers.Contains(player.GetInstanceID()))
            {
                PlayerCartScaleController.RestoreIfShrinkCartScaled(player, reason);
            }

            Unmark(player);
        }

        private static void ForceRestoreMarkedPlayer(PlayerAvatar player, string reason)
        {
            GameObject target = player == null ? null : player.gameObject;
            if (target == null)
            {
                return;
            }

            if (!ModConfig.PlayerAutoRestoreBeforeDeath.Value)
            {
                return;
            }

            try
            {
                ScaleController controller = ScaleManager.GetController(target);
                if (controller != null && controller.IsScaled)
                {
                    ScaleOptions options = controller.CurrentOptions;
                    options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
                    options.SuppressImpactFlash = ModConfig.HideScaleFlash.Value;
                    options.SuppressCameraShake = ModConfig.HideScaleFlash.Value;
                    ScaleManager.ForceUpdateOptions(target, options);
                    ScaleManager.ForceRestore(target);
                    DebugLog("Force restored marked player before death/revive: " + player.name + " reason=" + reason);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed death-safety restore for " + player.name + ": " + ex.Message);
            }
            finally
            {
                Unmark(player);
            }
        }

        private static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging != null && ModConfig.DebugLogging.Value)
            {
                Plugin.Log.LogInfo(message);
            }
        }
    }
}
