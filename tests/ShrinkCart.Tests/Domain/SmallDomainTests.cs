using NUnit.Framework;
using ShrinkCart.Domain;

namespace ShrinkCart.Tests.Domain
{
    public sealed class ScanCadenceTests
    {
        [TestCase(0, 1.0f)]
        [TestCase(1, 0.15f)]
        [TestCase(6, 0.15f)]
        [TestCase(7, 0.35f)]
        [TestCase(15, 0.35f)]
        [TestCase(16, 0.75f)]
        [TestCase(30, 0.75f)]
        [TestCase(31, 1.0f)]
        public void DynamicTiers(int tracked, float expected)
        {
            Assert.That(ScanCadence.Interval(true, tracked, 0.15f, 1.0f), Is.EqualTo(expected).Within(1e-6f));
        }

        [Test]
        public void DisabledUsesMinimum()
        {
            Assert.That(ScanCadence.Interval(false, 50, 0.15f, 1.0f), Is.EqualTo(0.15f).Within(1e-6f));
        }

        [Test]
        public void TiersAreClampedToConfiguredRange()
        {
            Assert.That(ScanCadence.Interval(true, 10, 0.5f, 2.0f), Is.EqualTo(0.5f).Within(1e-6f), "0.35 raised to minimum");
            Assert.That(ScanCadence.Interval(true, 20, 0.1f, 0.5f), Is.EqualTo(0.5f).Within(1e-6f), "0.75 lowered to maximum");
        }

        [Test]
        public void MaximumBelowMinimumIsRaised()
        {
            Assert.That(ScanCadence.Interval(true, 100, 0.8f, 0.2f), Is.EqualTo(0.8f).Within(1e-6f));
        }
    }

    public sealed class RetryCooldownTests
    {
        [Test]
        public void CoolsDownThenExpires()
        {
            RetryCooldown cooldown = new RetryCooldown();
            cooldown.Start(7, 10f, 1f);

            Assert.That(cooldown.IsCooling(7, 10.5f), Is.True);
            Assert.That(cooldown.IsCooling(7, 11f), Is.False);
            Assert.That(cooldown.Count, Is.EqualTo(0), "expired entry removed on query");
        }

        [Test]
        public void CancelAndPrune()
        {
            RetryCooldown cooldown = new RetryCooldown();
            cooldown.Start(1, 0f, 1f);
            cooldown.Start(2, 0f, 5f);
            cooldown.Cancel(1);
            cooldown.Prune(6f);

            Assert.That(cooldown.Count, Is.EqualTo(0));
        }
    }

    public sealed class ObjectNamesTests
    {
        [TestCase("Valuable Enemy - Small(Clone)", "Enemy - Small")]
        [TestCase("  Item Cart Cannon(Clone) ", "Item Cart Cannon")]
        [TestCase(null, "")]
        public void Clean(string input, string expected)
        {
            Assert.That(ObjectNames.Clean(input), Is.EqualTo(expected));
        }

        [TestCase("Enemy - Small", "EnemyOrbSmall")]
        [TestCase("enemy - medium", "EnemyOrbMedium")]
        [TestCase("Enemy - Big", "EnemyOrbBig")]
        [TestCase("Enemy - Berserker", "EnemyOrbBerserker")]
        public void ParsesEnemyOrbs(string cleanName, string expectedCategory)
        {
            Assert.That(ObjectNames.TryParseEnemyOrb(cleanName, out ShrinkCategory category), Is.True);
            Assert.That(category.ToString(), Is.EqualTo(expectedCategory));
        }

        [TestCase("Enemy - Ceiling Eye")]
        [TestCase("Enemy")]
        [TestCase("Vase - Small")]
        [TestCase("")]
        public void RejectsNonOrbs(string cleanName)
        {
            Assert.That(ObjectNames.TryParseEnemyOrb(cleanName, out _), Is.False);
        }
    }

    public sealed class MathUtilTests
    {
        [TestCase(0.3999999f, 0.4f)]
        [TestCase(0.35000002f, 0.35f)]
        [TestCase(5f, 5f)]
        [TestCase(0.125f, 0.12f)]
        public void RoundTo2Decimals_MatchesMathfRoundSemantics(float input, float expected)
        {
            Assert.That(MathUtil.RoundTo2Decimals(input), Is.EqualTo(expected));
        }

        [Test]
        public void Approximately()
        {
            Assert.That(MathUtil.Approximately(0.55f, 0.55f), Is.True);
            Assert.That(MathUtil.Approximately(0.55f, 0.56f), Is.False);
        }
    }
}
