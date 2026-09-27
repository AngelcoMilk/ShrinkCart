using NUnit.Framework;
using ShrinkCart.Domain;

namespace ShrinkCart.Tests.Domain
{
    public sealed class PlayerCycleTests
    {
        private const float Grace = 0.6f;
        private const float Trigger = 2f;

        [Test]
        public void FullCycle_ShrinkRestoreGrowRestore()
        {
            PlayerCycle cycle = new PlayerCycle();

            Assert.That(StandUntilTriggered(cycle, 0f), Is.EqualTo(PlayerCycleAction.ApplyShrink));
            cycle.OnApplied(PlayerCycleAction.ApplyShrink);
            Assert.That(cycle.State, Is.EqualTo(PlayerScaleState.Shrunk));

            Leave(cycle, 10f);
            Assert.That(StandUntilTriggered(cycle, 20f), Is.EqualTo(PlayerCycleAction.Restore));
            cycle.OnRestored();
            Assert.That(cycle.State, Is.EqualTo(PlayerScaleState.Normal));
            Assert.That(cycle.NextIsGrow, Is.True);

            Leave(cycle, 30f);
            Assert.That(StandUntilTriggered(cycle, 40f), Is.EqualTo(PlayerCycleAction.ApplyGrow));
            cycle.OnApplied(PlayerCycleAction.ApplyGrow);
            Assert.That(cycle.State, Is.EqualTo(PlayerScaleState.Grown));

            Leave(cycle, 50f);
            Assert.That(StandUntilTriggered(cycle, 60f), Is.EqualTo(PlayerCycleAction.Restore));
            cycle.OnRestored();
            Assert.That(cycle.NextIsGrow, Is.False);
        }

        [Test]
        public void TriggersOnlyOncePerStay()
        {
            PlayerCycle cycle = new PlayerCycle();
            Assert.That(StandUntilTriggered(cycle, 0f), Is.EqualTo(PlayerCycleAction.ApplyShrink));
            cycle.OnApplied(PlayerCycleAction.ApplyShrink);

            // 继续站在中心区域不会再次触发，必须离开车范围后重新进入。
            Assert.That(cycle.Step(true, true, 10f, Grace, Trigger), Is.EqualTo(PlayerCycleAction.None));
            Assert.That(cycle.Step(true, true, 20f, Grace, Trigger), Is.EqualTo(PlayerCycleAction.None));
        }

        [Test]
        public void LeavingCenterZoneResetsTimer()
        {
            PlayerCycle cycle = new PlayerCycle();
            cycle.Step(true, true, 0f, Grace, Trigger);
            cycle.Step(true, true, 1.5f, Grace, Trigger);
            cycle.Step(true, false, 1.6f, Grace, Trigger);
            cycle.Step(true, true, 1.7f, Grace, Trigger);

            Assert.That(cycle.Step(true, true, 3.0f, Grace, Trigger), Is.EqualTo(PlayerCycleAction.None), "timer restarted at 1.7");
            Assert.That(cycle.Step(true, true, 3.8f, Grace, Trigger), Is.EqualTo(PlayerCycleAction.ApplyShrink));
        }

        [Test]
        public void ShortExitWithinGrace_KeepsStay()
        {
            PlayerCycle cycle = new PlayerCycle();
            StandUntilTriggered(cycle, 0f);
            cycle.OnApplied(PlayerCycleAction.ApplyShrink);

            // 一直站着，然后短暂离开（宽容时间内）：不算离车，所以不会重新计次。
            cycle.Step(true, true, 4.9f, Grace, Trigger);
            cycle.Step(false, false, 5.0f, Grace, Trigger);
            cycle.Step(true, true, 5.3f, Grace, Trigger);
            Assert.That(cycle.Step(true, true, 8.0f, Grace, Trigger), Is.EqualTo(PlayerCycleAction.None));
        }

        [Test]
        public void ExternalRestore_AdvancesCycle()
        {
            PlayerCycle cycle = new PlayerCycle();
            StandUntilTriggered(cycle, 0f);
            cycle.OnApplied(PlayerCycleAction.ApplyShrink);

            cycle.OnExternalRestore();

            Assert.That(cycle.State, Is.EqualTo(PlayerScaleState.Normal));
            Assert.That(cycle.NextIsGrow, Is.True);
        }

        [Test]
        public void FailedApply_KeepsNormalState()
        {
            PlayerCycle cycle = new PlayerCycle();
            Assert.That(StandUntilTriggered(cycle, 0f), Is.EqualTo(PlayerCycleAction.ApplyShrink));

            // 调用方未回报成功：状态保持原样，下一次仍从缩小开始。
            Assert.That(cycle.State, Is.EqualTo(PlayerScaleState.Normal));
            Assert.That(cycle.NextIsGrow, Is.False);
        }

        private static PlayerCycleAction StandUntilTriggered(PlayerCycle cycle, float start)
        {
            cycle.Step(true, true, start, Grace, Trigger);
            return cycle.Step(true, true, start + Trigger, Grace, Trigger);
        }

        private static void Leave(PlayerCycle cycle, float now)
        {
            cycle.Step(false, false, now, Grace, Trigger);
            cycle.Step(false, false, now + Grace + 1f, Grace, Trigger);
        }
    }
}
