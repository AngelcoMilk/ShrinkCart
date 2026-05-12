using System;
using System.Globalization;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace ShrinkCart
{
    internal static class HostConfigSync
    {
        private const string ConfigKey = "ShrinkCart.HostConfig.v1";
        private const float SyncIntervalSeconds = 0.5f;
        private const int CategoryCount = 13;

        private sealed class Snapshot
        {
            internal bool CartEnabled;
            internal bool HideScaleFlash;
            internal bool ShrinkNonValuableItems;
            internal bool ShrinkShopPlayerItems;
            internal bool EnemyInCartInstantKill;
            internal float CartLeaveDebounceSeconds;
            internal float ReshrinkCooldownSeconds;
            internal readonly bool[] Enabled = new bool[CategoryCount];
            internal readonly float[] Factors = new float[CategoryCount];
        }

        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly StringBuilder Builder = new StringBuilder(512);

        private static Snapshot _remoteSnapshot;
        private static string _lastPublishedPayload;
        private static string _lastReadPayload;
        private static float _nextSyncTime;

        internal static void Reset()
        {
            _remoteSnapshot = null;
            _lastPublishedPayload = null;
            _lastReadPayload = null;
            _nextSyncTime = 0.0f;
        }

        internal static void Tick()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                _remoteSnapshot = null;
                _lastReadPayload = null;
                return;
            }

            if (UnityEngine.Time.time < _nextSyncTime)
            {
                return;
            }

            _nextSyncTime = UnityEngine.Time.time + SyncIntervalSeconds;

            if (PhotonNetwork.IsMasterClient)
            {
                PublishIfChanged();
            }
            else
            {
                ReadRemoteSnapshot();
            }
        }

        internal static bool EffectiveHideScaleFlash()
        {
            if (UsesLocalConfig())
            {
                return ModConfig.HideScaleFlash.Value;
            }

            return _remoteSnapshot != null ? _remoteSnapshot.HideScaleFlash : ModConfig.HideScaleFlash.Value;
        }

        internal static bool ShouldMarkCategoryForVisual(ShrinkCategory category)
        {
            if (UsesLocalConfig())
            {
                float ignored;
                return ModConfig.CartShrinkingEnabled.Value && ModConfig.TryGetScaleFactor(category, out ignored);
            }

            if (_remoteSnapshot == null)
            {
                return true;
            }

            if (!_remoteSnapshot.CartEnabled)
            {
                return false;
            }

            int index = (int)category;
            if (index < 0 || index >= CategoryCount)
            {
                return true;
            }

            return _remoteSnapshot.Enabled[index];
        }

        internal static bool ShouldMarkShopPlayerItemForVisual()
        {
            if (UsesLocalConfig())
            {
                return ModConfig.CartShrinkingEnabled.Value && ModConfig.ShrinkShopPlayerItems.Value;
            }

            if (_remoteSnapshot == null)
            {
                return true;
            }

            return _remoteSnapshot.CartEnabled && _remoteSnapshot.ShrinkShopPlayerItems;
        }

        internal static bool ShouldMarkNonValuableItemForVisual()
        {
            if (UsesLocalConfig())
            {
                return ModConfig.CartShrinkingEnabled.Value && ModConfig.ShrinkNonValuableItems.Value;
            }

            if (_remoteSnapshot == null)
            {
                return true;
            }

            return _remoteSnapshot.CartEnabled && _remoteSnapshot.ShrinkNonValuableItems;
        }

        private static bool UsesLocalConfig()
        {
            return !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
        }

        private static void PublishIfChanged()
        {
            string payload = BuildLocalPayload();
            if (payload == _lastPublishedPayload)
            {
                return;
            }

            Hashtable properties = new Hashtable();
            properties[ConfigKey] = payload;
            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
            _lastPublishedPayload = payload;
        }

        private static void ReadRemoteSnapshot()
        {
            object value;
            if (!PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ConfigKey, out value))
            {
                _remoteSnapshot = null;
                _lastReadPayload = null;
                return;
            }

            string payload = value as string;
            if (string.IsNullOrEmpty(payload) || payload == _lastReadPayload)
            {
                return;
            }

            Snapshot snapshot;
            if (!TryParseSnapshot(payload, out snapshot))
            {
                return;
            }

            _remoteSnapshot = snapshot;
            _lastReadPayload = payload;
        }

        private static string BuildLocalPayload()
        {
            Builder.Length = 0;
            Builder.Append("SC025");
            Append(ModConfig.CartShrinkingEnabled.Value);
            Append(ModConfig.HideScaleFlash.Value);
            Append(ModConfig.ShrinkNonValuableItems.Value);
            Append(ModConfig.ShrinkShopPlayerItems.Value);
            Append(ModConfig.EnemyInCartInstantKill.Value);
            Append(ModConfig.SafeCartLeaveDebounceSeconds());
            Append(ModConfig.SafeReshrinkCooldownSeconds());

            for (int i = 0; i < CategoryCount; i++)
            {
                bool enabled;
                float factor;
                GetLocalCategoryConfig((ShrinkCategory)i, out enabled, out factor);
                Append(enabled);
                Append(factor);
            }

            return Builder.ToString();
        }

        private static bool TryParseSnapshot(string payload, out Snapshot snapshot)
        {
            snapshot = null;
            string[] parts = payload.Split('|');
            int expected = 8 + CategoryCount * 2;
            if (parts.Length != expected || parts[0] != "SC025")
            {
                return false;
            }

            Snapshot parsed = new Snapshot();
            int index = 1;
            if (!TryParseBool(parts[index++], out parsed.CartEnabled) ||
                !TryParseBool(parts[index++], out parsed.HideScaleFlash) ||
                !TryParseBool(parts[index++], out parsed.ShrinkNonValuableItems) ||
                !TryParseBool(parts[index++], out parsed.ShrinkShopPlayerItems) ||
                !TryParseBool(parts[index++], out parsed.EnemyInCartInstantKill) ||
                !TryParseFloat(parts[index++], out parsed.CartLeaveDebounceSeconds) ||
                !TryParseFloat(parts[index++], out parsed.ReshrinkCooldownSeconds))
            {
                return false;
            }

            for (int i = 0; i < CategoryCount; i++)
            {
                if (!TryParseBool(parts[index++], out parsed.Enabled[i]) ||
                    !TryParseFloat(parts[index++], out parsed.Factors[i]))
                {
                    return false;
                }
            }

            snapshot = parsed;
            return true;
        }

        private static void GetLocalCategoryConfig(ShrinkCategory category, out bool enabled, out float factor)
        {
            switch (category)
            {
                case ShrinkCategory.Tiny:
                    enabled = ModConfig.TinyEnabled.Value;
                    factor = ModConfig.TinyScaleFactor.Value;
                    break;
                case ShrinkCategory.Small:
                    enabled = ModConfig.SmallEnabled.Value;
                    factor = ModConfig.SmallScaleFactor.Value;
                    break;
                case ShrinkCategory.Medium:
                    enabled = ModConfig.MediumEnabled.Value;
                    factor = ModConfig.MediumScaleFactor.Value;
                    break;
                case ShrinkCategory.Big:
                    enabled = ModConfig.BigEnabled.Value;
                    factor = ModConfig.BigScaleFactor.Value;
                    break;
                case ShrinkCategory.Wide:
                    enabled = ModConfig.WideEnabled.Value;
                    factor = ModConfig.WideScaleFactor.Value;
                    break;
                case ShrinkCategory.Tall:
                    enabled = ModConfig.TallEnabled.Value;
                    factor = ModConfig.TallScaleFactor.Value;
                    break;
                case ShrinkCategory.VeryTall:
                    enabled = ModConfig.VeryTallEnabled.Value;
                    factor = ModConfig.VeryTallScaleFactor.Value;
                    break;
                case ShrinkCategory.EnemyOrbSmall:
                    enabled = ModConfig.EnemyOrbEnabled.Value;
                    factor = ModConfig.EnemyOrbSmallScaleFactor.Value;
                    break;
                case ShrinkCategory.EnemyOrbMedium:
                    enabled = ModConfig.EnemyOrbEnabled.Value;
                    factor = ModConfig.EnemyOrbMediumScaleFactor.Value;
                    break;
                case ShrinkCategory.EnemyOrbBig:
                    enabled = ModConfig.EnemyOrbEnabled.Value;
                    factor = ModConfig.EnemyOrbBigScaleFactor.Value;
                    break;
                case ShrinkCategory.EnemyOrbBerserker:
                    enabled = ModConfig.EnemyOrbEnabled.Value;
                    factor = ModConfig.EnemyOrbBerserkerScaleFactor.Value;
                    break;
                case ShrinkCategory.Surplus:
                    enabled = ModConfig.SurplusEnabled.Value;
                    factor = ModConfig.SurplusScaleFactor.Value;
                    break;
                default:
                    enabled = true;
                    factor = ModConfig.FallbackScaleFactor.Value;
                    break;
            }

            factor = UnityEngine.Mathf.Clamp(factor, 0.05f, 1.0f);
        }

        private static void Append(bool value)
        {
            Builder.Append('|');
            Builder.Append(value ? "1" : "0");
        }

        private static void Append(float value)
        {
            Builder.Append('|');
            Builder.Append(value.ToString("R", Invariant));
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (value == "1")
            {
                result = true;
                return true;
            }

            if (value == "0")
            {
                result = false;
                return true;
            }

            result = false;
            return false;
        }

        private static bool TryParseFloat(string value, out float result)
        {
            return float.TryParse(value, NumberStyles.Float, Invariant, out result);
        }
    }
}
