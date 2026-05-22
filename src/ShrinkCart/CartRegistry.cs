using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShrinkCart
{
    internal static class CartRegistry
    {
        private sealed class CartState
        {
            internal PhysGrabCart Cart;
        }

        private static readonly Dictionary<int, CartState> Carts =
            new Dictionary<int, CartState>();

        private static readonly List<int> RemoveCartIds = new List<int>(8);

        private static readonly FieldInfo PhysGrabCartItemsInCartField =
            typeof(PhysGrabCart).GetField("itemsInCart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo PhysGrabCartItemsInCartCountField =
            typeof(PhysGrabCart).GetField("itemsInCartCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo PhysGrabCartHaulCurrentField =
            typeof(PhysGrabCart).GetField("haulCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly FieldInfo ValuableObjectDollarValueCurrentField =
            typeof(ValuableObject).GetField("dollarValueCurrent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

            Carts[cart.GetInstanceID()] = new CartState
            {
                Cart = cart
            };

            DebugLog("Registered cart content guard target: " + cart.name);
        }

        internal static void RegisterExistingCarts()
        {
            if (!IsHostOrSingleplayer())
            {
                return;
            }

            PhysGrabCart[] carts = Object.FindObjectsOfType<PhysGrabCart>();
            for (int i = 0; i < carts.Length; i++)
            {
                RegisterCart(carts[i]);
            }
        }

        internal static void CleanCartContents(PhysGrabCart cart)
        {
            if (!IsHostOrSingleplayer())
            {
                return;
            }

            CartState state = GetOrCreateState(cart);
            List<PhysGrabObject> items = GetItemsInCart(state == null ? null : state.Cart);
            if (state == null || state.Cart == null || items == null)
            {
                return;
            }

            RemoveCartLikeItems(state, items);
            ShrinkerCartController.MarkObjectsSeenInCart(state.Cart, items);
        }

        internal static void Reset()
        {
            Carts.Clear();
            RemoveCartIds.Clear();
            CartObjectGuard.Reset();
        }

        internal static void HandleBlockedCartInCart(PhysGrabInCart destination, PhysGrabObject item)
        {
            if (destination == null || destination.cart == null || item == null)
            {
                return;
            }

            CartState destinationState = GetOrCreateState(destination.cart);
            if (destinationState == null)
            {
                return;
            }

            List<PhysGrabObject> items = GetItemsInCart(destinationState.Cart);
            if (items != null)
            {
                RemoveCartLikeItems(destinationState, items);
            }

            DebugLog("Blocked cart-like object from cart Add: " + item.name);
        }

        private static CartState GetOrCreateState(PhysGrabCart cart)
        {
            if (cart == null)
            {
                return null;
            }

            CartState state;
            if (!Carts.TryGetValue(cart.GetInstanceID(), out state) || state == null)
            {
                RegisterCart(cart);
                Carts.TryGetValue(cart.GetInstanceID(), out state);
            }

            return state;
        }

        private static void PruneInvalidCarts()
        {
            RemoveCartIds.Clear();
            foreach (KeyValuePair<int, CartState> pair in Carts)
            {
                if (pair.Value == null || pair.Value.Cart == null)
                {
                    RemoveCartIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < RemoveCartIds.Count; i++)
            {
                Carts.Remove(RemoveCartIds[i]);
            }

            RemoveCartIds.Clear();
        }

        private static void RemoveCartLikeItems(CartState state, List<PhysGrabObject> items)
        {
            if (state == null || state.Cart == null || items == null)
            {
                return;
            }

            bool changed = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                PhysGrabObject item = items[i];
                if (item == null)
                {
                    items.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (CartObjectGuard.IsCartLike(item))
                {
                    items.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
            {
                RecalculateCartCounts(state.Cart);
                DebugLog("Removed cart-like object from cart contents: " + state.Cart.name);
            }
        }

        private static void RecalculateCartCounts(PhysGrabCart cart)
        {
            List<PhysGrabObject> items = GetItemsInCart(cart);
            if (cart == null || items == null)
            {
                return;
            }

            int count = 0;
            int haul = 0;
            for (int i = 0; i < items.Count; i++)
            {
                PhysGrabObject item = items[i];
                if (item == null)
                {
                    continue;
                }

                count++;
                ValuableObject valuable = item.GetComponent<ValuableObject>();
                if (valuable == null)
                {
                    valuable = item.GetComponentInParent<ValuableObject>();
                }

                if (valuable != null)
                {
                    haul += GetValuableDollarValueCurrent(valuable);
                }
            }

            if (PhysGrabCartItemsInCartCountField != null)
            {
                PhysGrabCartItemsInCartCountField.SetValue(cart, count);
            }

            if (PhysGrabCartHaulCurrentField != null)
            {
                PhysGrabCartHaulCurrentField.SetValue(cart, haul);
            }

            if (cart.valueScreen != null)
            {
                cart.valueScreen.UpdateValue(haul);
            }
        }

        private static List<PhysGrabObject> GetItemsInCart(PhysGrabCart cart)
        {
            if (cart == null || PhysGrabCartItemsInCartField == null)
            {
                return null;
            }

            return PhysGrabCartItemsInCartField.GetValue(cart) as List<PhysGrabObject>;
        }

        private static int GetValuableDollarValueCurrent(ValuableObject valuable)
        {
            if (valuable == null || ValuableObjectDollarValueCurrentField == null)
            {
                return 0;
            }

            object value = ValuableObjectDollarValueCurrentField.GetValue(valuable);
            if (value is float)
            {
                return (int)(float)value;
            }

            if (value is int)
            {
                return (int)value;
            }

            return 0;
        }

        private static void DebugLog(string message)
        {
            if (ModConfig.DebugLogging != null && ModConfig.DebugLogging.Value)
            {
                Plugin.Log.LogInfo(message);
            }
        }

        private static bool IsHostOrSingleplayer()
        {
            return Authority.IsHostOrSingleplayer();
        }
    }
}
