using UnityEngine;

namespace ProjectGaze.Gaze.Providers
{
    public sealed class InvensunA8BlinkDetectionProvider : MonoBehaviour, IBlinkDetectionProvider
    {
        [SerializeField]
        private float minBlinkDurationSeconds = 0.04f;

        [SerializeField]
        private float maxBlinkDurationSeconds = 0.40f;

        [SerializeField]
        private float confirmCooldownSeconds = 0.40f;

        [SerializeField]
        private float opennessThreshold = 0.35f;

        private InvensunA8DeviceRuntime deviceRuntime;
        private BlinkConfirmationRule blinkRule;

        public string ProviderName => "7Invensun Blink";

        public bool IsAvailable => deviceRuntime != null && deviceRuntime.IsConnected;

        public void Initialize()
        {
            deviceRuntime = GetComponent<InvensunA8DeviceRuntime>() ?? gameObject.AddComponent<InvensunA8DeviceRuntime>();
            deviceRuntime.Initialize();
            blinkRule ??= new BlinkConfirmationRule(minBlinkDurationSeconds, maxBlinkDurationSeconds, confirmCooldownSeconds);
        }

        public bool ConsumeBlinkConfirmation()
        {
            return blinkRule != null && blinkRule.ConsumeConfirmation();
        }

        private void Update()
        {
            blinkRule ??= new BlinkConfirmationRule(minBlinkDurationSeconds, maxBlinkDurationSeconds, confirmCooldownSeconds);

            if (!IsAvailable)
            {
                blinkRule.Reset();
                return;
            }

            if (deviceRuntime == null || !deviceRuntime.TryGetLatestSample(out var data))
            {
                return;
            }

            bool leftOpen = InvensunA8BlinkSignalUtility.IsEyeOpen(
                data.LeftBlinkDetected,
                data.LeftOpenness,
                opennessThreshold);
            bool rightOpen = InvensunA8BlinkSignalUtility.IsEyeOpen(
                data.RightBlinkDetected,
                data.RightOpenness,
                opennessThreshold);
            bool blinkActive = data.LeftBlinkDetected ||
                               data.RightBlinkDetected ||
                               (!leftOpen && !rightOpen);
            blinkRule.UpdateBlinkState(blinkActive, Time.unscaledDeltaTime);
        }
    }
}
