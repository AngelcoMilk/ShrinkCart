using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ShrinkCart
{
    internal static class CartObjectGuard
    {
        private static readonly HashSet<int> CartLikeObjectIds = new HashSet<int>();
        private static readonly HashSet<int> NonCartLikeObjectIds = new HashSet<int>();

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

            if (NonCartLikeObjectIds.Contains(id))
            {
                return false;
            }

            if (HasCartLikeComponent(item) || HasCartLikeItemType(item))
            {
                CartLikeObjectIds.Add(id);
                return true;
            }

            NonCartLikeObjectIds.Add(id);
            return false;
        }

        internal static void Reset()
        {
            CartLikeObjectIds.Clear();
            NonCartLikeObjectIds.Clear();
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
}
