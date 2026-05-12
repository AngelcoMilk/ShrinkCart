using BepInEx.Configuration;
using UnityEngine;

namespace ShrinkCart
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> CartShrinkingEnabled;
        internal static ConfigEntry<float> CartScaleFactor;
        internal static ConfigEntry<float> CartScaleSpeed;
        internal static ConfigEntry<float> RestoreGraceSeconds;
        internal static ConfigEntry<bool> PreserveCartMass;
        internal static ConfigEntry<bool> ShrinkNonValuableItems;
        internal static ConfigEntry<bool> SuppressValuableDamageRestore;

        internal static ConfigEntry<bool> VehicleCrushInstantKill;
        internal static ConfigEntry<bool> VehicleCrushKillEnemies;
        internal static ConfigEntry<bool> DebugLogging;

        internal static void Bind(ConfigFile config)
        {
            CartShrinkingEnabled = config.Bind(
                "Cart",
                "Enabled",
                true,
                "Shrink supported objects while they are inside a cart, then restore them after removal.");

            CartScaleFactor = config.Bind(
                "Cart",
                "ScaleFactor",
                0.4f,
                "Target size for cart contents. 0.4 means 40% of original size.");

            CartScaleSpeed = config.Bind(
                "Cart",
                "ScaleSpeed",
                2.5f,
                "ScalerCore animation speed for shrink and restore transitions.");

            RestoreGraceSeconds = config.Bind(
                "Cart",
                "RestoreGraceSeconds",
                0.75f,
                "Seconds an object can be absent from cart scans before this mod restores it.");

            PreserveCartMass = config.Bind(
                "Cart",
                "PreserveMass",
                true,
                "Keep original rigidbody mass while visually shrunken so cart weight stays honest.");

            ShrinkNonValuableItems = config.Bind(
                "Cart",
                "ShrinkNonValuableItems",
                true,
                "Also shrink normal items. Valuables are always supported.");

            SuppressValuableDamageRestore = config.Bind(
                "Cart",
                "SuppressValuableDamageRestore",
                true,
                "Prevent valuables from popping back to full size just because they bump inside the cart.");

            VehicleCrushInstantKill = config.Bind(
                "VehicleCrush",
                "InstantKillPlayers",
                false,
                "When enabled, active vehicle impact hurt colliders instantly kill players they run over.");

            VehicleCrushKillEnemies = config.Bind(
                "VehicleCrush",
                "InstantKillEnemies",
                false,
                "When enabled together with InstantKillPlayers, vehicle impact hurt colliders also instantly kill enemies.");

            DebugLogging = config.Bind(
                "Diagnostics",
                "DebugLogging",
                false,
                "Write extra cart shrink and restore messages to the BepInEx log.");
        }

        internal static float SafeScaleFactor()
        {
            return Mathf.Clamp(CartScaleFactor.Value, 0.05f, 1.0f);
        }

        internal static float SafeScaleSpeed()
        {
            return Mathf.Clamp(CartScaleSpeed.Value, 0.1f, 20.0f);
        }

        internal static float SafeRestoreGraceSeconds()
        {
            return Mathf.Clamp(RestoreGraceSeconds.Value, 0.05f, 10.0f);
        }
    }
}
