namespace ShrinkCart.Domain
{
    internal enum PlayerScaleState
    {
        Normal,
        Shrunk,
        Grown
    }

    internal enum PlayerCycleAction
    {
        None,
        Restore,
        ApplyShrink,
        ApplyGrow
    }

    /// <summary>
    /// 玩家站车循环（纯逻辑）：原样 → 缩小 → 原样 → 增大 → 原样……
    /// <list type="bullet">
    /// <item>在车中心区域连续站满触发时间后推进一步；每次进入车范围只触发一次，离开车范围后才重新计次。</item>
    /// <item>短暂离开车范围（宽容时间内，例如跳起或踩到货物）不算离开。</item>
    /// <item>缩放被外部解除（受伤/碰撞恢复等）时调用 <see cref="OnExternalRestore"/>，循环照常推进。</item>
    /// </list>
    /// 调用方负责执行返回的动作，并用 <see cref="OnRestored"/> / <see cref="OnApplied"/> 回报成功结果。
    /// </summary>
    internal sealed class PlayerCycle
    {
        private bool _wasInRange;
        private bool _wasInZone;
        private bool _triggeredThisStay;
        private float _zoneEnteredAt;
        private float _lastInRangeAt;

        internal PlayerScaleState State { get; private set; }

        /// <summary>下一次从原样出发时是增大（true）还是缩小（false）。</summary>
        internal bool NextIsGrow { get; private set; }

        internal PlayerCycleAction Step(bool inRange, bool inZone, float now, float exitGraceSeconds, float standTriggerSeconds)
        {
            if (inRange)
            {
                _lastInRangeAt = now;
            }
            else if (_wasInRange && now - _lastInRangeAt <= exitGraceSeconds)
            {
                inRange = true;
            }

            if (!inRange)
            {
                _wasInRange = false;
                _wasInZone = false;
                _triggeredThisStay = false;
                _zoneEnteredAt = 0f;
                _lastInRangeAt = 0f;
                return PlayerCycleAction.None;
            }

            _wasInRange = true;
            if (!inZone)
            {
                _wasInZone = false;
                _zoneEnteredAt = 0f;
                return PlayerCycleAction.None;
            }

            if (!_wasInZone)
            {
                _wasInZone = true;
                _zoneEnteredAt = now;
                _lastInRangeAt = now;
                return PlayerCycleAction.None;
            }

            if (_triggeredThisStay || now - _zoneEnteredAt < standTriggerSeconds)
            {
                return PlayerCycleAction.None;
            }

            _triggeredThisStay = true;
            if (State != PlayerScaleState.Normal)
            {
                return PlayerCycleAction.Restore;
            }

            return NextIsGrow ? PlayerCycleAction.ApplyGrow : PlayerCycleAction.ApplyShrink;
        }

        internal void OnRestored()
        {
            AdvanceToNormal();
        }

        internal void OnExternalRestore()
        {
            AdvanceToNormal();
        }

        internal void OnApplied(PlayerCycleAction action)
        {
            if (action == PlayerCycleAction.ApplyGrow)
            {
                State = PlayerScaleState.Grown;
            }
            else if (action == PlayerCycleAction.ApplyShrink)
            {
                State = PlayerScaleState.Shrunk;
            }
        }

        private void AdvanceToNormal()
        {
            if (State == PlayerScaleState.Normal)
            {
                return;
            }

            NextIsGrow = State == PlayerScaleState.Shrunk;
            State = PlayerScaleState.Normal;
        }
    }
}
