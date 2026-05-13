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
        public const string PluginVersion = "0.2.8";

        internal static Plugin Instance;
        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ModConfig.Bind(Config);
            ShrinkerCartController.Reset();
            PlayerCartScaleController.Reset();
            EnemyInCartKillController.Reset();
            HostConfigSync.Reset();

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private void Update()
        {
            HostConfigSync.Tick();
            ShrinkerCartController.Tick();
            PlayerCartScaleController.Tick();
        }

        private void OnDestroy()
        {
            ShrinkerCartController.RestoreAll();
            PlayerCartScaleController.RestoreAll();
            VehicleCrushController.RestoreAll();
            HostConfigSync.Reset();

            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }
}
