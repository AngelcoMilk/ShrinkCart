using UnityEngine;

namespace ShrinkCart
{
    internal static class Authority
    {
        private const float CheckIntervalSeconds = 0.25f;

        private static bool _cachedIsHostOrSingleplayer = true;
        private static float _nextCheckTime;

        internal static bool IsHostOrSingleplayer()
        {
            float now = Time.unscaledTime;
            if (now < _nextCheckTime)
            {
                return _cachedIsHostOrSingleplayer;
            }

            _nextCheckTime = now + CheckIntervalSeconds;
            try
            {
                _cachedIsHostOrSingleplayer = SemiFunc.IsMasterClientOrSingleplayer();
            }
            catch
            {
                _cachedIsHostOrSingleplayer = true;
            }

            return _cachedIsHostOrSingleplayer;
        }

        internal static void Reset()
        {
            _nextCheckTime = 0.0f;
            _cachedIsHostOrSingleplayer = true;
        }
    }
}
