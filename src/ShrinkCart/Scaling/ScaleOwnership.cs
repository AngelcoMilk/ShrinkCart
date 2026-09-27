using System.Collections.Generic;
using UnityEngine;

namespace ShrinkCart.Scaling
{
    internal enum ScaleKind
    {
        Item,
        ValuableBox,
        DeadHead,
        Player
    }

    /// <summary>
    /// 记录 ShrinkCart 自己施加的每个缩放（目标、种类、倍率）。只恢复记录在案且仍与记录一致的缩放；
    /// 不再像 0.2.x 那样用"参数长得像 ShrinkCart"来认领别人的缩放。
    /// </summary>
    internal sealed class ScaleOwnership
    {
        private readonly Dictionary<int, OwnedScale> _owned = new Dictionary<int, OwnedScale>();

        internal int Count => _owned.Count;

        internal void Register(GameObject target, ScaleKind kind, float factor)
        {
            _owned[target.GetInstanceID()] = new OwnedScale(target, kind, factor);
        }

        internal bool TryGet(int id, out OwnedScale owned)
        {
            return _owned.TryGetValue(id, out owned);
        }

        internal bool IsOwned(int id)
        {
            return _owned.ContainsKey(id);
        }

        internal void Release(int id)
        {
            _owned.Remove(id);
        }

        /// <summary>换关兜底：各功能已恢复并释放自己的记录，这里清掉目标已销毁等情况遗留的条目。</summary>
        internal void Clear()
        {
            _owned.Clear();
        }

        internal sealed class OwnedScale
        {
            internal OwnedScale(GameObject target, ScaleKind kind, float factor)
            {
                Target = target;
                Kind = kind;
                Factor = factor;
            }

            internal GameObject Target { get; }

            internal ScaleKind Kind { get; }

            /// <summary>施加时的倍率。ScalerCore 只在 Factor ≤ 0 时替换为默认值，因此 CurrentOptions.Factor 会原样保留它。</summary>
            internal float Factor { get; }
        }
    }
}
