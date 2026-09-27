namespace ShrinkCart.Game
{
    /// <summary>R.E.P.O. v0.4.x 的代币箱（ItemValuableBox）与外观箱（CosmeticWorldObject）识别。</summary>
    internal static class SpecialBoxDetector
    {
        internal static bool IsSpecialBox(PhysGrabObject item)
        {
            return FindCosmeticWorldObject(item) != null || FindValuableBox(item) != null;
        }

        internal static string Describe(PhysGrabObject item)
        {
            if (FindCosmeticWorldObject(item) != null)
            {
                return "CosmeticWorldObject";
            }

            return FindValuableBox(item) != null ? "ItemValuableBox" : "none";
        }

        internal static CosmeticWorldObject FindCosmeticWorldObject(PhysGrabObject item)
        {
            if (item == null)
            {
                return null;
            }

            CosmeticWorldObject cosmetic = item.GetComponent<CosmeticWorldObject>();
            if (cosmetic != null)
            {
                return cosmetic;
            }

            cosmetic = item.GetComponentInParent<CosmeticWorldObject>();
            return cosmetic != null ? cosmetic : item.GetComponentInChildren<CosmeticWorldObject>(true);
        }

        internal static ItemValuableBox FindValuableBox(PhysGrabObject item)
        {
            if (item == null)
            {
                return null;
            }

            ItemValuableBox box = item.GetComponent<ItemValuableBox>();
            if (box != null)
            {
                return box;
            }

            box = item.GetComponentInParent<ItemValuableBox>();
            return box != null ? box : item.GetComponentInChildren<ItemValuableBox>(true);
        }

        /// <summary>代币箱所属的 PhysGrabObject（自身、父级或子级中第一个）。</summary>
        internal static PhysGrabObject FindOwner(ItemValuableBox box)
        {
            if (box == null)
            {
                return null;
            }

            PhysGrabObject owner = box.GetComponent<PhysGrabObject>();
            if (owner != null)
            {
                return owner;
            }

            owner = box.GetComponentInParent<PhysGrabObject>();
            return owner != null ? owner : box.GetComponentInChildren<PhysGrabObject>(true);
        }
    }
}
