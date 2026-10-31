using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ShrinkCart.Core;
using UnityEngine;

namespace ShrinkCart.Game
{
    /// <summary>
    /// 对 Assembly-CSharp 非公开成员的唯一访问点。编译时引用 publicize 后的 Assembly-CSharp（仅存在于 obj/，不入库），
    /// 因此这里是强类型访问，游戏更新改名/删字段会直接变成编译错误。
    /// <para>约束（BuildTools 构建期强制）：</para>
    /// <list type="number">
    /// <item>ShrinkCart 中任何对原版非公开成员的访问都只能出现在本类。</item>
    /// <item>每个方法用 <see cref="GameMemberAttribute"/> 声明它触及的成员，声明必须与 IL 实际引用一致。</item>
    /// <item>每个方法都标记 NoInlining：成员缺失时只有调用该方法才会触发 JIT 异常，
    /// 而启动自检已让依赖它的功能不激活，调用方的 try/catch 兜底其余情况。</item>
    /// </list>
    /// </summary>
    internal static class GameAccess
    {
        // ---- 车内观测（CartObservation）----

        [GameMember(typeof(PhysGrabCart), "itemsInCart", typeof(List<PhysGrabObject>), FeatureId.CartObservation)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static List<PhysGrabObject> ItemsInCart(PhysGrabCart cart)
        {
            return cart.itemsInCart;
        }

        /// <summary>原版每次真实扫描后把计时器重置为 0.5 秒；≤0 表示下一次 ObjectsInCart 会扫描（前提是 12 m 内有玩家）。</summary>
        [GameMember(typeof(PhysGrabCart), "objectInCartCheckTimer", typeof(float), FeatureId.CartObservation)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static float CartCheckTimer(PhysGrabCart cart)
        {
            return cart.objectInCartCheckTimer;
        }

        [GameMember(typeof(ItemAttributes), "itemType", typeof(SemiFunc.itemType), FeatureId.CartObservation)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static SemiFunc.itemType ItemTypeOf(ItemAttributes attributes)
        {
            return attributes.itemType;
        }

        [GameMember(typeof(PhysGrabObject), "isGun", typeof(bool), FeatureId.CartObservation)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool IsGun(PhysGrabObject item)
        {
            return item.isGun;
        }

        // ---- 车类物品拦截（CartNestingGuard）----

        [GameMember(typeof(PhysGrabCart), "itemsInCartCount", typeof(int), FeatureId.CartNestingGuard)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int ItemsInCartCount(PhysGrabCart cart)
        {
            return cart.itemsInCartCount;
        }

        [GameMember(typeof(PhysGrabCart), "itemsInCartCount", typeof(int), FeatureId.CartNestingGuard)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SetItemsInCartCount(PhysGrabCart cart, int value)
        {
            cart.itemsInCartCount = value;
        }

        [GameMember(typeof(PhysGrabCart), "haulCurrent", typeof(int), FeatureId.CartNestingGuard)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int HaulCurrent(PhysGrabCart cart)
        {
            return cart.haulCurrent;
        }

        [GameMember(typeof(PhysGrabCart), "haulCurrent", typeof(int), FeatureId.CartNestingGuard)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SetHaulCurrent(PhysGrabCart cart, int value)
        {
            cart.haulCurrent = value;
        }

        [GameMember(typeof(ValuableObject), "dollarValueCurrent", typeof(float), FeatureId.CartNestingGuard)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static float DollarValueCurrent(ValuableObject valuable)
        {
            return valuable.dollarValueCurrent;
        }

        // ---- 敌人进车秒杀（EnemyCrush）----

        [GameMember(typeof(Enemy), "Health", typeof(EnemyHealth), FeatureId.EnemyCrush)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static EnemyHealth HealthOf(Enemy enemy)
        {
            return enemy.Health;
        }

        [GameMember(typeof(EnemyHealth), "healthCurrent", typeof(int), FeatureId.EnemyCrush)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int CurrentHealth(EnemyHealth health)
        {
            return health.healthCurrent;
        }

        [GameMember(typeof(EnemyHealth), "dead", typeof(bool), FeatureId.EnemyCrush)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool IsDead(EnemyHealth health)
        {
            return health.dead;
        }

        // ---- 死亡头颅（DeadHeadShrink）----

        [GameMember(typeof(PlayerDeathHead), "physGrabObject", typeof(PhysGrabObject), FeatureId.DeadHeadShrink)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static PhysGrabObject HeadPhysGrabObject(PlayerDeathHead head)
        {
            return head.physGrabObject;
        }

        [GameMember(typeof(PlayerDeathHead), "triggered", typeof(bool), FeatureId.DeadHeadShrink)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool IsHeadTriggered(PlayerDeathHead head)
        {
            return head.triggered;
        }

        [GameMember(typeof(PlayerDeathHead), "inExtractionPoint", typeof(bool), FeatureId.DeadHeadShrink)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool IsHeadInExtractionPoint(PlayerDeathHead head)
        {
            return head.inExtractionPoint;
        }

        [GameMember(typeof(PlayerDeathHead), "inTruck", typeof(bool), FeatureId.DeadHeadShrink)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool IsHeadInTruck(PlayerDeathHead head)
        {
            return head.inTruck;
        }

        // ---- 玩家缩放（PlayerScaling）----

        /// <summary>车内检测体积（与原版 ObjectsInCart 的 OverlapBox 使用同一个 Transform）。</summary>
        [GameMember(typeof(PhysGrabCart), "inCart", typeof(Transform), FeatureId.PlayerScaling)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Transform InCartVolume(PhysGrabCart cart)
        {
            return cart.inCart;
        }

        [GameMember(typeof(PlayerAvatar), "playerCollider", typeof(Collider), FeatureId.PlayerScaling)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Collider AvatarCollider(PlayerAvatar avatar)
        {
            return avatar.playerCollider;
        }
    }
}
