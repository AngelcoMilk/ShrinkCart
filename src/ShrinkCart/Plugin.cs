using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ShrinkCart
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("Vippy.ScalerCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("nickklmao.repoconfig", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "AngelcoMilk.ShrinkCart";
        public const string PluginName = "ShrinkCart";
        public const string PluginVersion = "0.2.35";

        internal static Plugin Instance;
        internal static ManualLogSource Log;

        private Harmony _harmony;
        private bool _playerScalingWasEnabled;
        private bool _wasHostOrSingleplayer;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Authority.Reset();
            ModConfig.Bind(Config);
            ValuableBoxScaleAdapter.Reset();
            ValuableBoxScaleAdapter.RegisterHandler();
            ShrinkerCartController.Reset();
            PlayerCartScaleController.Reset();
            EnemyInCartKillController.Reset();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            bool hostOrSingleplayer = Authority.IsHostOrSingleplayer();
            if (hostOrSingleplayer && !_wasHostOrSingleplayer)
            {
                CartRegistry.RegisterExistingCarts();
                if (ModConfig.PlayerScalingEnabled())
                {
                    PlayerCartScaleController.Reset();
                    PlayerCartScaleController.RegisterExistingCarts();
                    _playerScalingWasEnabled = true;
                }
            }

            ShrinkerCartController.Tick();
            bool playerScalingEnabled = hostOrSingleplayer && ModConfig.PlayerScalingEnabled();
            if (playerScalingEnabled)
            {
                if (!_playerScalingWasEnabled)
                {
                    PlayerCartScaleController.Reset();
                    PlayerCartScaleController.RegisterExistingCarts();
                }

                PlayerCartScaleController.Tick();
            }
            else if (_playerScalingWasEnabled)
            {
                if (hostOrSingleplayer)
                {
                    PlayerCartScaleController.Disable();
                }
                else
                {
                    PlayerCartScaleController.Reset();
                }
            }

            _playerScalingWasEnabled = playerScalingEnabled;
            _wasHostOrSingleplayer = hostOrSingleplayer;
        }

        private void OnDestroy()
        {
            ShrinkerCartController.RestoreAll();
            PlayerCartScaleController.RestoreAll();
            CartRegistry.Reset();
            Authority.Reset();

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }
}
