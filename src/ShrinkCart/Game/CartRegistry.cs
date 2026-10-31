using System;
using System.Collections.Generic;
using ShrinkCart.Core;
using UnityEngine;

namespace ShrinkCart.Game
{
    /// <summary>
    /// 场上购物车登记表。所有端都登记（只是被动的字典），只有主机侧功能会使用。
    /// 来源：PhysGrabCart.Start postfix、获得主机权限时的全场补登记。车被销毁后由 <see cref="Prune"/> 移除并通知。
    /// </summary>
    internal sealed class CartRegistry
    {
        private readonly Dictionary<int, CartInfo> _carts = new Dictionary<int, CartInfo>();
        private readonly List<int> _removeIds = new List<int>(8);

        /// <summary>车被销毁时通知：参数为车实例 ID 与当前时间。</summary>
        internal event Action<int, float> CartRemoved;

        internal Dictionary<int, CartInfo>.ValueCollection All => _carts.Values;

        internal int Count => _carts.Count;

        internal CartInfo GetOrRegister(PhysGrabCart cart)
        {
            if (cart == null)
            {
                return null;
            }

            int id = cart.GetInstanceID();
            if (!_carts.TryGetValue(id, out CartInfo info))
            {
                info = new CartInfo(cart, id);
                _carts[id] = info;
                ModLog.Debug("Registered cart: " + cart.name + (info.IsSmall ? " (small)" : string.Empty));
            }

            return info;
        }

        internal void RegisterExisting()
        {
            PhysGrabCart[] carts = UnityEngine.Object.FindObjectsByType<PhysGrabCart>(FindObjectsSortMode.None);
            for (int i = 0; i < carts.Length; i++)
            {
                GetOrRegister(carts[i]);
            }
        }

        internal void Prune(float now)
        {
            _removeIds.Clear();
            foreach (KeyValuePair<int, CartInfo> pair in _carts)
            {
                if (pair.Value.Cart == null)
                {
                    _removeIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < _removeIds.Count; i++)
            {
                _carts.Remove(_removeIds[i]);
                Action<int, float> handler = CartRemoved;
                if (handler != null)
                {
                    handler(_removeIds[i], now);
                }
            }
        }

        internal void Clear()
        {
            _carts.Clear();
        }

        internal sealed class CartInfo
        {
            internal CartInfo(PhysGrabCart cart, int id)
            {
                Cart = cart;
                Id = id;
                IsSmall = cart.isSmallCart;
            }

            internal PhysGrabCart Cart { get; }

            internal int Id { get; }

            internal bool IsSmall { get; }

            /// <summary>玩家缩放使用的车内体积，由 PlayerScalingFeature 延迟解析并缓存。</summary>
            internal Transform InCartVolume;
        }
    }
}
