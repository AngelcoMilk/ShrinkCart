using System.Collections.Generic;

namespace ShrinkCart.Domain
{
    /// <summary>按实例 ID 记录"在某时刻之前不要再处理"的冷却表（重缩冷却、秒杀重试冷却）。</summary>
    internal sealed class RetryCooldown
    {
        private readonly Dictionary<int, float> _until = new Dictionary<int, float>();
        private readonly List<int> _expired = new List<int>(8);

        internal int Count => _until.Count;

        internal bool IsCooling(int id, float now)
        {
            if (!_until.TryGetValue(id, out float until))
            {
                return false;
            }

            if (now < until)
            {
                return true;
            }

            _until.Remove(id);
            return false;
        }

        internal void Start(int id, float now, float durationSeconds)
        {
            _until[id] = now + durationSeconds;
        }

        internal void Cancel(int id)
        {
            _until.Remove(id);
        }

        /// <summary>清理已过期的条目，避免长时间运行时表无限增长。</summary>
        internal void Prune(float now)
        {
            if (_until.Count == 0)
            {
                return;
            }

            _expired.Clear();
            foreach (KeyValuePair<int, float> pair in _until)
            {
                if (now >= pair.Value)
                {
                    _expired.Add(pair.Key);
                }
            }

            for (int i = 0; i < _expired.Count; i++)
            {
                _until.Remove(_expired[i]);
            }
        }

        internal void Clear()
        {
            _until.Clear();
        }
    }
}
