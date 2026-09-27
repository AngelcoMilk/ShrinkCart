using System;
using HarmonyLib;
using ShrinkCart.Bootstrap;
using ShrinkCart.Core;
using UnityEngine;

namespace ShrinkCart.Patches
{
    [PatchFeature(
        FeatureId.Core,
        Frequency = "每次切换关卡一次",
        Side = "所有端调用；只有主机侧已激活的功能会恢复缩放")]
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    internal static class RunManagerChangeLevelPatch
    {
        private static void Prefix()
        {
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (runtime == null)
            {
                return;
            }

            try
            {
                runtime.OnLevelChanging(Time.time);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.Core, "RunManager.ChangeLevel prefix", ex);
            }
        }
    }

    [PatchFeature(
        FeatureId.DeadHeadShrink,
        Frequency = "每次玩家复活一次",
        Side = "所有端调用；只有主机侧死亡头颅功能激活时处理")]
    [HarmonyPatch(typeof(PlayerDeathHead), nameof(PlayerDeathHead.Revive))]
    internal static class PlayerDeathHeadRevivePatch
    {
        private static void Prefix(PlayerDeathHead __instance)
        {
            ShrinkCartRuntime runtime = ShrinkCartRuntime.Current;
            if (runtime == null)
            {
                return;
            }

            try
            {
                runtime.DeadHeads.RestoreBeforeRevive(__instance);
            }
            catch (Exception ex)
            {
                runtime.ReportFailure(FeatureId.DeadHeadShrink, "PlayerDeathHead.Revive prefix", ex);
            }
        }
    }
}
