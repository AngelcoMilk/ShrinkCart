using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShrinkCart
{
    internal static class CartRegistry
    {
        private const float PenetrationPadding = 0.03f;
        private const float MaximumCartVelocity = 4.0f;

        private sealed class CartState
        {
            internal PhysGrabCart Cart;
            internal PhysGrabObject CartObject;
            internal Rigidbody Body;
            internal readonly List<Collider> SolidColliders = new List<Collider>(16);
        }

        private struct PenetrationResult
        {
            internal Vector3 Direction;
            internal float Distance;
            internal bool HasHit;
        }

        private static readonly Dictionary<int, CartState> Carts =
            new Dictionary<int, CartState>();

        private static readonly List<int> RemoveCartIds = new List<int>(8);
        private static readonly HashSet<int> FixedUpdateHandledCartIds = new HashSet<int>();
        private static readonly HashSet<long> FixedStepResolvedPairs = new HashSet<long>();
        private static int _lastFixedStepKey = -1;

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
            if (cart == null)
            {
                return;
            }

            CartState state = new CartState
            {
                Cart = cart,
                CartObject = cart.GetComponent<PhysGrabObject>(),
                Body = cart.GetComponent<Rigidbody>()
            };
            RefreshColliders(state);
            Carts[cart.GetInstanceID()] = state;

            DebugLog("Registered cart guard target: " + cart.name);
        }

        internal static void Tick()
        {
            if (!IsHostOrSingleplayer())
            {
                return;
            }

            PruneInvalidCarts();
            if (Carts.Count == 0)
            {
                return;
            }

            foreach (CartState state in Carts.Values)
            {
                RemoveCartLikeItems(state);
            }

            if (!ModConfig.PreventCartOverlap.Value)
            {
                return;
            }

            ResolveAllOverlaps();
        }

        internal static void FixedTick(PhysGrabCart cart)
        {
            if (cart == null || !ModConfig.PreventCartOverlap.Value || !IsHostOrSingleplayer())
            {
                return;
            }

            PruneInvalidCarts();
            CartState state = GetOrCreateState(cart);
            if (state == null)
            {
                return;
            }

            RemoveCartLikeItems(state);

            PrepareFixedStepPairCache();
            int id = cart.GetInstanceID();
            if (FixedUpdateHandledCartIds.Contains(id))
            {
                return;
            }

            FixedUpdateHandledCartIds.Add(id);
            ResolveOverlapsFor(state);
        }

        internal static void CleanCartContents(PhysGrabCart cart)
        {
            CartState state = GetOrCreateState(cart);
            RemoveCartLikeItems(state);
        }

        internal static void Reset()
        {
            Carts.Clear();
            RemoveCartIds.Clear();
            FixedUpdateHandledCartIds.Clear();
            FixedStepResolvedPairs.Clear();
            _lastFixedStepKey = -1;
        }

        internal static void HandleBlockedCartInCart(PhysGrabInCart destination, PhysGrabObject item)
        {
            if (destination == null || destination.cart == null || item == null)
            {
                return;
            }

            if (!ModConfig.PreventCartOverlap.Value)
            {
                return;
            }

            if (!IsHostOrSingleplayer())
            {
                return;
            }

            CartState destinationState = GetOrCreateState(destination.cart);
            CartState itemState = FindCartState(item);
            if (destinationState == null || itemState == null || destinationState == itemState)
            {
                return;
            }

            ResolveOverlap(destinationState, itemState);
        }

        private static readonly List<CartState> CartStatesScratch = new List<CartState>(16);

        private static List<CartState> GetCartStatesScratch()
        {
            CartStatesScratch.Clear();
            foreach (CartState state in Carts.Values)
            {
                if (state != null && state.Cart != null)
                {
                    CartStatesScratch.Add(state);
                }
            }

            return CartStatesScratch;
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

        private static CartState FindCartState(PhysGrabObject item)
        {
            if (item == null)
            {
                return null;
            }

            PhysGrabCart cart = item.GetComponent<PhysGrabCart>();
            if (cart == null)
            {
                cart = item.GetComponentInParent<PhysGrabCart>();
            }

            if (cart == null)
            {
                cart = item.GetComponentInChildren<PhysGrabCart>(true);
            }

            return GetOrCreateState(cart);
        }

        private static void PruneInvalidCarts()
        {
            RemoveCartIds.Clear();
            foreach (KeyValuePair<int, CartState> pair in Carts)
            {
                CartState state = pair.Value;
                if (state == null || state.Cart == null)
                {
                    RemoveCartIds.Add(pair.Key);
                    continue;
                }

                if (state.CartObject == null)
                {
                    state.CartObject = state.Cart.GetComponent<PhysGrabObject>();
                }

                if (state.Body == null)
                {
                    state.Body = state.Cart.GetComponent<Rigidbody>();
                }

                if (state.SolidColliders.Count == 0)
                {
                    RefreshColliders(state);
                }
            }

            for (int i = 0; i < RemoveCartIds.Count; i++)
            {
                Carts.Remove(RemoveCartIds[i]);
            }

            RemoveCartIds.Clear();
        }

        private static void RemoveCartLikeItems(CartState state)
        {
            List<PhysGrabObject> items = GetItemsInCart(state == null ? null : state.Cart);
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
                    CartState other = FindCartState(item);
                    if (other != null && other != state && ModConfig.PreventCartOverlap.Value)
                    {
                        ResolveOverlap(state, other);
                    }
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

        private static void ResolveOverlap(CartState a, CartState b)
        {
            if (a == null || b == null || a.Cart == null || b.Cart == null || a.Cart == b.Cart)
            {
                return;
            }

            PenetrationResult penetration = ComputeCartPenetration(a, b);
            if (!penetration.HasHit)
            {
                return;
            }

            SeparateCarts(a, b, penetration.Direction, penetration.Distance);
        }

        private static void ResolveAllOverlaps()
        {
            List<CartState> states = GetCartStatesScratch();
            for (int i = 0; i < states.Count; i++)
            {
                for (int j = i + 1; j < states.Count; j++)
                {
                    ResolveOverlap(states[i], states[j]);
                }
            }

            states.Clear();
        }

        private static void ResolveOverlapsFor(CartState state)
        {
            List<CartState> states = GetCartStatesScratch();
            for (int i = 0; i < states.Count; i++)
            {
                CartState other = states[i];
                if (other != null && other != state)
                {
                    ResolveOverlapOncePerFixedStep(state, other);
                }
            }

            states.Clear();
        }

        private static void PrepareFixedStepPairCache()
        {
            int key = Mathf.RoundToInt(Time.fixedTime / Mathf.Max(Time.fixedDeltaTime, 0.0001f));
            if (key == _lastFixedStepKey)
            {
                return;
            }

            _lastFixedStepKey = key;
            FixedUpdateHandledCartIds.Clear();
            FixedStepResolvedPairs.Clear();
        }

        private static void ResolveOverlapOncePerFixedStep(CartState a, CartState b)
        {
            if (a == null || b == null || a.Cart == null || b.Cart == null)
            {
                return;
            }

            int idA = a.Cart.GetInstanceID();
            int idB = b.Cart.GetInstanceID();
            int first = Mathf.Min(idA, idB);
            int second = Mathf.Max(idA, idB);
            long key = ((long)first << 32) ^ (uint)second;
            if (FixedStepResolvedPairs.Contains(key))
            {
                return;
            }

            FixedStepResolvedPairs.Add(key);
            ResolveOverlap(a, b);
        }

        private static PenetrationResult ComputeCartPenetration(CartState a, CartState b)
        {
            PenetrationResult best = new PenetrationResult();
            EnsureColliders(a);
            EnsureColliders(b);

            for (int i = 0; i < a.SolidColliders.Count; i++)
            {
                Collider ca = a.SolidColliders[i];
                if (ca == null || !ca.enabled)
                {
                    continue;
                }

                Bounds boundsA = ca.bounds;
                for (int j = 0; j < b.SolidColliders.Count; j++)
                {
                    Collider cb = b.SolidColliders[j];
                    if (cb == null || !cb.enabled)
                    {
                        continue;
                    }

                    if (!boundsA.Intersects(cb.bounds))
                    {
                        continue;
                    }

                    Vector3 direction;
                    float distance;
                    if (!Physics.ComputePenetration(
                            ca,
                            ca.transform.position,
                            ca.transform.rotation,
                            cb,
                            cb.transform.position,
                            cb.transform.rotation,
                            out direction,
                            out distance))
                    {
                        continue;
                    }

                    direction.y = 0.0f;
                    if (direction.sqrMagnitude < 0.0001f)
                    {
                        direction = a.Cart.transform.position - b.Cart.transform.position;
                        direction.y = 0.0f;
                    }

                    if (direction.sqrMagnitude < 0.0001f)
                    {
                        direction = a.Cart.transform.right;
                        direction.y = 0.0f;
                    }

                    if (distance > best.Distance)
                    {
                        best.Direction = direction.normalized;
                        best.Distance = distance;
                        best.HasHit = true;
                    }
                }
            }

            return best;
        }

        private static void SeparateCarts(CartState a, CartState b, Vector3 direction, float distance)
        {
            Rigidbody rbA = a.Body;
            Rigidbody rbB = b.Body;
            if (rbA == null && rbB == null)
            {
                return;
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float strength = ModConfig.SafeCartSeparationStrength();
            float step = Mathf.Clamp((distance + PenetrationPadding) * strength, 0.0f, ModConfig.SafeCartMaximumCorrectionDistance());
            Vector3 move = direction.normalized * step;

            bool canMoveA = rbA != null && !rbA.isKinematic;
            bool canMoveB = rbB != null && !rbB.isKinematic;
            if (canMoveA && canMoveB)
            {
                MoveCart(rbA, move * 0.5f);
                MoveCart(rbB, -move * 0.5f);
            }
            else if (canMoveA)
            {
                MoveCart(rbA, move);
            }
            else if (canMoveB)
            {
                MoveCart(rbB, -move);
            }

            if (ModConfig.CartClearCrushVelocity.Value)
            {
                RemoveClosingVelocity(rbA, direction);
                RemoveClosingVelocity(rbB, -direction);
            }

            ClampVelocity(rbA);
            ClampVelocity(rbB);

            DebugLog(
                "Corrected cart penetration: " +
                a.Cart.name +
                " <-> " +
                b.Cart.name +
                " depth=" +
                distance.ToString("0.###"));
        }

        private static void MoveCart(Rigidbody body, Vector3 delta)
        {
            if (body == null || body.isKinematic)
            {
                return;
            }

            body.MovePosition(body.position + delta);
        }

        private static void RemoveClosingVelocity(Rigidbody body, Vector3 outwardDirection)
        {
            if (body == null || body.isKinematic || outwardDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 normal = outwardDirection.normalized;
            float inwardSpeed = Vector3.Dot(body.velocity, -normal);
            if (inwardSpeed > 0.0f)
            {
                body.velocity += normal * inwardSpeed;
            }
        }

        private static void ClampVelocity(Rigidbody body)
        {
            if (body == null)
            {
                return;
            }

            Vector3 velocity = body.velocity;
            Vector3 horizontal = new Vector3(velocity.x, 0.0f, velocity.z);
            if (horizontal.magnitude > MaximumCartVelocity)
            {
                horizontal = horizontal.normalized * MaximumCartVelocity;
                body.velocity = new Vector3(horizontal.x, Mathf.Min(velocity.y, MaximumCartVelocity), horizontal.z);
            }

            Vector3 angular = body.angularVelocity;
            if (angular.magnitude > MaximumCartVelocity)
            {
                body.angularVelocity = angular.normalized * MaximumCartVelocity;
            }
        }

        private static void EnsureColliders(CartState state)
        {
            if (state != null && state.SolidColliders.Count == 0)
            {
                RefreshColliders(state);
            }
        }

        private static void RefreshColliders(CartState state)
        {
            if (state == null || state.Cart == null)
            {
                return;
            }

            state.SolidColliders.Clear();
            Collider[] colliders = state.Cart.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && !collider.isTrigger)
                {
                    state.SolidColliders.Add(collider);
                }
            }
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
            try
            {
                return SemiFunc.IsMasterClientOrSingleplayer();
            }
            catch
            {
                return true;
            }
        }
    }
}
