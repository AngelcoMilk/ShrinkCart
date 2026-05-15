using System;
using System.Collections.Generic;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class PlayerScaleDeathSafety
    {
        private static readonly HashSet<int> ActiveShrinkCartScaledPlayers = new HashSet<int>();

        internal static void Mark(PlayerAvatar player)
        {
            if (player != null)
            {
                ActiveShrinkCartScaledPlayers.Add(player.GetInstanceID());
            }
        }

        internal static void UnmarkActive(PlayerAvatar player)
        {
            if (player != null)
            {
                ActiveShrinkCartScaledPlayers.Remove(player.GetInstanceID());
            }
        }

        internal static void ClearActive()
        {
            ActiveShrinkCartScaledPlayers.Clear();
        }

        internal static void ClearAll()
        {
            ActiveShrinkCartScaledPlayers.Clear();
        }

        internal static void RestoreBeforeDeath(PlayerAvatar player, string reason)
        {
            if (!ModConfig.PlayerScalingEnabled())
            {
                return;
            }

            if (player == null || !ActiveShrinkCartScaledPlayers.Contains(player.GetInstanceID()))
            {
                return;
            }

            if (PlayerCartScaleController.RestoreIfShrinkCartScaled(player, reason))
            {
                return;
            }

            ForceRestoreMarkedPlayer(player, reason);
        }

        private static void ForceRestoreMarkedPlayer(PlayerAvatar player, string reason)
        {
            GameObject target = player == null ? null : player.gameObject;
            if (target == null)
            {
                return;
            }

            try
            {
                ScaleController controller = ScaleManager.GetController(target);
                if (controller != null)
                {
                    if (controller.IsScaled)
                    {
                        ScaleOptions options = controller.CurrentOptions;
                        options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
                        options.SuppressImpactFlash = ModConfig.HideScaleFlash.Value;
                        options.SuppressCameraShake = ModConfig.HideScaleFlash.Value;
                        ScaleManager.ForceUpdateOptions(target, options);
                        ScaleManager.ForceRestore(target);
                    }

                    DebugLog("Cleaned ShrinkCart player scale state before death: " + player.name + " reason=" + reason);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed death-safety restore for " + player.name + ": " + ex.Message);
            }
            finally
            {
                ActiveShrinkCartScaledPlayers.Remove(player.GetInstanceID());
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
