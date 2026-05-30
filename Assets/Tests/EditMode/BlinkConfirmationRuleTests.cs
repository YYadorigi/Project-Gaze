using NUnit.Framework;
using ProjectGaze.Gaze;

namespace ProjectGaze.Tests
{
    public sealed class BlinkConfirmationRuleTests
    {
        [Test]
        public void TooShortClosure_DoesNotQueueBlink()
        {
            var rule = new BlinkConfirmationRule(0.10f, 0.26f, 0.40f);

            rule.Update(false, false, 0.05f);
            rule.Update(true, true, 0.01f);

            Assert.That(rule.ConsumeConfirmation(), Is.False);
        }

        [Test]
        public void ClosureWithinWindow_QueuesBlinkOnce()
        {
            var rule = new BlinkConfirmationRule(0.10f, 0.26f, 0.40f);

            rule.Update(false, false, 0.12f);
            rule.Update(true, true, 0.01f);

            Assert.That(rule.ConsumeConfirmation(), Is.True);
            Assert.That(rule.ConsumeConfirmation(), Is.False);
        }

        [Test]
        public void Cooldown_PreventsImmediateSecondBlink()
        {
            var rule = new BlinkConfirmationRule(0.10f, 0.26f, 0.40f);

            rule.Update(false, false, 0.12f);
            rule.Update(true, true, 0.01f);
            Assert.That(rule.ConsumeConfirmation(), Is.True);

            rule.Update(false, false, 0.12f);
            rule.Update(true, true, 0.01f);
            Assert.That(rule.ConsumeConfirmation(), Is.False);

            rule.Update(true, true, 0.50f);
            rule.Update(false, false, 0.12f);
            rule.Update(true, true, 0.01f);
            Assert.That(rule.ConsumeConfirmation(), Is.True);
        }

        [Test]
        public void BlinkStateWithinWindow_QueuesBlinkWithoutRequiringPerEyeOpenState()
        {
            var rule = new BlinkConfirmationRule(0.10f, 0.26f, 0.40f);

            rule.UpdateBlinkState(true, 0.12f);
            rule.UpdateBlinkState(false, 0.01f);

            Assert.That(rule.ConsumeConfirmation(), Is.True);
        }
    }
}
