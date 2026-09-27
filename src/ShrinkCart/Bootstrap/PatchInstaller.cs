using System;
using System.Reflection;
using HarmonyLib;
using ShrinkCart.Core;

namespace ShrinkCart.Bootstrap
{
    /// <summary>
    /// 逐个安装本程序集中的 Harmony patch 类。某个类失败（目标方法在新版游戏中改名、签名变化等）
    /// 只会让它 [PatchFeature] 标注的功能不可用，其余功能照常运行；不再像 PatchAll 那样全有或全无。
    /// </summary>
    internal static class PatchInstaller
    {
        internal static void Install(Harmony harmony, Assembly assembly, FeatureAvailability availability, out int installed, out int failed)
        {
            installed = 0;
            failed = 0;
            foreach (Type type in AccessTools.GetTypesFromAssembly(assembly))
            {
                if (!type.IsDefined(typeof(HarmonyPatch), false))
                {
                    continue;
                }

                PatchFeatureAttribute feature = (PatchFeatureAttribute)Attribute.GetCustomAttribute(type, typeof(PatchFeatureAttribute));
                FeatureId owner = feature != null ? feature.Feature : FeatureId.Core;
                if (feature == null)
                {
                    ModLog.Warn("Harmony patch class without [PatchFeature], treated as Core: " + type.FullName);
                }

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    installed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    string reason = "patch " + type.Name + " failed: " + ex.GetBaseException().Message;
                    availability.MarkUnavailable(owner, reason);
                    ModLog.Warn(reason);
                }
            }
        }
    }
}
