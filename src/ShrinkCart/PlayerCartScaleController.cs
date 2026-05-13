using System;
using System.Collections.Generic;
using System.Reflection;
using ScalerCore;
using UnityEngine;

namespace ShrinkCart
{
    internal static class PlayerCartScaleController
    {
        private const float TickIntervalSeconds = 0.1f;
        private const float CenterZoneHorizontalScale = 0.45f;
        private const float CenterZoneVerticalPaddingBelow = 0.6f;
        private const float CenterZoneVerticalPaddingAbove = 0.8f;
        private const float MinimumCenterHalfExtent = 0.15f;
        private const float StandPointYOffset = 0.05f;

        private sealed class PlayerState
        {
            internal PlayerAvatar Player;
            internal bool ShrinkCartScaled;
            internal bool WasInTriggerZone;
            internal bool TriggeredThisStay;
            internal float TriggerZoneEnteredTime;
        }

        private sealed class CartState
        {
            internal PhysGrabCart Cart;
            internal Transform InCart;
            internal PhysGrabObject CartObject;
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

        private static readonly FieldInfo PhysGrabCartPhysGrabObjectField =
            typeof(PhysGrabCart).GetField("physGrabObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo PlayerPhysObjectStanderObjectsField =
            typeof(PlayerPhysObjectStander).GetField("physGrabObjects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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
                InCart = GetInCartTransform(cart),
                CartObject = GetCartPhysGrabObject(cart)
            };

            DebugLog("Registered regular cart for player stand-toggle: " + cart.name);
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

            _nextTickTime = now + TickIntervalSeconds;

            if (!ModConfig.CartShrinkingEnabled.Value || !ModConfig.ShrinkShopPlayerItems.Value)
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
                if (state != null && state.ShrinkCartScaled && state.Player != null)
                {
                    RestorePlayer(state.Player.gameObject);
                }
            }

            PlayerStates.Clear();
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

            if (state.ShrinkCartScaled && !ScaleManager.IsScaled(player.gameObject))
            {
                state.ShrinkCartScaled = false;
            }

            bool inTriggerZone = IsPlayerStandingInAnyCart(player);
            if (!inTriggerZone)
            {
                if (state.WasInTriggerZone)
                {
                    DebugLog("Player left cart center stand zone: " + player.name);
                }

                state.WasInTriggerZone = false;
                state.TriggeredThisStay = false;
                state.TriggerZoneEnteredTime = 0.0f;
                return;
            }

            if (!state.WasInTriggerZone)
            {
                state.WasInTriggerZone = true;
                state.TriggeredThisStay = false;
                state.TriggerZoneEnteredTime = now;
                DebugLog("Player entered cart center stand zone: " + player.name);
                return;
            }

            if (state.TriggeredThisStay ||
                now - state.TriggerZoneEnteredTime < ModConfig.SafePlayerCartStandTriggerSeconds())
            {
                return;
            }

            state.TriggeredThisStay = true;
            DebugLog("Player cart center stand timer completed: " + player.name);

            if (state.ShrinkCartScaled)
            {
                if (RestorePlayer(player.gameObject))
                {
                    state.ShrinkCartScaled = false;
                    DebugLog("Restored player after standing in cart center: " + player.name);
                }

                return;
            }

            if (ScaleManager.IsScaled(player.gameObject))
            {
                DebugLog("Skipped player cart shrink because another scale session is active: " + player.name);
                return;
            }

            if (ShrinkPlayer(player.gameObject))
            {
                state.ShrinkCartScaled = true;
                DebugLog("Shrunk player after standing in cart center: " + player.name);
            }
        }

        private static bool ShrinkPlayer(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            ScaleOptions options = ScaleOptions.Default;
            options.Factor = ModConfig.SafePlayerCartScaleFactor();
            options.Speed = ModConfig.SafeScaleSpeed();
            options.RestoreSpeed = ModConfig.SafeRestoreScaleSpeed();
            options.Duration = 0.0f;
            options.AllowedTargets = ScaleTargets.Players;
            options.SuppressImpactFlash = ModConfig.HideScaleFlash.Value;
            options.SuppressCameraShake = ModConfig.HideScaleFlash.Value;
            options.IgnoreBonkExpand = true;
            options.RejectExternalApply = true;

            try
            {
                bool applied = ScaleManager.ApplyIfNotScaled(target, options);
                if (!applied)
                {
                    DebugLog("ScalerCore rejected player cart shrink: " + target.name);
                }

                return applied;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Failed to shrink player standing in cart: " + ex.Message);
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
                    options.SuppressImpactFlash = ModConfig.HideScaleFlash.Value;
                    options.SuppressCameraShake = ModConfig.HideScaleFlash.Value;
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

        private static bool IsPlayerStandingInAnyCart(PlayerAvatar player)
        {
            foreach (CartState cartState in RegisteredCarts.Values)
            {
                if (cartState != null && IsPlayerStandingInCartCenter(player, cartState))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPlayerStandingInCartCenter(PlayerAvatar player, CartState cartState)
        {
            Transform inCart = cartState.InCart;
            if (inCart == null && cartState.Cart != null)
            {
                inCart = GetInCartTransform(cartState.Cart);
                cartState.InCart = inCart;
            }

            PhysGrabObject cartObject = cartState.CartObject;
            if (cartObject == null && cartState.Cart != null)
            {
                cartObject = GetCartPhysGrabObject(cartState.Cart);
                cartState.CartObject = cartObject;
            }

            if (player == null || inCart == null || cartObject == null)
            {
                return false;
            }

            if (!IsPlayerStandingOnCartObject(player, cartObject))
            {
                return false;
            }

            return IsPointInsideCartCenterZone(GetPlayerStandPoint(player), inCart);
        }

        private static bool IsPointInsideCartCenterZone(Vector3 point, Transform inCart)
        {
            Vector3 local = Quaternion.Inverse(inCart.rotation) * (point - inCart.position);
            Vector3 half = inCart.lossyScale;
            float centerHalfX = Mathf.Max(Mathf.Abs(half.x) * CenterZoneHorizontalScale, MinimumCenterHalfExtent);
            float centerHalfZ = Mathf.Max(Mathf.Abs(half.z) * CenterZoneHorizontalScale, MinimumCenterHalfExtent);
            float halfY = Mathf.Abs(half.y);

            return Mathf.Abs(local.x) <= centerHalfX &&
                   Mathf.Abs(local.z) <= centerHalfZ &&
                   local.y >= -halfY - CenterZoneVerticalPaddingBelow &&
                   local.y <= halfY + CenterZoneVerticalPaddingAbove;
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

        private static PhysGrabObject GetCartPhysGrabObject(PhysGrabCart cart)
        {
            if (cart == null || PhysGrabCartPhysGrabObjectField == null)
            {
                return null;
            }

            return PhysGrabCartPhysGrabObjectField.GetValue(cart) as PhysGrabObject;
        }

        private static bool IsPlayerStandingOnCartObject(PlayerAvatar player, PhysGrabObject cartObject)
        {
            if (player == null ||
                player.physObjectStander == null ||
                cartObject == null ||
                PlayerPhysObjectStanderObjectsField == null)
            {
                return false;
            }

            List<PhysGrabObject> standingObjects =
                PlayerPhysObjectStanderObjectsField.GetValue(player.physObjectStander) as List<PhysGrabObject>;
            if (standingObjects == null || standingObjects.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < standingObjects.Count; i++)
            {
                if (standingObjects[i] == cartObject)
                {
                    return true;
                }
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
            try
            {
                return SemiFunc.IsMasterClientOrSingleplayer();
            }
            catch
            {
                return true;
            }
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
