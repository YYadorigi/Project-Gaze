using ProjectGaze.Gaze.Providers;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectGaze.Gaze
{
    public sealed class GazeInteractionController : MonoBehaviour
    {
        private Camera targetCamera;
        private SpatialPageRegistry pageRegistry;
        private GazeRayProjector gazeRayProjector;
        private GazeInteractionStateMachine stateMachine;
        private GazeInteractionSettings settings;
        private IGazeTrackingProvider eyeTrackingGazeProvider;
        private IBlinkDetectionProvider eyeTrackingBlinkProvider;
        private PageSelectionMode selectionMode = PageSelectionMode.StereoGaze;

        private string candidatePageId;
        private string stablePageId;
        private float candidateDuration;
        private float lostDuration;
        private string activeGazeProviderName = "None";
        private string activeBlinkProviderName = "None";
        private bool hasLastTrackingSample;
        private GazeTrackingSample lastTrackingSample;
        private GazeHitResult lastHitResult;
        private GazeInteractionSnapshot currentSnapshot = new(GazeInteractionMode.Idle, null, null);

        public event Action<GazeInteractionSnapshot> SnapshotChanged;

        public event Action<string> PreviewTargetChanged;

        public event Action<string> ConfirmedTargetChanged;

        public GazeInteractionSnapshot CurrentSnapshot => currentSnapshot;

        public string ActiveGazeProviderName => activeGazeProviderName;

        public string ActiveBlinkProviderName => activeBlinkProviderName;

        public GazeDepthMatchingMode DepthMatchingMode =>
            gazeRayProjector != null
                ? gazeRayProjector.DepthMatchingMode
                : GazeDepthMatchingMode.DiscreteDepthLayer;

        public bool HasLastTrackingSample => hasLastTrackingSample;

        public GazeTrackingSample LastTrackingSample => lastTrackingSample;

        public GazeHitResult LastHitResult => lastHitResult;

        public void Initialize(
            Camera targetCamera,
            SpatialPageRegistry pageRegistry,
            GazeRayProjector gazeRayProjector,
            GazeInteractionSettings settings,
            IGazeTrackingProvider eyeTrackingGazeProvider,
            IBlinkDetectionProvider eyeTrackingBlinkProvider,
            PageSelectionMode selectionMode,
            string initialConfirmedPageId = null)
        {
            this.targetCamera = targetCamera;
            this.pageRegistry = pageRegistry;
            this.gazeRayProjector = gazeRayProjector;
            this.settings = settings;
            this.eyeTrackingGazeProvider = eyeTrackingGazeProvider;
            this.eyeTrackingBlinkProvider = eyeTrackingBlinkProvider;
            this.selectionMode = selectionMode;

            settings.Clamp();
            stateMachine = new GazeInteractionStateMachine(settings);
            stateMachine.SeedConfirmedPage(initialConfirmedPageId);
            currentSnapshot = stateMachine.CurrentSnapshot;
        }

        public void SetDepthMatchingMode(GazeDepthMatchingMode depthMatchingMode)
        {
            if (gazeRayProjector == null || gazeRayProjector.DepthMatchingMode == depthMatchingMode)
            {
                return;
            }

            gazeRayProjector.SetDepthMatchingMode(depthMatchingMode);
        }

        private void Update()
        {
            if (targetCamera == null || pageRegistry == null || stateMachine == null)
            {
                return;
            }

            GazeTrackingSample sample = default;
            bool trackingAvailable = eyeTrackingGazeProvider != null && eyeTrackingGazeProvider.IsAvailable;
            activeGazeProviderName = trackingAvailable ? eyeTrackingGazeProvider.ProviderName : "None";
            activeBlinkProviderName = eyeTrackingBlinkProvider != null && eyeTrackingBlinkProvider.IsAvailable
                ? eyeTrackingBlinkProvider.ProviderName
                : "None";

            bool hasSample = trackingAvailable && eyeTrackingGazeProvider.TryGetSample(out sample);
            GazeHitResult hitResult = default;
            string immediateHitPageId = null;

            if (hasSample)
            {
                hitResult = selectionMode == PageSelectionMode.MouseFallback
                    ? gazeRayProjector.ProjectFromMouse(targetCamera, sample, ReadMouseScrollDelta())
                    : gazeRayProjector.ProjectFromGaze(targetCamera, sample);
                immediateHitPageId = hitResult.HasHitPage ? hitResult.TargetId : null;
                hasLastTrackingSample = true;
                lastTrackingSample = sample;
                lastHitResult = hitResult;
            }
            else
            {
                hasLastTrackingSample = false;
                lastTrackingSample = default;
                lastHitResult = default;
            }

            UpdateStablePage(immediateHitPageId, hasSample);

            bool blinkConfirmed = eyeTrackingBlinkProvider != null &&
                                  eyeTrackingBlinkProvider.IsAvailable &&
                                  eyeTrackingBlinkProvider.ConsumeBlinkConfirmation();
            var input = new GazeInteractionInput(hasSample, immediateHitPageId, stablePageId, blinkConfirmed, Time.deltaTime);
            var previousSnapshot = currentSnapshot;
            currentSnapshot = stateMachine.Tick(input);
            NotifySnapshotChanges(previousSnapshot, currentSnapshot);
        }

        private void UpdateStablePage(string immediateHitPageId, bool trackingAvailable)
        {
            if (trackingAvailable && !string.IsNullOrEmpty(immediateHitPageId))
            {
                lostDuration = 0f;

                if (candidatePageId == immediateHitPageId)
                {
                    candidateDuration += Time.deltaTime;
                }
                else
                {
                    candidatePageId = immediateHitPageId;
                    candidateDuration = Time.deltaTime;
                }

                if (candidateDuration >= settings.PreviewDwellSeconds)
                {
                    stablePageId = candidatePageId;
                }

                return;
            }

            candidatePageId = null;
            candidateDuration = 0f;
            lostDuration += Time.deltaTime;

            if (lostDuration >= settings.LeaveHysteresisSeconds)
            {
                stablePageId = null;
            }
        }

        private static float ReadMouseScrollDelta()
        {
            return Mouse.current?.scroll.ReadValue().y ?? 0f;
        }

        private void NotifySnapshotChanges(
            in GazeInteractionSnapshot previousSnapshot,
            in GazeInteractionSnapshot nextSnapshot)
        {
            if (SnapshotsEqual(previousSnapshot, nextSnapshot))
            {
                return;
            }

            SnapshotChanged?.Invoke(nextSnapshot);

            if (!string.Equals(previousSnapshot.PreviewPageId, nextSnapshot.PreviewPageId, StringComparison.Ordinal))
            {
                PreviewTargetChanged?.Invoke(nextSnapshot.PreviewPageId);
            }

            if (!string.Equals(previousSnapshot.ConfirmedPageId, nextSnapshot.ConfirmedPageId, StringComparison.Ordinal))
            {
                ConfirmedTargetChanged?.Invoke(nextSnapshot.ConfirmedPageId);
            }
        }

        private static bool SnapshotsEqual(
            in GazeInteractionSnapshot left,
            in GazeInteractionSnapshot right)
        {
            return left.Mode == right.Mode &&
                   string.Equals(left.PreviewPageId, right.PreviewPageId, StringComparison.Ordinal) &&
                   string.Equals(left.ConfirmedPageId, right.ConfirmedPageId, StringComparison.Ordinal);
        }
    }
}
