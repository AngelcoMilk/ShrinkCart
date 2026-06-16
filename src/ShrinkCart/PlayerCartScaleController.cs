using System;
using System.Collections.Generic;
using System.Reflection;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class PlayerCartScaleController
    {
        private const float CenterZoneHorizontalScale = 0.45f;
        private const float FloorProjectionPaddingBelow = 0.25f;
        private const float FloorProjectionStandingHeightAbove = 1.4f;
        private const float MinimumCenterHalfExtent = 0.15f;
        private const float StandPointYOffset = 0.05f;

        private enum PlayerCartScaleState
        {
            Normal,
            Shrunk,
            Grown
        }

        private enum PlayerCartNextAction
        {
            Shrink,
            Grow
        }

        private sealed class PlayerState
        {
            internal PlayerAvatar Player;
            internal PlayerCartScaleState ScaleState;
            internal PlayerCartNextAction NextAction;
            internal bool WasInCartRange;
            internal bool WasInTriggerZone;
            internal bool TriggeredThisStay;
            internal float TriggerZoneEnteredTime;
            internal float LastInsideCenterTime;
        }

        private sealed class CartState
        {
            internal PhysGrabCart Cart;
            internal Transform InCart;
        }

        private struct CartZoneResult
        {
            internal bool InCartRange;
            internal bool InTriggerZone;
        }

        private static readonly Dictionary<int, CartState> RegisteredCarts =
            new Dictionary<int, CartState>();

        private static readonly Dictionary<int, PlayerState> PlayerStates =
            new Dictionary<int, PlayerState>();

        private static readonly HashSet<int> ExcludedCartIds = new HashSet<int>();

        private static readonly List<int> RemoveCartIds = new List<int>(8);
        private static readonly List<int> RemovePlayerIds = new List<int>(8);

        private static readonly FieldInfo PhysGrabCartInCartField =
            typeof(PhysGrabCart).GetField("inCart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo PlayerAvatarColliderField =
            typeof(PlayerAvatar).GetField("collider", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static float _nextTickTime;

        internal static void Reset()
        {
            RegisteredCarts.Clear();
            PlayerStates.Clear();
            ExcludedCartIds.Clear();
            RemoveCartIds.Clear();
            RemovePlayerIds.Clear();
            _nextTickTime = 0.0f;
        }

        internal static void RegisterCart(PhysGrabCart cart)
        {
            if (!IsHostOrSingleplayer())
            {
                return;
            }

            if (cart == null)
            {
                return;
            }

            if (IsExcludedPlayerScaleCart(cart))
            {
                return;
            }

            RegisteredCarts[cart.GetInstanceID()] = new CartState
            {
                Cart = cart,
                InCart = GetInCartTransform(cart)
            };

            DebugLog("Registered regular cart for player stand-toggle: " + cart.name);
        }

        internal static void RegisterExistingCarts()
        {
            PhysGrabCart[] carts = UnityEngine.Object.FindObjectsOfType<PhysGrabCart>();
            for (int i = 0; i < carts.Length; i++)
            {
                RegisterCart(carts[i]);
            }
        }

        internal static void Tick()
        {
            if (!IsHostOrSingleplayer())
            {
                return;
            }

            float now = Time.time;
            if (now < _nextTickTime)
            {
                return;
            }

            _nextTickTime = now + ModConfig.SafePlayerCartDetectionIntervalSeconds();

            if (!ModConfig.PlayerScalingEnabled())
            {
                RestoreAll();
                return;
            }

            if (RegisteredCarts.Count == 0)
            {
                return;
            }

            PruneInvalidCarts();
            List<PlayerAvatar> players = GetPlayers();
            if (players == null || players.Count == 0)
            {
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                ProcessPlayer(players[i], now);
            }

            PruneMissingPlayers();
        }

        internal static void RestoreAll()
        {
            if (PlayerStates.Count == 0)
            {
                return;
            }

            foreach (PlayerState state in PlayerStates.Values)
            {
                if (state != null && state.ScaleState != PlayerCartScaleState.Normal && state.Player != null)
                {
                    RestorePlayer(state.Player.gameObject);
                }
            }

            PlayerStates.Clear();
        }

        internal static void Disable()
        {
            RestoreAll();
            Reset();
        }

        private static void ProcessPlayer(PlayerAvatar player, float now)
        {
            if (player == null || player.gameObject == null || !player.gameObject.activeInHierarchy)
            {
                return;
            }

            int id = player.GetInstanceID();
            PlayerState state;
            if (!PlayerStates.TryGetValue(id, out state))
            {
                state = new PlayerState
                {
                    Player = player
                };
                PlayerStates[id] = state;
            }
            else
            {
                state.Player = player;
            }

            if (state.ScaleState != PlayerCartScaleState.Normal && !ScaleManager.IsScaled(player.gameObject))
            {
                AdvanceCycleAfterRestore(state);
                state.ScaleState = PlayerCartScaleState.Normal;
            }
            else if (state.ScaleState == PlayerCartScaleState.Normal)
            {
                PlayerCartScaleState adoptedState;
                if (TryGetShrinkCartPlayerScaleState(player.gameObject, out adoptedState))
                {
                    state.ScaleState = adoptedState;
                    DebugLog("Adopted existing ShrinkCart-like player scale state: " + player.name + " state=" + adoptedState);
                }
            }

            CartZoneResult zone = GetPlayerCartZone(player);
            bool inCartRange = zone.InCartRange;
            bool inTriggerZone = zone.InTriggerZone;
            if (inCartRange)
            {
                state.LastInsideCenterTime = now;
            }
            else if (state.WasInCartRange &&
                     now - state.LastInsideCenterTime <= ModConfig.SafePlayerCartExitGraceSeconds())
            {
                inCartRange = true;
            }

            if (!inCartRange)
            {
                if (state.WasInCartRange)
                {
                    DebugLog("Player left cart floor range: " + player.name);
                }

                state.WasInCartRange = false;
                state.WasInTriggerZone = false;
                state.TriggeredThisStay = false;
                state.TriggerZoneEnteredTime = 0.0f;
                state.LastInsideCenterTime = 0.0f;
                return;
            }

            state.WasInCartRange = true;
            if (!inTriggerZone)
            {
                state.WasInTriggerZone = false;
                state.TriggerZoneEnteredTime = 0.0f;
                return;
            }

            if (!state.WasInTriggerZone)
            {
                state.WasInTriggerZone = true;
                state.TriggerZoneEnteredTime = now;
                state.LastInsideCenterTime = now;
                DebugLog("Player entered cart floor trigger zone: " + player.name);
                return;
            }

            if (state.TriggeredThisStay ||
                now - state.TriggerZoneEnteredTime < ModConfig.SafePlayerCartStandTriggerSeconds())
            {
                return;
            }

            state.TriggeredThisStay = true;
            DebugLog("Player cart floor trigger timer completed: " + player.name);

            if (state.ScaleState == PlayerCartScaleState.Shrunk ||
                state.ScaleState == PlayerCartScaleState.Grown)
            {
                PlayerCartScaleState previousState = state.ScaleState;
                if (RestorePlayer(player.gameObject))
                {
                    state.ScaleState = PlayerCartScaleState.Normal;
                    state.NextAction = previousState == PlayerCartScaleState.Shrunk
                        ? PlayerCartNextAction.Grow
                        : PlayerCartNextAction.Shrink;
                    DebugLog("Restored player after standing in cart center: " + player.name + " next=" + state.NextAction);
                }

                return;
            }

            if (ScaleManager.IsScaled(player.gameObject))
            {
                DebugLog("Skipped player cart shrink because another scale session is active: " + player.name);
                return;
            }

            PlayerCartScaleState targetState = state.NextAction == PlayerCartNextAction.Grow
                ? PlayerCartScaleState.Grown
                : PlayerCartScaleState.Shrunk;
            float factor = targetState == PlayerCartScaleState.Grown
                ? ModConfig.SafePlayerCartGrowFactor()
                : ModConfig.SafePlayerCartScaleFactor();

            if (ApplyPlayerScale(player.gameObject, factor, targetState))
            {
                state.ScaleState = targetState;
                DebugLog("Applied player cart scale after standing in cart center: " + player.name + " state=" + targetState + " factor=" + factor.ToString("0.###"));
            }
        }

        private static void AdvanceCycleAfterRestore(PlayerState state)
        {
            if (state == null)
            {
                return;
            }

            if (state.ScaleState == PlayerCartScaleState.Shrunk)
            {
                state.NextAction = PlayerCartNextAction.Grow;
            }
            else if (state.ScaleState == PlayerCartScaleState.Grown)
            {
                state.NextAction = PlayerCartNextAction.Shrink;
            }
        }

        private static bool ApplyPlayerScale(GameObject target, float factor, PlayerCartScaleState targetState)
        {
            if (target == null)
            {
                return false;
            }

            ScaleOptions options = ScaleOptions.Default;
            options.Factor = factor;
            options.Speed = ModConfig.SafeScaleSpeed();
            options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
            options.Duration = 0.0f;
            options.AllowedTargets = ScaleTargets.Players;
            options.SuppressImpactFlash = true;
            options.SuppressCameraShake = true;
            options.IgnoreBonkExpand = ModConfig.RestorePlayerOnDamage != null && !ModConfig.RestorePlayerOnDamage.Value;
            options.RejectExternalApply = false;

            try
            {
                bool applied = ScaleManager.ApplyIfNotScaled(target, options);
                if (!applied)
                {
                    DebugLog("ScalerCore rejected player cart scale: " + target.name + " state=" + targetState);
                }

                return applied;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to scale player standing in cart: " + ex.Message);
                return false;
            }
        }

        private static bool RestorePlayer(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            try
            {
                if (!ScaleManager.IsScaled(target))
                {
                    return true;
                }

                ScaleController controller = ScaleManager.GetController(target);
                if (controller != null && controller.IsScaled)
                {
                    ScaleOptions options = controller.CurrentOptions;
                    options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
                    options.SuppressImpactFlash = true;
                    options.SuppressCameraShake = true;
                    ScaleManager.ForceUpdateOptions(target, options);
                }

                ScaleManager.ForceRestore(target);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to restore player from cart toggle: " + ex.Message);
                return false;
            }
        }

        private static bool TryGetShrinkCartPlayerScaleState(GameObject target, out PlayerCartScaleState scaleState)
        {
            scaleState = PlayerCartScaleState.Normal;
            if (target == null || !ScaleManager.IsScaled(target))
            {
                return false;
            }

            ScaleController controller = ScaleManager.GetController(target);
            if (controller == null || !controller.IsScaled)
            {
                return false;
            }

            ScaleOptions options = controller.CurrentOptions;
            if (options.AllowedTargets != ScaleTargets.Players ||
                !options.SuppressImpactFlash ||
                !options.SuppressCameraShake ||
                options.RejectExternalApply)
            {
                return false;
            }

            if (Mathf.Approximately(options.Factor, ModConfig.SafePlayerCartScaleFactor()))
            {
                scaleState = PlayerCartScaleState.Shrunk;
                return true;
            }

            if (Mathf.Approximately(options.Factor, ModConfig.SafePlayerCartGrowFactor()))
            {
                scaleState = PlayerCartScaleState.Grown;
                return true;
            }

            return false;
        }

        private static CartZoneResult GetPlayerCartZone(PlayerAvatar player)
        {
            CartZoneResult result = new CartZoneResult();
            Vector3 standPoint = GetPlayerStandPoint(player);
            foreach (CartState cartState in RegisteredCarts.Values)
            {
                if (cartState == null)
                {
                    continue;
                }

                CartZoneResult cartResult = GetPlayerCartZone(player, cartState, standPoint);
                if (cartResult.InCartRange)
                {
                    result.InCartRange = true;
                }

                if (cartResult.InTriggerZone)
                {
                    result.InTriggerZone = true;
                    return result;
                }
            }

            return result;
        }

        private static CartZoneResult GetPlayerCartZone(PlayerAvatar player, CartState cartState, Vector3 standPoint)
        {
            CartZoneResult result = new CartZoneResult();
            Transform inCart = cartState.InCart;
            if (inCart == null && cartState.Cart != null)
            {
                inCart = GetInCartTransform(cartState.Cart);
                cartState.InCart = inCart;
            }

            if (player == null || inCart == null)
            {
                return result;
            }

            result.InCartRange = IsPointInsideCartFloorProjection(standPoint, inCart, 1.0f);
            result.InTriggerZone = result.InCartRange &&
                                   IsPointInsideCartFloorProjection(standPoint, inCart, CenterZoneHorizontalScale);
            return result;
        }

        private static bool IsPointInsideCartFloorProjection(Vector3 point, Transform inCart, float horizontalScale)
        {
            Vector3 local = Quaternion.Inverse(inCart.rotation) * (point - inCart.position);
            Vector3 half = inCart.localScale * 0.5f;
            float centerHalfX = Mathf.Max(Mathf.Abs(half.x) * horizontalScale, MinimumCenterHalfExtent);
            float centerHalfZ = Mathf.Max(Mathf.Abs(half.z) * horizontalScale, MinimumCenterHalfExtent);
            float floorY = -Mathf.Abs(half.y);

            return Mathf.Abs(local.x) <= centerHalfX &&
                   Mathf.Abs(local.z) <= centerHalfZ &&
                   local.y >= floorY - FloorProjectionPaddingBelow &&
                   local.y <= floorY + FloorProjectionStandingHeightAbove;
        }

        private static Transform GetInCartTransform(PhysGrabCart cart)
        {
            if (cart == null || PhysGrabCartInCartField == null)
            {
                return null;
            }

            return PhysGrabCartInCartField.GetValue(cart) as Transform;
        }

        private static void PruneInvalidCarts()
        {
            RemoveCartIds.Clear();
            foreach (KeyValuePair<int, CartState> pair in RegisteredCarts)
            {
                if (pair.Value == null ||
                    pair.Value.Cart == null ||
                    pair.Value.InCart == null ||
                    IsExcludedPlayerScaleCart(pair.Value.Cart))
                {
                    RemoveCartIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < RemoveCartIds.Count; i++)
            {
                RegisteredCarts.Remove(RemoveCartIds[i]);
            }

            RemoveCartIds.Clear();
        }

        private static bool IsExcludedPlayerScaleCart(PhysGrabCart cart)
        {
            if (cart == null)
            {
                return true;
            }

            int id = cart.GetInstanceID();
            if (ExcludedCartIds.Contains(id))
            {
                return true;
            }

            if (cart.isSmallCart)
            {
                ExcludedCartIds.Add(id);
                DebugLog("Excluded small cart from player stand-toggle: " + cart.name);
                return true;
            }

            return false;
        }

        private static Vector3 GetPlayerStandPoint(PlayerAvatar player)
        {
            Collider collider = null;
            if (player != null && PlayerAvatarColliderField != null)
            {
                collider = PlayerAvatarColliderField.GetValue(player) as Collider;
            }

            if (collider != null)
            {
                Bounds bounds = collider.bounds;
                return new Vector3(bounds.center.x, bounds.min.y + StandPointYOffset, bounds.center.z);
            }

            Vector3 position = player.playerTransform != null ? player.playerTransform.position : player.transform.position;
            return position + Vector3.up * StandPointYOffset;
        }

        private static void PruneMissingPlayers()
        {
            if (PlayerStates.Count == 0)
            {
                return;
            }

            RemovePlayerIds.Clear();
            foreach (KeyValuePair<int, PlayerState> pair in PlayerStates)
            {
                if (pair.Value == null || pair.Value.Player == null)
                {
                    RemovePlayerIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < RemovePlayerIds.Count; i++)
            {
                PlayerStates.Remove(RemovePlayerIds[i]);
            }

            RemovePlayerIds.Clear();
        }

        private static List<PlayerAvatar> GetPlayers()
        {
            try
            {
                if (GameDirector.instance != null && GameDirector.instance.PlayerList != null)
                {
                    return GameDirector.instance.PlayerList;
                }
            }
            catch
            {
            }

            try
            {
                return SemiFunc.PlayerGetAll();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsHostOrSingleplayer()
        {
            return Authority.IsHostOrSingleplayer();
        }

        private static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging.Value)
            {
                Plugin.Log.LogInfo(message);
            }
        }
    }
}
