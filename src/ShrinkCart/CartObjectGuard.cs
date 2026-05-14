using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShrinkCart
{
    internal static class CartObjectGuard
    {
        private static readonly HashSet<int> CartLikeObjectIds = new HashSet<int>();

        private static readonly FieldInfo ItemAttributesItemTypeField =
            typeof(ItemAttributes).GetField("itemType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static bool IsCartLike(PhysGrabObject item)
        {
            if (item == null || item.gameObject == null)
            {
                return false;
            }

            int id = item.gameObject.GetInstanceID();
            if (CartLikeObjectIds.Contains(id))
            {
                return true;
            }

            if (HasCartLikeComponent(item) || HasCartLikeItemType(item))
            {
                CartLikeObjectIds.Add(id);
                return true;
            }

            return false;
        }

        internal static bool ShouldBlockCartInCart(PhysGrabInCart destination, PhysGrabObject item)
        {
            if (destination == null || destination.cart == null || item == null)
            {
                return false;
            }

            if (!IsCartLike(item))
            {
                return false;
            }

            return true;
        }

        private static bool HasCartLikeComponent(PhysGrabObject item)
        {
            return item.GetComponent<PhysGrabCart>() != null ||
                   item.GetComponentInParent<PhysGrabCart>() != null ||
                   item.GetComponentInChildren<PhysGrabCart>(true) != null ||
                   item.GetComponent<ItemVehicle>() != null ||
                   item.GetComponentInParent<ItemVehicle>() != null ||
                   item.GetComponentInChildren<ItemVehicle>(true) != null ||
                   item.GetComponent<ItemCartCannon>() != null ||
                   item.GetComponent<ItemCartCannonMain>() != null ||
                   item.GetComponent<ItemCartLaser>() != null ||
                   item.GetComponentInParent<ItemCartCannon>() != null ||
                   item.GetComponentInParent<ItemCartCannonMain>() != null ||
                   item.GetComponentInParent<ItemCartLaser>() != null ||
                   item.GetComponentInChildren<ItemCartCannon>(true) != null ||
                   item.GetComponentInChildren<ItemCartCannonMain>(true) != null ||
                   item.GetComponentInChildren<ItemCartLaser>(true) != null;
        }

        private static bool HasCartLikeItemType(PhysGrabObject item)
        {
            ItemAttributes attributes = item.GetComponent<ItemAttributes>();
            if (attributes == null || ItemAttributesItemTypeField == null)
            {
                return false;
            }

            object value = ItemAttributesItemTypeField.GetValue(attributes);
            if (!(value is SemiFunc.itemType))
            {
                return false;
            }

            SemiFunc.itemType itemType = (SemiFunc.itemType)value;
            return itemType == SemiFunc.itemType.cart ||
                   itemType == SemiFunc.itemType.vehicle ||
                   itemType == SemiFunc.itemType.pocket_cart;
        }

    }

    internal static class CartCollisionGuard
    {
        private const float IgnoreSeconds = 0.75f;

        private sealed class IgnoredPair
        {
            internal Collider A;
            internal Collider B;
            internal float RestoreTime;
        }

        private static readonly List<IgnoredPair> IgnoredPairs = new List<IgnoredPair>(32);
        private static readonly List<int> RemoveIndexes = new List<int>(16);

        internal static void HandleBlockedCartInCart(PhysGrabInCart destination, PhysGrabObject item)
        {
            if (destination == null || destination.cart == null || item == null)
            {
                return;
            }

            Collider[] destinationColliders = destination.cart.GetComponentsInChildren<Collider>(true);
            Collider[] itemColliders = item.GetComponentsInChildren<Collider>(true);
            if (destinationColliders == null || itemColliders == null)
            {
                return;
            }

            float restoreTime = Time.time + IgnoreSeconds;
            for (int i = 0; i < destinationColliders.Length; i++)
            {
                Collider a = destinationColliders[i];
                if (a == null || a.isTrigger)
                {
                    continue;
                }

                for (int j = 0; j < itemColliders.Length; j++)
                {
                    Collider b = itemColliders[j];
                    if (b == null || b.isTrigger || a == b)
                    {
                        continue;
                    }

                    Physics.IgnoreCollision(a, b, true);
                    IgnoredPairs.Add(new IgnoredPair
                    {
                        A = a,
                        B = b,
                        RestoreTime = restoreTime
                    });
                }
            }

            DebugLog("Temporarily ignored cart collision for blocked cart-in-cart object: " + item.name);
        }

        internal static void Tick()
        {
            if (IgnoredPairs.Count == 0)
            {
                return;
            }

            float now = Time.time;
            RemoveIndexes.Clear();
            for (int i = 0; i < IgnoredPairs.Count; i++)
            {
                IgnoredPair pair = IgnoredPairs[i];
                if (pair == null || now < pair.RestoreTime)
                {
                    continue;
                }

                if (pair.A != null && pair.B != null)
                {
                    Physics.IgnoreCollision(pair.A, pair.B, false);
                }

                RemoveIndexes.Add(i);
            }

            for (int i = RemoveIndexes.Count - 1; i >= 0; i--)
            {
                IgnoredPairs.RemoveAt(RemoveIndexes[i]);
            }

            RemoveIndexes.Clear();
        }

        internal static void Reset()
        {
            for (int i = 0; i < IgnoredPairs.Count; i++)
            {
                IgnoredPair pair = IgnoredPairs[i];
                if (pair != null && pair.A != null && pair.B != null)
                {
                    Physics.IgnoreCollision(pair.A, pair.B, false);
                }
            }

            IgnoredPairs.Clear();
            RemoveIndexes.Clear();
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
