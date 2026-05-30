using System;
using UnityEngine;

namespace ProjectGaze.Gaze
{
    [Serializable]
    public sealed class GazeInteractionSettings
    {
        [Header("Temporal")]
        public float PreviewDwellSeconds = 0.12f;
        public float LeaveHysteresisSeconds = 0.08f;
        public float SwitchCooldownSeconds = 0.10f;
        public float BlinkConfirmCooldownSeconds = 0.40f;

        [Header("Blink")]
        public float BlinkMinDurationSeconds = 0.10f;
        public float BlinkMaxDurationSeconds = 0.26f;

        [Header("Visual")]
        public float DormantAlpha = 0.26f;
        public float PreviewAlpha = 0.78f;
        public float ConfirmedAlpha = 0.94f;
        public float SuppressedAlpha = 0.18f;
        public float ContentDormantAlpha = 0.42f;
        public float ContentPreviewAlpha = 0.76f;
        public float ContentConfirmedAlpha = 1.00f;
        public float ContentSuppressedAlpha = 0.26f;
        public float PageFadeSpeed = 3.5f;
        public float DormantBrightness = 0.88f;
        public float PreviewBrightness = 0.94f;
        public float ConfirmedBrightness = 1.00f;
        public float SuppressedBrightness = 0.74f;

        public void Clamp()
        {
            PreviewDwellSeconds = Mathf.Max(0.01f, PreviewDwellSeconds);
            LeaveHysteresisSeconds = Mathf.Max(0.01f, LeaveHysteresisSeconds);
            SwitchCooldownSeconds = Mathf.Max(0.0f, SwitchCooldownSeconds);
            BlinkConfirmCooldownSeconds = Mathf.Max(0.0f, BlinkConfirmCooldownSeconds);
            BlinkMinDurationSeconds = Mathf.Max(0.01f, BlinkMinDurationSeconds);
            BlinkMaxDurationSeconds = Mathf.Max(BlinkMinDurationSeconds, BlinkMaxDurationSeconds);
            DormantAlpha = Mathf.Clamp01(DormantAlpha);
            PreviewAlpha = Mathf.Clamp01(PreviewAlpha);
            ConfirmedAlpha = Mathf.Clamp01(ConfirmedAlpha);
            SuppressedAlpha = Mathf.Clamp01(SuppressedAlpha);
            ContentDormantAlpha = Mathf.Clamp01(ContentDormantAlpha);
            ContentPreviewAlpha = Mathf.Clamp01(ContentPreviewAlpha);
            ContentConfirmedAlpha = Mathf.Clamp01(ContentConfirmedAlpha);
            ContentSuppressedAlpha = Mathf.Clamp01(ContentSuppressedAlpha);
            PageFadeSpeed = Mathf.Max(0.1f, PageFadeSpeed);
        }
    }
}
