using System.Collections.Generic;

namespace ShrinkCart.Domain
{
    /// <summary>
    /// 跟踪"已被本 mod 缩小的对象是否仍在车内"（纯逻辑，只使用实例 ID 与时间）。
    /// <list type="bullet">
    /// <item>某辆车完成一次真实扫描但没看到对象 → 从此刻开始计离车时间（AbsentSince）。</item>
    /// <item>任何在场信号（trigger 进车、扫描看到）都会清除离车计时，并把对象归到对应的车。</item>
    /// <item>车被销毁 → 该车上的对象开始计时。</item>
    /// <item>车没有扫描（原版在附近没有玩家时不扫描）→ 对象保持冻结，不会被误恢复。</item>
    /// </list>
    /// 离车时间达到防抖阈值的对象由 <see cref="CollectDue"/> 返回，由调用方负责真正恢复。
    /// 调用方每帧用 <see cref="HasDue"/>（O(1)）判断是否需要收集，使恢复在到期时刻准时发生（与 0.2.x 的手感一致），
    /// 而不是等到下一次维护间隔。
    /// </summary>
    internal sealed class PresenceTracker
    {
        private sealed class Entry
        {
            internal int CartId;
            internal float LastSeen;
            internal float AbsentSince = float.NaN;
            internal int SeenInScan;
        }

        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        private int _scanCounter;
        private int _activeScanCartId;
        private int _activeScanSerial;

        // 所有处于离车计时中的对象里最早的开始时间。只会偏早（对象又被看到或被移除后未重算），不会偏晚。
        private float _earliestAbsentSince = float.NaN;

        internal int Count => _entries.Count;

        internal bool Contains(int id)
        {
            return _entries.ContainsKey(id);
        }

        internal bool IsAbsent(int id)
        {
            return _entries.TryGetValue(id, out Entry entry) && !float.IsNaN(entry.AbsentSince);
        }

        /// <summary>开始跟踪一个刚在 <paramref name="cartId"/> 中缩小的对象。</summary>
        internal void Track(int id, int cartId, float now)
        {
            _entries[id] = new Entry
            {
                CartId = cartId,
                LastSeen = now,
                SeenInScan = CurrentScanSerialFor(cartId)
            };
        }

        /// <summary>在场信号。对象未被跟踪时返回 false。</summary>
        internal bool TryMarkSeen(int id, int cartId, float now)
        {
            if (!_entries.TryGetValue(id, out Entry entry))
            {
                return false;
            }

            entry.CartId = cartId;
            entry.LastSeen = now;
            entry.AbsentSince = float.NaN;
            entry.SeenInScan = CurrentScanSerialFor(cartId);
            return true;
        }

        /// <summary>某辆车开始处理一次真实扫描结果；随后对扫描到的对象调用 <see cref="TryMarkSeen"/>。</summary>
        internal void BeginScan(int cartId)
        {
            _scanCounter++;
            if (_scanCounter == 0)
            {
                _scanCounter = 1;
            }

            _activeScanCartId = cartId;
            _activeScanSerial = _scanCounter;
        }

        /// <summary>扫描结束：归属于该车、但本轮没被看到的对象开始计离车时间。</summary>
        internal void EndScan(int cartId, float now)
        {
            int serial = _activeScanSerial != 0 && _activeScanCartId == cartId ? _activeScanSerial : -1;
            foreach (Entry entry in _entries.Values)
            {
                if (entry.CartId == cartId && entry.SeenInScan != serial && float.IsNaN(entry.AbsentSince))
                {
                    MarkAbsent(entry, now);
                }
            }

            _activeScanCartId = 0;
            _activeScanSerial = 0;
        }

        internal void OnCartRemoved(int cartId, float now)
        {
            foreach (Entry entry in _entries.Values)
            {
                if (entry.CartId == cartId && float.IsNaN(entry.AbsentSince))
                {
                    MarkAbsent(entry, now);
                }
            }
        }

        /// <summary>
        /// 是否可能有对象的离车时间已达 <paramref name="debounceSeconds"/>。O(1)，供每帧调用；
        /// 返回 true 时再调用 <see cref="CollectDue"/>（偶尔会是已失效的 true，收集结果为空，无副作用）。
        /// </summary>
        internal bool HasDue(float now, float debounceSeconds)
        {
            return !float.IsNaN(_earliestAbsentSince) && now - _earliestAbsentSince >= debounceSeconds;
        }

        /// <summary>收集离车时间已达 <paramref name="debounceSeconds"/> 的对象 ID（结果写入 <paramref name="result"/>），并重算最早离车时间。</summary>
        internal void CollectDue(float now, float debounceSeconds, List<int> result)
        {
            result.Clear();
            float earliest = float.NaN;
            foreach (KeyValuePair<int, Entry> pair in _entries)
            {
                float absentSince = pair.Value.AbsentSince;
                if (float.IsNaN(absentSince))
                {
                    continue;
                }

                if (now - absentSince >= debounceSeconds)
                {
                    result.Add(pair.Key);
                }

                // 到期对象也计入：调用方移除它们之后，下一次收集会把这个值重算为剩余对象的最早时间。
                if (float.IsNaN(earliest) || absentSince < earliest)
                {
                    earliest = absentSince;
                }
            }

            _earliestAbsentSince = earliest;
        }

        internal void CopyIds(List<int> result)
        {
            result.Clear();
            foreach (int id in _entries.Keys)
            {
                result.Add(id);
            }
        }

        internal void Remove(int id)
        {
            _entries.Remove(id);
        }

        internal void Clear()
        {
            _entries.Clear();
            _activeScanCartId = 0;
            _activeScanSerial = 0;
            _earliestAbsentSince = float.NaN;
        }

        private void MarkAbsent(Entry entry, float now)
        {
            entry.AbsentSince = now;
            if (float.IsNaN(_earliestAbsentSince) || now < _earliestAbsentSince)
            {
                _earliestAbsentSince = now;
            }
        }

        private int CurrentScanSerialFor(int cartId)
        {
            return _activeScanSerial != 0 && cartId == _activeScanCartId ? _activeScanSerial : 0;
        }
    }
}
