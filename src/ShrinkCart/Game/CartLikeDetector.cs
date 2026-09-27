using UnityEngine;

namespace ShrinkCart.Game
{
    /// <summary>
    /// "车类"物体判定：购物车/小推车本体及其父子物体、载具、C.A.R.T. Cannon / Laser（组件或名称），
    /// 或 itemType 为 cart / vehicle / pocket_cart。
    /// 等价于 0.2.x 中 CartObjectGuard 与 IsPermanentlyExcludedCartItem 两套判定的并集（旧代码先调用前者，实际生效的就是并集）。
    /// 结果由 <see cref="ObjectClassifier"/> 按实例缓存。
    /// </summary>
    internal static class CartLikeDetector
    {
        internal static bool Compute(PhysGrabObject item, string cleanName)
        {
            if (HasOnSelfParentOrChildren<PhysGrabCart>(item) ||
                HasOnSelfParentOrChildren<ItemVehicle>(item) ||
                HasOnSelfParentOrChildren<ItemCartCannon>(item) ||
                HasOnSelfParentOrChildren<ItemCartCannonMain>(item) ||
                HasOnSelfParentOrChildren<ItemCartLaser>(item))
            {
                return true;
            }

            if (cleanName == "Item Cart Cannon" || cleanName == "Item Cart Laser")
            {
                return true;
            }

            ItemAttributes attributes = item.GetComponent<ItemAttributes>();
            if (attributes == null)
            {
                return false;
            }

            SemiFunc.itemType itemType = GameAccess.ItemTypeOf(attributes);
            return itemType == SemiFunc.itemType.cart ||
                   itemType == SemiFunc.itemType.vehicle ||
                   itemType == SemiFunc.itemType.pocket_cart;
        }

        private static bool HasOnSelfParentOrChildren<T>(PhysGrabObject item)
            where T : Component
        {
            return item.GetComponent<T>() != null ||
                   item.GetComponentInParent<T>() != null ||
                   item.GetComponentInChildren<T>(true) != null;
        }
    }
}
