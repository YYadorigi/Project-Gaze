using UnityEngine;

namespace ProjectGaze.Gaze
{
    public sealed class BlinkConfirmationRule
    {
        private readonly float minBlinkDurationSeconds;
        private readonly float maxBlinkDurationSeconds;
        private readonly float confirmCooldownSeconds;

        private bool eyesClosed;
        private float closedDuration;
        private float cooldownRemaining;
        private bool blinkQueued;

        public BlinkConfirmationRule(
            float minBlinkDurationSeconds,
            float maxBlinkDurationSeconds,
            float confirmCooldownSeconds)
        {
            this.minBlinkDurationSeconds = Mathf.Max(0.01f, minBlinkDurationSeconds);
            this.maxBlinkDurationSeconds = Mathf.Max(this.minBlinkDurationSeconds, maxBlinkDurationSeconds);
            this.confirmCooldownSeconds = Mathf.Max(0f, confirmCooldownSeconds);
        }

        public void Reset()
        {
            eyesClosed = false;
            closedDuration = 0f;
            cooldownRemaining = 0f;
            blinkQueued = false;
        }

        public void Update(bool leftEyeUsable, bool rightEyeUsable, float deltaTime)
        {
            UpdateBlinkState(!leftEyeUsable && !rightEyeUsable, deltaTime);
        }

        public void UpdateBlinkState(bool blinkActive, float deltaTime)
        {
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);

            if (blinkActive)
            {
                closedDuration += deltaTime;
                eyesClosed = true;
                return;
            }

            if (eyesClosed &&
                closedDuration >= minBlinkDurationSeconds &&
                closedDuration <= maxBlinkDurationSeconds &&
                cooldownRemaining <= 0f)
            {
                blinkQueued = true;
                cooldownRemaining = confirmCooldownSeconds;
            }

            closedDuration = 0f;
            eyesClosed = false;
        }

        public bool ConsumeConfirmation()
        {
            if (!blinkQueued)
            {
                return false;
            }

            blinkQueued = false;
            return true;
        }
    }
}
