using System;
using BepInEx;
using HarmonyLib;
using ShrinkCart.Bootstrap;
using ShrinkCart.Config;
using ShrinkCart.Core;
using ShrinkCart.Game;
using ShrinkCart.Scaling;
using UnityEngine;

namespace ShrinkCart
{
    /// <summary>
    /// BepInEx 入口。启动顺序：日志 → 配置绑定与迁移 → 游戏成员自检 → 代币箱 handler → 逐类安装 patch → 组合根 → 启动摘要。
    /// 任一步骤抛出异常时插件保持停用（所有 patch 在 Current 为 null 时直接放行原版逻辑）。
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("Vippy.ScalerCore", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("nickklmao.repoconfig", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        private ShrinkCartRuntime _runtime;

        private void Awake()
        {
            ModLog.Initialize(Logger, () => ModConfig.DebugLoggingEnabled);
            try
            {
                ModConfig.Bind(base.Config);

                FeatureAvailability availability = new FeatureAvailability();
                int missingMembers = GameAccessSelfCheck.Run(availability);
                ValuableBoxSupport.RegisterHandler(availability);

                _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
                PatchInstaller.Install(_harmony, typeof(Plugin).Assembly, availability, out int installed, out int failed);

                _runtime = new ShrinkCartRuntime(availability);
                ShrinkCartRuntime.Current = _runtime;

                ModLog.Info(
                    MyPluginInfo.PLUGIN_NAME + " " + MyPluginInfo.PLUGIN_VERSION + " loaded: patches " + installed + "/" + (installed + failed) +
                    ", missing game members " + missingMembers + ", config language " + (ModConfig.IsEnglish ? "English" : "中文") +
                    ". " + availability.Describe());
            }
            catch (Exception ex)
            {
                ShrinkCartRuntime.Current = null;
                _runtime = null;
                ModLog.Error("ShrinkCart failed to initialize and stays inactive: " + ex);
            }
        }

        private void Update()
        {
            if (_runtime != null)
            {
                _runtime.Update(Time.time, Time.unscaledTime);
            }
        }

        private void OnDestroy()
        {
            if (_runtime != null)
            {
                _runtime.Shutdown(Time.time);
                _runtime = null;
            }

            ShrinkCartRuntime.Current = null;
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                _harmony = null;
            }
        }
    }
}
