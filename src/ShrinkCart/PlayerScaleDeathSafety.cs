using System;
using System.Collections.Generic;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class PlayerScaleDeathSafety
    {
        private static readonly HashSet<int> ActiveShrinkCartScaledPlayers = new HashSet<int>();
        private static readonly HashSet<int> EverShrinkCartScaledPlayers = new HashSet<int>();

        internal static void Mark(PlayerAvatar player)
        {
            if (player != null)
            {
                int id = player.GetInstanceID();
                ActiveShrinkCartScaledPlayers.Add(id);
                EverShrinkCartScaledPlayers.Add(id);
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
            try
            {
                ScaleManager.CleanupAll();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed global ScalerCore cleanup during ShrinkCart reset: " + ex.Message);
            }

            ActiveShrinkCartScaledPlayers.Clear();
            EverShrinkCartScaledPlayers.Clear();
        }

        internal static void RestoreBeforeDeath(PlayerAvatar player, string reason)
        {
            if (player == null || !EverShrinkCartScaledPlayers.Contains(player.GetInstanceID()))
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

            if (EverShrinkCartScaledPlayers.Contains(player.GetInstanceID()))
            {
                PlayerCartScaleController.RestoreIfShrinkCartScaled(player, reason);
                ForceRestoreMarkedPlayer(player, reason);
            }

            ActiveShrinkCartScaledPlayers.Remove(player.GetInstanceID());
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

                    DebugLog("Cleaned ShrinkCart player scale state before death/revive: " + player.name + " reason=" + reason);
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
