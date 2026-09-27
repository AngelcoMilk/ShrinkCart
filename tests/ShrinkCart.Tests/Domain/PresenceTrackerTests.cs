using System.Collections.Generic;
using NUnit.Framework;
using ShrinkCart.Domain;

namespace ShrinkCart.Tests.Domain
{
    public sealed class PresenceTrackerTests
    {
        private const int CartA = 100;
        private const int CartB = 200;
        private const int Item = 1;
        private const float Debounce = 0.5f;

        private readonly List<int> _due = new List<int>();

        [Test]
        public void SeenInScan_IsNotAbsent()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);

            Scan(tracker, CartA, 1f, Item);

            Assert.That(tracker.IsAbsent(Item), Is.False);
            tracker.CollectDue(10f, Debounce, _due);
            Assert.That(_due, Is.Empty);
        }

        [Test]
        public void MissingFromScan_BecomesDueAfterDebounce()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);

            Scan(tracker, CartA, 1f);

            Assert.That(tracker.IsAbsent(Item), Is.True);
            tracker.CollectDue(1.4f, Debounce, _due);
            Assert.That(_due, Is.Empty, "still within debounce");
            tracker.CollectDue(1.5f, Debounce, _due);
            Assert.That(_due, Is.EquivalentTo(new[] { Item }));
        }

        [Test]
        public void NoScan_KeepsItemFrozen()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);

            // 原版在 12 m 内没有玩家时不扫描：无论过多久都不应判定离车。
            tracker.CollectDue(1000f, Debounce, _due);

            Assert.That(_due, Is.Empty);
            Assert.That(tracker.IsAbsent(Item), Is.False);
        }

        [Test]
        public void TriggerPresence_ClearsAbsence()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);
            Scan(tracker, CartA, 1f);

            Assert.That(tracker.TryMarkSeen(Item, CartA, 1.2f), Is.True);

            Assert.That(tracker.IsAbsent(Item), Is.False);
            tracker.CollectDue(5f, Debounce, _due);
            Assert.That(_due, Is.Empty);
        }

        [Test]
        public void MovedToAnotherCart_OldCartScanDoesNotMarkAbsent()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);

            Scan(tracker, CartB, 1f, Item);
            Scan(tracker, CartA, 1.1f);

            Assert.That(tracker.IsAbsent(Item), Is.False);
        }

        [Test]
        public void CartRemoved_StartsCountdown()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);

            tracker.OnCartRemoved(CartA, 2f);

            tracker.CollectDue(2.5f, Debounce, _due);
            Assert.That(_due, Is.EquivalentTo(new[] { Item }));
        }

        [Test]
        public void TrackedDuringScan_CountsAsSeenInThatScan()
        {
            PresenceTracker tracker = new PresenceTracker();

            tracker.BeginScan(CartA);
            tracker.Track(Item, CartA, 1f);
            tracker.EndScan(CartA, 1f);

            Assert.That(tracker.IsAbsent(Item), Is.False);
        }

        [Test]
        public void TriggerBetweenScans_DoesNotCountForNextScan()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);
            tracker.TryMarkSeen(Item, CartA, 0.5f);

            Scan(tracker, CartA, 1f);

            Assert.That(tracker.IsAbsent(Item), Is.True, "only presence inside the scan counts for that scan");
        }

        [Test]
        public void TryMarkSeen_UnknownItem_ReturnsFalse()
        {
            PresenceTracker tracker = new PresenceTracker();

            Assert.That(tracker.TryMarkSeen(Item, CartA, 0f), Is.False);
        }

        [Test]
        public void HasDue_FalseWhileNothingIsAbsent()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);
            Scan(tracker, CartA, 1f, Item);

            Assert.That(tracker.HasDue(100f, Debounce), Is.False);
        }

        [Test]
        public void HasDue_TrueExactlyWhenDebounceElapses()
        {
            // 与 0.2.x 相同的手感：最后一次确认在车内（扫描时刻）+ 防抖时间，到点即恢复，不等维护间隔。
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);
            Scan(tracker, CartA, 1f);

            Assert.That(tracker.HasDue(1.25f, Debounce), Is.False);
            Assert.That(tracker.HasDue(1.5f, Debounce), Is.True);
        }

        [Test]
        public void HasDue_StaleAfterSeenAgain_CollectsNothingAndClears()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);
            Scan(tracker, CartA, 1f);
            tracker.TryMarkSeen(Item, CartA, 1.25f);

            Assert.That(tracker.HasDue(1.5f, Debounce), Is.True, "may be stale (early), never late");
            tracker.CollectDue(1.5f, Debounce, _due);
            Assert.That(_due, Is.Empty);
            Assert.That(tracker.HasDue(1.75f, Debounce), Is.False, "recomputed after collection");
        }

        [Test]
        public void HasDue_FollowsNextAbsentItemAfterDueItemRemoved()
        {
            const int Other = 2;
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);
            tracker.Track(Other, CartB, 0f);
            Scan(tracker, CartA, 1f);
            Scan(tracker, CartB, 1.25f);

            tracker.CollectDue(1.5f, Debounce, _due);
            Assert.That(_due, Is.EquivalentTo(new[] { Item }));
            tracker.Remove(Item);

            tracker.CollectDue(1.5f, Debounce, _due);
            Assert.That(_due, Is.Empty);
            Assert.That(tracker.HasDue(1.625f, Debounce), Is.False);
            Assert.That(tracker.HasDue(1.75f, Debounce), Is.True);
            tracker.CollectDue(1.75f, Debounce, _due);
            Assert.That(_due, Is.EquivalentTo(new[] { Other }));
        }

        [Test]
        public void CartRemoved_MakesHasDueTrueAfterDebounce()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);

            tracker.OnCartRemoved(CartA, 2f);

            Assert.That(tracker.HasDue(2.25f, Debounce), Is.False);
            Assert.That(tracker.HasDue(2.5f, Debounce), Is.True);
        }

        [Test]
        public void Clear_ResetsHasDue()
        {
            PresenceTracker tracker = new PresenceTracker();
            tracker.Track(Item, CartA, 0f);
            Scan(tracker, CartA, 1f);

            tracker.Clear();

            Assert.That(tracker.HasDue(10f, Debounce), Is.False);
        }

        private static void Scan(PresenceTracker tracker, int cartId, float now, params int[] seen)
        {
            tracker.BeginScan(cartId);
            for (int i = 0; i < seen.Length; i++)
            {
                tracker.TryMarkSeen(seen[i], cartId, now);
            }

            tracker.EndScan(cartId, now);
        }
    }
}
