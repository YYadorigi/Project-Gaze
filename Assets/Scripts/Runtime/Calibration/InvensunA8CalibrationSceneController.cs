using System;
using System.Collections;
using System.Runtime.InteropServices;
using ProjectGaze.Gaze;
using ProjectGaze.Gaze.Depth;
using ProjectGaze.Gaze.Providers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectGaze.Calibration
{
    public sealed class InvensunA8CalibrationSceneController : MonoBehaviour
    {
        private const float RecommendedScoreThreshold = 98.0f;
        private const float PointTimeoutSeconds = 10.0f;
        private const float BetweenPointsDelaySeconds = 0.50f;
        private const float CalibrationMoveSpeed = 10.0f;
        private const float CalibrationScaleDurationSeconds = 0.25f;
        private const float CalibrationPostScaleDelaySeconds = 0.25f;
        private const float CalibrationMarkerMoveStopEpsilon = 0.01f;
        private const float CalibrationMarkerBaseScale = 0.30f;
        private const float CalibrationMarkerTargetScale = 0.65f;
        private const float CalibrationMarkerSize = 100.0f;
        private const float DepthCalibrationMarkerWorldScale = 0.55f;
        private const float DepthCalibrationSampleWindowSeconds = 0.60f;
        private const int DepthCalibrationMinimumSamples = 10;
        private const float DepthCalibrationMaxViewportError = 0.16f;
        private const float CompletionStatusSeconds = 1.0f;
        private const float PostureCenteredMin = 0.30f;
        private const float PostureCenteredMax = 0.70f;
        private const float PostureHoldSeconds = 1.5f;
        private const float PostureWindowSize = 460.0f;
        private const float PostureCatSize = 156.0f;
        private const float PostureProgressBarWidth = 320.0f;
        private const float PostureProgressBarHeight = 18.0f;

        private static readonly InvensunA8Native.ImageCallback ImageCallbackDelegate = OnImageCallback;
        private static readonly InvensunA8Native.GazeCallback GazeCallbackDelegate = OnGazeCallback;
        private static readonly InvensunA8Native.CalibrationProcessCallback CalibrationProcessCallbackDelegate = OnCalibrationProcessCallback;
        private static readonly InvensunA8Native.CalibrationFinishCallback CalibrationFinishCallbackDelegate = OnCalibrationFinishCallback;

        private readonly IInvensunA8CalibrationApi calibrationApi = new InvensunA8CalibrationApi();
        private readonly ICalibrationArtifactService artifactService = new CalibrationArtifactService();
        private readonly ScreenCalibrationPointRunner screenCalibrationPointRunner = new();
        private readonly object callbackLock = new();

        private GCHandle callbackHandle;
        private Coroutine calibrationCoroutine;
        private Coroutine continueCoroutine;
        private CalibrationSceneState state = CalibrationSceneState.Checking;
        private string statusMessage = "Checking 7Invensun calibration state...";
        private int currentPointIndex = -1;
        private int currentPointProgress;
        private bool currentPointFinished;
        private bool currentPointFailed;
        private int currentPointError;
        private bool runtimeStarted;
        private bool trackingStarted;
        private float leftScore;
        private float rightScore;
        private byte[] completedCalibrationCoefficientBuffer;
        private float postureHoldProgressSeconds;
        private bool hasPupilCenters;
        private bool headCentered;
        private Vector2 averagePupilCenter;
        private float eyeAngleDegrees;
        private bool hasLatestBinocularGazeSample;
        private BinocularGazeSample latestBinocularGazeSample;
        private Vector2 calibrationMarkerAnchoredPosition = Vector2.zero;
        private int currentDepthPointIndex = -1;
        private int depthProgressTargetCount;
        private int depthCalibrationRecordCount;
        private bool depthCalibrationSucceeded;
        private GazeDepthCalibrationDataset depthCalibrationDataset;
        private GazeDepthModelBundle depthCalibrationModel;
        private GazeDepthContinuousValidationSession depthValidationSession;
        private InvensunA8CalibrationMetadata lastCalibrationMetadata;
        private Camera mainCamera;
        private CalibrationDisplayModeController displayModeController;
        private Canvas calibrationCanvas;
        private RawImage calibrationMarkerImage;
        private RectTransform calibrationMarkerRect;
        private Transform depthCalibrationMarkerTransform;
        private SpriteRenderer depthCalibrationMarkerRenderer;
        private Sprite depthCalibrationMarkerSprite;
        private Texture2D panelTexture;
        private Texture2D solidTexture;
        private Texture2D calibrationPointTexture;
        private Texture2D postureWindowTexture;
        private Texture2D postureCatTexture;
        private Texture2D postureCatAlertTexture;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle warningStyle;
        private GUIStyle panelStyle;

        private void Start()
        {
            EnsureMainCameraConfigured();
            calibrationCoroutine = StartCoroutine(RunCalibrationFlow());
        }

        private void Update()
        {
            var keyboard = Keyboard.current;

            if (state == CalibrationSceneState.Review)
            {
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    RestartCalibration();
                    return;
                }

                if (keyboard != null &&
                    keyboard.sKey.wasPressedThisFrame &&
                    CalibrationSceneFlow.HasPendingRequestedScene())
                {
                    SkipCalibrationAndContinueWithMouseFallback();
                    return;
                }

                return;
            }

            if (state == CalibrationSceneState.Error && keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    RestartCalibration();
                    return;
                }

                if (keyboard.sKey.wasPressedThisFrame &&
                    CalibrationSceneFlow.HasPendingRequestedScene())
                {
                    SkipCalibrationAndContinueWithMouseFallback();
                }
            }
        }

        private void EnsureMainCameraConfigured()
        {
            mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();

            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            displayModeController = new CalibrationDisplayModeController(mainCamera);
            displayModeController.ConfigureMonoForScreenCalibration();
        }

        private void OnGUI()
        {
            if (state == CalibrationSceneState.Launching)
            {
                return;
            }

            EnsureGuiResources();

            const float panelWidth = 680f;
            float panelHeight = state == CalibrationSceneState.Error || state == CalibrationSceneState.DepthCalibrating ? 246f : 196f;
            const float margin = 24f;
            float panelX = margin;
            float panelY = ResolveInfoPanelY(margin);
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none, panelStyle);

            float contentX = panelX + 20f;
            float cursorY = panelY + 16f;
            float contentWidth = panelWidth - 40f;

            GUI.Label(new Rect(contentX, cursorY, contentWidth, 30f), "7Invensun Calibration", titleStyle);
            cursorY += 38f;

            string instructions;
            switch (state)
            {
                case CalibrationSceneState.AligningPosture:
                    instructions = "First align your sitting posture. Move until the cat stays inside the frame, then hold steady before calibration starts.";
                    break;

                case CalibrationSceneState.Calibrating:
                    instructions = "Keep your head stable and follow the target point with your eyes. The current point will advance after the SDK reports completion.";
                    break;

                case CalibrationSceneState.DepthCalibrating:
                    instructions = "Depth calibration is collecting binocular samples from 3D targets. Keep your gaze fixed on the marker until it moves to the next depth position.";
                    break;

                case CalibrationSceneState.Completed:
                    instructions = CalibrationSceneFlow.HasPendingRequestedScene()
                        ? "Calibration completed. The requested scene will load automatically after this score summary."
                        : "Calibration completed. This standalone calibration scene will remain on the score page. Press R to recalibrate.";
                    break;

                default:
                    instructions = "Before entering the formal gaze demo, calibration should be completed. Scores around 95+ are typically much more usable.";
                    break;
            }
            GUI.Label(new Rect(contentX, cursorY, contentWidth, 48f), instructions, bodyStyle);
            cursorY += 56f;

            float statusHeight = state == CalibrationSceneState.DepthCalibrating ? 82f : 48f;
            GUI.Label(new Rect(contentX, cursorY, contentWidth, statusHeight), statusMessage, bodyStyle);
            cursorY += statusHeight + 4f;

            if (state == CalibrationSceneState.AligningPosture)
            {
                DrawPostureAlignmentVisual();
                return;
            }

            if (state == CalibrationSceneState.Calibrating)
            {
                string progressText = currentPointIndex >= 0
                    ? $"Point {currentPointIndex + 1}/{InvensunA8CalibrationUtility.CalibrationPointCount}    SDK Progress: {currentPointProgress}%"
                    : "Preparing calibration points...";
                GUI.Label(new Rect(contentX, cursorY, contentWidth, 28f), progressText, bodyStyle);
                return;
            }

            if (state == CalibrationSceneState.DepthCalibrating)
            {
                int targetCount = depthProgressTargetCount > 0
                    ? depthProgressTargetCount
                    : GazeDepthCalibrationTargetLibrary.TargetCount;
                string progressText = currentDepthPointIndex >= 0
                    ? $"Depth Target {currentDepthPointIndex + 1}/{targetCount}    Captured Records: {depthCalibrationRecordCount}"
                    : "Preparing depth calibration targets...";
                GUI.Label(new Rect(contentX, cursorY, contentWidth, 28f), progressText, bodyStyle);
                return;
            }

            if (state == CalibrationSceneState.Review)
            {
                string reviewText = $"Calibration score below the recommended threshold {RecommendedScoreThreshold:F0}. Left={leftScore:F1}, Right={rightScore:F1}.";
                GUI.Label(new Rect(contentX, cursorY, contentWidth, 32f), reviewText, warningStyle);
                cursorY += 38f;
                string reviewActionText = CalibrationSceneFlow.HasPendingRequestedScene()
                    ? "Calibration score is not accepted. Press R to recalibrate from the xy stage, or press S to continue this scene with mouse fallback."
                    : "Calibration score is not accepted. Press R to recalibrate from the xy stage.";
                GUI.Label(new Rect(contentX, cursorY, contentWidth, 28f), reviewActionText, bodyStyle);
                return;
            }

            if (state == CalibrationSceneState.Completed)
            {
                string completedText = $"Calibration scores. Left={leftScore:F1}, Right={rightScore:F1}.";
                GUI.Label(new Rect(contentX, cursorY, contentWidth, 32f), completedText, bodyStyle);
                return;
            }

            if (state == CalibrationSceneState.Error)
            {
                string errorActionText = CalibrationSceneFlow.HasPendingRequestedScene()
                    ? "Press R to restart calibration, or press S to continue the requested scene with mouse fallback."
                    : "Press R to restart calibration.";
                GUI.Label(new Rect(contentX, cursorY, contentWidth, 64f), errorActionText, warningStyle);
            }
        }

        private float ResolveInfoPanelY(float defaultMargin)
        {
            if (state == CalibrationSceneState.Calibrating)
            {
                return ResolveYBetweenLeftCalibrationTargets(defaultMargin);
            }

            return state == CalibrationSceneState.DepthCalibrating ? 140f : defaultMargin;
        }

        private static float ResolveYBetweenLeftCalibrationTargets(float fallbackY)
        {
            try
            {
                Vector2 topLeftTarget = InvensunA8CalibrationUtility.GetDemoStyleCalibrationAnchoredPosition(1);
                Vector2 leftMiddleTarget = InvensunA8CalibrationUtility.GetDemoStyleCalibrationAnchoredPosition(8);
                float scale = Screen.width / InvensunA8CalibrationUtility.VendorCalibrationReferenceResolution.x;
                float topLeftY = ResolveGuiY(topLeftTarget.y, scale);
                float leftMiddleY = ResolveGuiY(leftMiddleTarget.y, scale);
                return Mathf.Lerp(topLeftY, leftMiddleY, 0.5f);
            }
            catch
            {
                return fallbackY;
            }
        }

        private static float ResolveGuiY(float anchoredY, float scale)
        {
            return (Screen.height * 0.5f) - (anchoredY * scale);
        }

        private IEnumerator RunCalibrationFlow()
        {
            if (mainCamera == null || displayModeController == null)
            {
                EnsureMainCameraConfigured();
            }

            displayModeController?.ConfigureMonoForScreenCalibration();
            ShutdownRuntime();
            state = CalibrationSceneState.StartingRuntime;
            statusMessage = "Starting 7Invensun runtime for screen-space xy calibration in mono mode...";

            if (!TryStartRuntime())
            {
                yield break;
            }

            state = CalibrationSceneState.AligningPosture;
            postureHoldProgressSeconds = 0f;
            statusMessage = "Align your head position before calibration begins.";
            yield return WaitForStablePosture();

            if (state == CalibrationSceneState.Error)
            {
                yield break;
            }

            int startCalibrationRet = calibrationApi.StartCalibration(screenCalibrationPointRunner.PointCount);
            if (startCalibrationRet != 0)
            {
                FailCalibration($"7Invensun start calibration failed with code {startCalibrationRet}.");
                yield break;
            }

            state = CalibrationSceneState.Calibrating;
            EnsureCalibrationVisual();
            calibrationMarkerAnchoredPosition = screenCalibrationPointRunner.GetAnchoredPosition(0);
            SetCalibrationMarkerAnchoredPosition(calibrationMarkerAnchoredPosition);
            SetCalibrationMarkerVisible(true);

            for (int pointIndex = 0; pointIndex < screenCalibrationPointRunner.PointCount; pointIndex += 1)
            {
                currentPointIndex = pointIndex;
                currentPointProgress = 0;
                ResetPointCallbackState();
                statusMessage = $"Calibrating point {pointIndex + 1}/{screenCalibrationPointRunner.PointCount}...";
                Vector2 point = screenCalibrationPointRunner.GetAnchoredPosition(pointIndex);
                if (pointIndex > 0)
                {
                    yield return AnimateCalibrationMarkerMove(point);
                }

                yield return AnimateCalibrationMarkerScaleCue();

                Vector2 sdkPointValue = ResolveCalibrationMarkerSdkPoint();
                var sdkPoint = new InvensunA8Point2D
                {
                    X = sdkPointValue.x,
                    Y = sdkPointValue.y
                };

                int startPointRet = calibrationApi.StartCalibrationPoint(
                    pointIndex + 1,
                    ref sdkPoint,
                    Marshal.GetFunctionPointerForDelegate(CalibrationProcessCallbackDelegate),
                    GCHandle.ToIntPtr(callbackHandle),
                    Marshal.GetFunctionPointerForDelegate(CalibrationFinishCallbackDelegate),
                    GCHandle.ToIntPtr(callbackHandle));

                if (startPointRet != 0)
                {
                    FailCalibration($"7Invensun start calibration point {pointIndex + 1} failed with code {startPointRet}.");
                    yield break;
                }

                float elapsedSeconds = 0f;
                while (true)
                {
                    bool pointFinished;
                    bool pointFailed;
                    int pointError;

                    lock (callbackLock)
                    {
                        pointFinished = currentPointFinished;
                        pointFailed = currentPointFailed;
                        pointError = currentPointError;
                    }

                    if (pointFinished)
                    {
                        break;
                    }

                    if (pointFailed)
                    {
                        FailCalibration($"Calibration point {pointIndex + 1} failed with SDK error {pointError}.");
                        yield break;
                    }

                    if (elapsedSeconds >= PointTimeoutSeconds)
                    {
                        TryCancelCalibration();
                        FailCalibration($"Calibration point {pointIndex + 1} timed out after {PointTimeoutSeconds:F0} seconds.");
                        yield break;
                    }

                    elapsedSeconds += Time.unscaledDeltaTime;
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(BetweenPointsDelaySeconds);
            }

            SetCalibrationMarkerVisible(false);
            if (!TryCompleteScreenCalibration(out var coefficient, out bool meetsRecommendedThreshold))
            {
                yield break;
            }

            if (!meetsRecommendedThreshold)
            {
                EnterReviewForRejectedScreenCalibration();
                yield break;
            }

            if (!TryStartTracking(ref coefficient))
            {
                yield break;
            }

            ClearLatestBinocularGazeSample();

            if (displayModeController == null || !displayModeController.CanEnterStereoForDepthCalibration)
            {
                string stereoStatus = displayModeController != null
                    ? displayModeController.StereoStatusText
                    : "Calibration display mode controller is missing.";
                FailCalibration($"ThinkVision stereo display is required for z depth calibration. {stereoStatus}");
                yield break;
            }

            statusMessage = "Switching ThinkVision display to fixed 3D mode for z calibration...";
            yield return displayModeController.EnterStereoForDepthCalibration();
            if (!displayModeController.TryResolveCalibrationRayCamera(out _, out string rayCameraFailureReason))
            {
                FailCalibration($"ThinkVision stereo rig is not ready for z depth calibration. {rayCameraFailureReason}");
                yield break;
            }

            statusMessage = displayModeController.DepthCalibrationStereoStatus;
            ClearLatestBinocularGazeSample();

            depthCalibrationSucceeded = false;
            depthCalibrationDataset = null;
            depthCalibrationModel = null;
            depthValidationSession = null;
            yield return RunDepthCalibration();
            if (!depthCalibrationSucceeded)
            {
                yield break;
            }

            // Temporary thesis-data pass: keep this continuous depth validation until
            // the final error figure has been generated, then remove it as a unit.
            yield return RunDepthValidation();
            if (depthValidationSession == null)
            {
                yield break;
            }

            if (!PersistFinalCalibrationArtifacts(
                    meetsRecommendedThreshold,
                    depthCalibrationDataset,
                    depthCalibrationModel,
                    depthValidationSession))
            {
                yield break;
            }

            FinalizeCalibrationState(meetsRecommendedThreshold);
        }

        private IEnumerator WaitForStablePosture()
        {
            while (true)
            {
                bool postureAvailable;
                bool centered;
                Vector2 averagedCenter;

                lock (callbackLock)
                {
                    postureAvailable = hasPupilCenters;
                    centered = headCentered;
                    averagedCenter = averagePupilCenter;
                }

                if (!postureAvailable)
                {
                    postureHoldProgressSeconds = 0f;
                    statusMessage = "Waiting for both pupils to be detected...";
                    yield return null;
                    continue;
                }

                if (centered)
                {
                    postureHoldProgressSeconds += Time.unscaledDeltaTime;
                    statusMessage = $"Head aligned. Hold steady {Mathf.Clamp(PostureHoldSeconds - postureHoldProgressSeconds, 0f, PostureHoldSeconds):F1}s. Average pupil center=({averagedCenter.x:F2}, {averagedCenter.y:F2})";

                    if (postureHoldProgressSeconds >= PostureHoldSeconds)
                    {
                        statusMessage = "Posture alignment complete. Starting calibration...";
                        yield return new WaitForSecondsRealtime(0.2f);
                        yield break;
                    }
                }
                else
                {
                    postureHoldProgressSeconds = 0f;
                    statusMessage = $"Adjust your sitting posture until the cat enters the frame. Average pupil center=({averagedCenter.x:F2}, {averagedCenter.y:F2})";
                }

                yield return null;
            }
        }

        private IEnumerator AnimateCalibrationMarkerMove(Vector2 targetPoint)
        {
            while (Vector2.Distance(calibrationMarkerAnchoredPosition, targetPoint) > CalibrationMarkerMoveStopEpsilon)
            {
                calibrationMarkerAnchoredPosition = Vector2.Lerp(
                    calibrationMarkerAnchoredPosition,
                    targetPoint,
                    CalibrationMoveSpeed * Time.unscaledDeltaTime);
                SetCalibrationMarkerAnchoredPosition(calibrationMarkerAnchoredPosition);
                yield return null;
            }

            calibrationMarkerAnchoredPosition = targetPoint;
            SetCalibrationMarkerAnchoredPosition(calibrationMarkerAnchoredPosition);
        }

        private IEnumerator AnimateCalibrationMarkerScaleCue()
        {
            yield return AnimateCalibrationMarkerScale(CalibrationMarkerBaseScale, CalibrationMarkerTargetScale, CalibrationScaleDurationSeconds);
            yield return AnimateCalibrationMarkerScale(CalibrationMarkerTargetScale, CalibrationMarkerBaseScale, CalibrationScaleDurationSeconds);
            yield return new WaitForSecondsRealtime(CalibrationPostScaleDelaySeconds);
        }

        private IEnumerator AnimateCalibrationMarkerScale(float startScale, float endScale, float durationSeconds)
        {
            float elapsedSeconds = 0f;

            while (elapsedSeconds < durationSeconds)
            {
                float t = elapsedSeconds / durationSeconds;
                SetCalibrationMarkerScale(Mathf.Lerp(startScale, endScale, t));
                elapsedSeconds += Time.unscaledDeltaTime;
                yield return null;
            }

            SetCalibrationMarkerScale(endScale);
        }

        private bool TryCompleteScreenCalibration(out InvensunA8Coefficient coefficient, out bool meetsRecommendedThreshold)
        {
            coefficient = new InvensunA8Coefficient();
            meetsRecommendedThreshold = false;

            int computeRet = calibrationApi.ComputeCalibration(ref coefficient);
            int completeRet = calibrationApi.CompleteCalibration();

            leftScore = 0f;
            rightScore = 0f;
            int scoreRet = calibrationApi.GetCalibrationScore(ref leftScore, ref rightScore);

            if (computeRet != 0)
            {
                FailCalibration($"7Invensun compute calibration failed with code {computeRet}.");
                return false;
            }

            if (completeRet != 0)
            {
                FailCalibration($"7Invensun complete calibration failed with code {completeRet}.");
                return false;
            }

            if (scoreRet != 0)
            {
                FailCalibration($"7Invensun get calibration score failed with code {scoreRet}.");
                return false;
            }

            if (!InvensunA8CalibrationPersistenceUtility.IsValidCoefficientBuffer(coefficient.Buffer))
            {
                int byteCount = coefficient.Buffer?.Length ?? 0;
                FailCalibration(
                    $"7Invensun returned an invalid calibration coefficient buffer. Expected {InvensunA8CalibrationPersistenceUtility.ExpectedCoefficientBytes} bytes, got {byteCount}.");
                return false;
            }

            meetsRecommendedThreshold = InvensunA8CalibrationUtility.MeetsRecommendedScore(
                leftScore,
                rightScore,
                RecommendedScoreThreshold);
            completedCalibrationCoefficientBuffer = coefficient.Buffer;
            lastCalibrationMetadata = new InvensunA8CalibrationMetadata
            {
                AcceptedForPlay = false,
                MeetsRecommendedThreshold = meetsRecommendedThreshold,
                LeftScore = leftScore,
                RightScore = rightScore,
                RecommendedThreshold = RecommendedScoreThreshold,
                HasDepthCalibrationModel = false,
                DepthCalibrationRecordCount = 0,
                SavedAtUtc = DateTime.UtcNow.ToString("O")
            };
            return true;
        }

        private void EnterReviewForRejectedScreenCalibration()
        {
            ShutdownRuntime();
            state = CalibrationSceneState.Review;
            statusMessage = $"Screen-space xy calibration score below threshold {RecommendedScoreThreshold:F0}. Left={leftScore:F1}, Right={rightScore:F1}. Press R to recalibrate from the xy stage.";
        }

        private bool TryStartTracking(ref InvensunA8Coefficient coefficient)
        {
            int trackingRet;
            try
            {
                trackingRet = calibrationApi.StartTracking(ref coefficient);
            }
            catch (Exception exception)
            {
                FailCalibration($"7Invensun start tracking for depth calibration threw an exception: {exception.Message}");
                return false;
            }

            if (trackingRet != 0)
            {
                FailCalibration($"7Invensun start tracking for depth calibration failed with code {trackingRet}.");
                return false;
            }

            trackingStarted = true;
            return true;
        }

        private IEnumerator RunDepthCalibration()
        {
            state = CalibrationSceneState.DepthCalibrating;
            statusMessage = "Collecting depth calibration samples...";
            depthCalibrationRecordCount = 0;
            currentDepthPointIndex = -1;
            depthProgressTargetCount = GazeDepthCalibrationTargetLibrary.TargetCount;

            var runner = new InvensunA8DepthCalibrationRunner(
                TryResolveDepthCalibrationRayCamera,
                DepthCalibrationSampleWindowSeconds,
                DepthCalibrationMinimumSamples,
                BetweenPointsDelaySeconds * 0.5f,
                TryGetDepthCalibrationSample,
                EnsureDepthCalibrationVisual,
                SetDepthCalibrationMarkerVisible,
                SetDepthCalibrationMarkerWorldPosition,
                SetDepthCalibrationMarkerScale,
                OrientDepthCalibrationMarkerToCamera,
                AnimateDepthCalibrationMarkerMove,
                AnimateDepthCalibrationMarkerScaleCue,
                index => currentDepthPointIndex = index,
                recordCount => depthCalibrationRecordCount = recordCount,
                message => statusMessage = message);

            yield return runner.Run();
            if (!runner.Succeeded)
            {
                FailCalibration(runner.FailureMessage);
                yield break;
            }

            depthCalibrationDataset = runner.Dataset;
            depthCalibrationModel = runner.Model;
            depthCalibrationSucceeded = true;
        }

        private IEnumerator RunDepthValidation()
        {
            state = CalibrationSceneState.DepthCalibrating;
            statusMessage = "Collecting continuous depth validation samples...";
            depthCalibrationRecordCount = 0;
            currentDepthPointIndex = -1;
            depthProgressTargetCount = GazeDepthContinuousValidationTargetLibrary.TargetCount;

            var runner = new InvensunA8DepthValidationRunner(
                TryResolveDepthCalibrationRayCamera,
                DepthCalibrationSampleWindowSeconds,
                DepthCalibrationMinimumSamples,
                BetweenPointsDelaySeconds * 0.5f,
                TryGetDepthCalibrationSample,
                EnsureDepthCalibrationVisual,
                SetDepthCalibrationMarkerVisible,
                SetDepthCalibrationMarkerWorldPosition,
                SetDepthCalibrationMarkerScale,
                OrientDepthCalibrationMarkerToCamera,
                AnimateDepthCalibrationMarkerMove,
                AnimateDepthCalibrationMarkerScaleCue,
                index => currentDepthPointIndex = index,
                recordCount => depthCalibrationRecordCount = recordCount,
                message => statusMessage = message,
                new CalibratedGazeDepthEstimator(depthCalibrationModel));

            yield return runner.Run();
            if (!runner.Succeeded)
            {
                FailCalibration(runner.FailureMessage);
                yield break;
            }

            depthValidationSession = runner.Session;
        }

        private bool TryResolveDepthCalibrationRayCamera(out Camera rayCamera, out string reason)
        {
            if (displayModeController != null &&
                displayModeController.TryResolveCalibrationRayCamera(out rayCamera, out reason))
            {
                return true;
            }

            rayCamera = mainCamera;
            if (rayCamera == null)
            {
                reason = "Depth calibration ray camera is missing.";
                return false;
            }

            if (!GazeViewportPointUtility.TryBuildWorldRay(rayCamera, new Vector2(0.5f, 0.5f), out _))
            {
                reason = "Depth calibration fallback camera cannot produce a finite center viewport ray.";
                return false;
            }

            reason = "Using fallback main camera for depth calibration rays.";
            return true;
        }

        private bool TryGetDepthCalibrationSample(Vector2 targetViewportPoint, out BinocularGazeSample sample, out string rejectionReason)
        {
            lock (callbackLock)
            {
                sample = latestBinocularGazeSample;
                if (!hasLatestBinocularGazeSample)
                {
                    rejectionReason = "Waiting for a fresh A8 binocular gaze callback.";
                    return false;
                }
            }

            if (!sample.HasRecommendedScreenPoint)
            {
                rejectionReason = "A8 sample does not include a valid recommended gaze point.";
                return false;
            }

            if (!sample.HasStereoScreenPoints)
            {
                rejectionReason = "A8 sample does not include valid left and right screen gaze points.";
                return false;
            }

            Vector2 currentViewport = GazeViewportPointUtility.DisplayAreaToViewport(sample.RecommendedScreenPointNormalized);
            float viewportDistance = Vector2.Distance(currentViewport, targetViewportPoint);
            if (viewportDistance > DepthCalibrationMaxViewportError)
            {
                rejectionReason = $"Gaze is outside the target window. Viewport distance={viewportDistance:F3}, max={DepthCalibrationMaxViewportError:F3}.";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private bool PersistFinalCalibrationArtifacts(
            bool meetsRecommendedThreshold,
            GazeDepthCalibrationDataset dataset,
            GazeDepthModelBundle model,
            GazeDepthContinuousValidationSession validationSession)
        {
            bool acceptedForPlay = meetsRecommendedThreshold &&
                                   model != null &&
                                   dataset?.Records != null &&
                                   dataset.Records.Length > 0;
            if (!acceptedForPlay)
            {
                FailCalibration("Depth calibration artifacts are incomplete; calibration was not accepted for runtime use.");
                return false;
            }

            string calibrationRootPath = InvensunA8RuntimeLayoutUtility.BuildCalibrationPersistenceRootPath(Application.persistentDataPath);

            try
            {
                artifactService.SaveCoefficient(Application.persistentDataPath, completedCalibrationCoefficientBuffer);
                GazeDepthPersistence.Save(calibrationRootPath, dataset, model);
                string experimentDataRootPath = GazeDepthPersistence.ResolveUnityExperimentDataRootPath();
                GazeExperimentDataCapture.TrySaveContinuousValidationSession(experimentDataRootPath, validationSession);
            }
            catch (Exception exception)
            {
                FailCalibration($"Failed to save depth calibration artifacts: {exception.Message}");
                return false;
            }

            if (lastCalibrationMetadata == null)
            {
                lastCalibrationMetadata = new InvensunA8CalibrationMetadata
                {
                    LeftScore = leftScore,
                    RightScore = rightScore,
                    RecommendedThreshold = RecommendedScoreThreshold
                };
            }

            lastCalibrationMetadata.AcceptedForPlay = acceptedForPlay;
            lastCalibrationMetadata.MeetsRecommendedThreshold = meetsRecommendedThreshold;
            lastCalibrationMetadata.HasDepthCalibrationModel = true;
            lastCalibrationMetadata.DepthCalibrationRecordCount = dataset?.Records?.Length ?? 0;
            lastCalibrationMetadata.SavedAtUtc = DateTime.UtcNow.ToString("O");
            try
            {
                artifactService.SaveMetadata(Application.persistentDataPath, lastCalibrationMetadata);
            }
            catch (Exception exception)
            {
                FailCalibration($"Failed to save calibration metadata: {exception.Message}");
                return false;
            }

            return true;
        }

        private void FinalizeCalibrationState(bool meetsRecommendedThreshold)
        {
            ShutdownRuntime();

            if (meetsRecommendedThreshold)
            {
                state = CalibrationSceneState.Completed;
                statusMessage = $"Calibration completed. Left={leftScore:F1}, Right={rightScore:F1}.";
                if (continueCoroutine != null)
                {
                    StopCoroutine(continueCoroutine);
                }

                if (CalibrationSceneFlow.HasPendingRequestedScene())
                {
                    continueCoroutine = StartCoroutine(ContinueToRequestedSceneAfterDelay());
                }
                return;
            }

            state = CalibrationSceneState.Review;
            statusMessage = $"Calibration completed, but the score is below the recommended range. Left={leftScore:F1}, Right={rightScore:F1}.";
        }

        private void RestartCalibration()
        {
            if (calibrationCoroutine != null)
            {
                StopCoroutine(calibrationCoroutine);
            }

            if (continueCoroutine != null)
            {
                StopCoroutine(continueCoroutine);
                continueCoroutine = null;
            }

            currentPointIndex = -1;
            currentPointProgress = 0;
            ResetPostureState();
            completedCalibrationCoefficientBuffer = null;
            displayModeController?.ConfigureMonoForScreenCalibration();
            statusMessage = "Restarting calibration...";
            calibrationCoroutine = StartCoroutine(RunCalibrationFlow());
        }

        private void FailCalibration(string message)
        {
            ShutdownRuntime();
            state = CalibrationSceneState.Error;
            statusMessage = message;
            currentPointIndex = -1;
            currentPointProgress = 0;
            postureHoldProgressSeconds = 0f;
        }

        private void ContinueToRequestedScene()
        {
            ShutdownRuntime();
            state = CalibrationSceneState.Launching;
            CalibrationSceneFlow.CompleteCalibrationAndLoadRequestedScene();
        }

        private void SkipCalibrationAndContinueWithMouseFallback()
        {
            ShutdownRuntime();
            state = CalibrationSceneState.Launching;
            CalibrationSceneFlow.SkipCalibrationAndLoadRequestedScene();
        }

        private IEnumerator ContinueToRequestedSceneAfterDelay()
        {
            yield return new WaitForSecondsRealtime(CompletionStatusSeconds);
            continueCoroutine = null;
            ContinueToRequestedScene();
        }

        private bool TryStartRuntime()
        {
            string sourceRootPath = System.IO.Path.Combine(Application.streamingAssetsPath, "7ia8");
            ResetPostureState();
            if (!InvensunA8RuntimeLayoutUtility.TryStageRuntimeAssetsForNativeSdk(
                    sourceRootPath,
                    Application.persistentDataPath,
                    ResolveNativeSdkWorkingDirectory(),
                    out string configPath,
                    out _,
                    out string layoutStatus))
            {
                FailCalibration(layoutStatus);
                return false;
            }

            statusMessage = layoutStatus;

            EnsureCallbackHandle();
            IntPtr context = GCHandle.ToIntPtr(callbackHandle);

            int imageRet = InvensunA8Native.SetImageCallback(
                Marshal.GetFunctionPointerForDelegate(ImageCallbackDelegate),
                context);
            int gazeRet = InvensunA8Native.SetGazeCallback(
                Marshal.GetFunctionPointerForDelegate(GazeCallbackDelegate),
                context);
            if (imageRet != 0 || gazeRet != 0)
            {
                FailCalibration($"7Invensun callback registration failed. image={imageRet}, gaze={gazeRet}");
                return false;
            }

            int startRet;
            try
            {
                startRet = InvensunA8Native.Start(configPath);
            }
            catch (Exception exception)
            {
                FailCalibration($"7Invensun runtime start threw an exception: {exception.Message}");
                return false;
            }

            if (startRet != 0)
            {
                FailCalibration(InvensunA8NativeErrorMessages.DescribeRuntimeStartFailure(startRet, configPath));
                return false;
            }

            runtimeStarted = true;
            statusMessage = $"7Invensun calibration runtime started. Config path: {configPath}";
            return true;
        }

        private static string ResolveNativeSdkWorkingDirectory()
        {
            return InvensunA8RuntimeLayoutUtility.ResolveNativeSdkWorkingDirectory(
                Application.dataPath,
                Environment.CurrentDirectory);
        }

        private void ShutdownRuntime()
        {
            TryCancelCalibration();

            if (trackingStarted)
            {
                try
                {
                    InvensunA8Native.StopTracking();
                }
                catch
                {
                }
            }

            if (runtimeStarted)
            {
                try
                {
                    InvensunA8Native.Stop();
                }
                catch
                {
                }
            }

            trackingStarted = false;
            runtimeStarted = false;
            SetCalibrationMarkerVisible(false);
            SetDepthCalibrationMarkerVisible(false);
            ClearLatestBinocularGazeSample();

            if (callbackHandle.IsAllocated)
            {
                callbackHandle.Free();
            }
        }

        private void TryCancelCalibration()
        {
            try
            {
                calibrationApi.CancelCalibration();
            }
            catch
            {
            }
        }

        private void OnDestroy()
        {
            ShutdownRuntime();

            if (calibrationCanvas != null)
            {
                UnityObjectLifecycleUtility.DestroyObject(calibrationCanvas.gameObject);
            }

            if (depthCalibrationMarkerTransform != null)
            {
                UnityObjectLifecycleUtility.DestroyObject(depthCalibrationMarkerTransform.gameObject);
            }

            if (depthCalibrationMarkerSprite != null)
            {
                UnityObjectLifecycleUtility.DestroyObject(depthCalibrationMarkerSprite);
            }

            if (panelTexture != null)
            {
                UnityObjectLifecycleUtility.DestroyObject(panelTexture);
            }

            if (solidTexture != null)
            {
                UnityObjectLifecycleUtility.DestroyObject(solidTexture);
            }

            UnityObjectLifecycleUtility.UnloadAssetOrDestroy(calibrationPointTexture);
            UnityObjectLifecycleUtility.UnloadAssetOrDestroy(postureWindowTexture);
            UnityObjectLifecycleUtility.UnloadAssetOrDestroy(postureCatTexture);
            UnityObjectLifecycleUtility.UnloadAssetOrDestroy(postureCatAlertTexture);
        }

        private void OnApplicationQuit()
        {
            ShutdownRuntime();
        }

        private void EnsureCallbackHandle()
        {
            if (!callbackHandle.IsAllocated)
            {
                callbackHandle = GCHandle.Alloc(this);
            }
        }

        private void ResetPointCallbackState()
        {
            lock (callbackLock)
            {
                currentPointFinished = false;
                currentPointFailed = false;
                currentPointError = 0;
            }
        }

        private void ClearLatestBinocularGazeSample()
        {
            lock (callbackLock)
            {
                hasLatestBinocularGazeSample = false;
                latestBinocularGazeSample = default;
            }
        }

        private void ResetPostureState()
        {
            lock (callbackLock)
            {
                postureHoldProgressSeconds = 0f;
                hasPupilCenters = false;
                headCentered = false;
                averagePupilCenter = default;
                eyeAngleDegrees = 0f;
            }

            calibrationMarkerAnchoredPosition = Vector2.zero;
            SetCalibrationMarkerAnchoredPosition(calibrationMarkerAnchoredPosition);
            SetCalibrationMarkerScale(CalibrationMarkerBaseScale);
            SetCalibrationMarkerVisible(false);
            currentDepthPointIndex = -1;
            depthProgressTargetCount = 0;
            depthCalibrationRecordCount = 0;
            depthCalibrationSucceeded = false;
            depthCalibrationDataset = null;
            depthCalibrationModel = null;
            depthValidationSession = null;
            SetDepthCalibrationMarkerVisible(false);
        }

        private void HandleCalibrationProcess(int percent)
        {
            lock (callbackLock)
            {
                currentPointProgress = percent;
            }
        }

        private void HandleCalibrationFinish(int error)
        {
            lock (callbackLock)
            {
                currentPointProgress = 100;
                currentPointFinished = error == 0;
                currentPointFailed = error != 0;
                currentPointError = error;
            }
        }

        private void HandleGazeFrame(InvensunA8EyeDataFrame eyes)
        {
            Vector2 leftPupilCenter = new(eyes.LeftPupil.PupilCenter.X, eyes.LeftPupil.PupilCenter.Y);
            Vector2 rightPupilCenter = new(eyes.RightPupil.PupilCenter.X, eyes.RightPupil.PupilCenter.Y);
            bool postureAvailable = InvensunA8PostureUtility.TryResolveAveragePupilCenter(
                leftPupilCenter,
                rightPupilCenter,
                out Vector2 averagedPupilCenter);
            bool centered = postureAvailable &&
                            InvensunA8PostureUtility.IsHeadCentered(
                                averagedPupilCenter,
                                PostureCenteredMin,
                                PostureCenteredMax);
            float angleDegrees = InvensunA8PostureUtility.CalculateEyeAngleDegrees(leftPupilCenter, rightPupilCenter);
            Vector2 recommendedPoint = new(eyes.RecommendedGaze.GazePoint.X, eyes.RecommendedGaze.GazePoint.Y);
            bool recommendedPointValid = InvensunA8BitMaskUtility.IsFlagSet(eyes.RecommendedGaze.GazeBitMask, InvensunA8GazeValidityBit.GazePoint);
            long timestamp = unchecked((long)eyes.Timestamp);
            var binocularSample = InvensunA8BinocularGazeSampleUtility.CreateSample(
                eyes,
                recommendedPoint,
                recommendedPointValid,
                timestamp);

            lock (callbackLock)
            {
                hasPupilCenters = postureAvailable;
                averagePupilCenter = averagedPupilCenter;
                headCentered = centered;
                eyeAngleDegrees = angleDegrees;
                hasLatestBinocularGazeSample = binocularSample.HasAnySignal;
                latestBinocularGazeSample = binocularSample;
            }
        }

        private void DrawPostureAlignmentVisual()
        {
            float windowX = (Screen.width - PostureWindowSize) * 0.5f;
            float windowY = (Screen.height - PostureWindowSize) * 0.5f - 36f;
            var windowRect = new Rect(windowX, windowY, PostureWindowSize, PostureWindowSize);

            if (postureWindowTexture != null)
            {
                GUI.DrawTexture(windowRect, postureWindowTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUI.DrawTexture(windowRect, solidTexture);
            }

            bool postureAvailable;
            bool centered;
            Vector2 averagedCenter;
            float angleDegrees;

            lock (callbackLock)
            {
                postureAvailable = hasPupilCenters;
                centered = headCentered;
                averagedCenter = averagePupilCenter;
                angleDegrees = eyeAngleDegrees;
            }

            Vector2 catCenter = new(windowRect.center.x, windowRect.center.y);
            if (postureAvailable)
            {
                catCenter.x += (averagedCenter.x - 0.5f) * (PostureWindowSize * 0.92f);
                catCenter.y += (0.5f - averagedCenter.y) * (PostureWindowSize * 0.92f);
            }

            var catRect = new Rect(
                catCenter.x - PostureCatSize * 0.5f,
                catCenter.y - PostureCatSize * 0.5f,
                PostureCatSize,
                PostureCatSize);
            var catTexture = centered ? postureCatTexture : postureCatAlertTexture;
            if (catTexture != null)
            {
                Matrix4x4 previousMatrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(angleDegrees, catCenter);
                GUI.DrawTexture(catRect, catTexture, ScaleMode.ScaleToFit, true);
                GUI.matrix = previousMatrix;
            }

            float fill = Mathf.Clamp01(postureHoldProgressSeconds / PostureHoldSeconds);
            float barX = (Screen.width - PostureProgressBarWidth) * 0.5f;
            float barY = windowRect.yMax + 24f;
            var backgroundRect = new Rect(barX, barY, PostureProgressBarWidth, PostureProgressBarHeight);
            var fillRect = new Rect(barX, barY, PostureProgressBarWidth * fill, PostureProgressBarHeight);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.18f);
            GUI.DrawTexture(backgroundRect, solidTexture);
            GUI.color = centered
                ? new Color(0.23f, 0.86f, 0.44f, 0.94f)
                : new Color(0.93f, 0.25f, 0.18f, 0.94f);
            GUI.DrawTexture(fillRect, solidTexture);
            GUI.color = previousColor;
        }

        private void EnsureCalibrationVisual()
        {
            EnsureGuiResources();

            if (calibrationCanvas == null)
            {
                var canvasObject = new GameObject(
                    "InvensunA8CalibrationCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));

                calibrationCanvas = canvasObject.GetComponent<Canvas>();
                calibrationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                calibrationCanvas.sortingOrder = short.MaxValue;
                calibrationCanvas.pixelPerfect = false;

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = InvensunA8CalibrationUtility.VendorCalibrationReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;

            }

            if (calibrationMarkerImage == null)
            {
                var markerObject = new GameObject(
                    "CalibrationMarker",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(RawImage));
                markerObject.transform.SetParent(calibrationCanvas.transform, false);

                calibrationMarkerRect = markerObject.GetComponent<RectTransform>();
                calibrationMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
                calibrationMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
                calibrationMarkerRect.pivot = new Vector2(0.5f, 0.5f);
                calibrationMarkerRect.sizeDelta = new Vector2(CalibrationMarkerSize, CalibrationMarkerSize);

                calibrationMarkerImage = markerObject.GetComponent<RawImage>();
                calibrationMarkerImage.texture = calibrationPointTexture != null ? calibrationPointTexture : solidTexture;
                calibrationMarkerImage.raycastTarget = false;

                SetCalibrationMarkerScale(CalibrationMarkerBaseScale);
                SetCalibrationMarkerVisible(false);
            }
        }

        private void EnsureDepthCalibrationVisual()
        {
            EnsureGuiResources();

            if (depthCalibrationMarkerTransform != null)
            {
                return;
            }

            var markerObject = new GameObject("DepthCalibrationMarker", typeof(SpriteRenderer));
            depthCalibrationMarkerTransform = markerObject.transform;
            depthCalibrationMarkerRenderer = markerObject.GetComponent<SpriteRenderer>();
            depthCalibrationMarkerRenderer.sprite = ResolveDepthCalibrationSprite();
            depthCalibrationMarkerRenderer.sortingOrder = short.MaxValue;
            depthCalibrationMarkerRenderer.color = Color.white;
            depthCalibrationMarkerTransform.localScale = Vector3.one * DepthCalibrationMarkerWorldScale;
            SetDepthCalibrationMarkerVisible(false);
        }

        private Sprite ResolveDepthCalibrationSprite()
        {
            if (depthCalibrationMarkerSprite == null)
            {
                var sourceTexture = calibrationPointTexture != null ? calibrationPointTexture : solidTexture;
                depthCalibrationMarkerSprite = Sprite.Create(
                    sourceTexture,
                    new Rect(0f, 0f, sourceTexture.width, sourceTexture.height),
                    new Vector2(0.5f, 0.5f),
                    sourceTexture.width);
            }

            return depthCalibrationMarkerSprite;
        }

        private IEnumerator AnimateDepthCalibrationMarkerMove(Vector3 targetWorldPoint)
        {
            if (depthCalibrationMarkerTransform == null)
            {
                yield break;
            }

            while (Vector3.Distance(depthCalibrationMarkerTransform.position, targetWorldPoint) > 0.01f)
            {
                depthCalibrationMarkerTransform.position = Vector3.Lerp(
                    depthCalibrationMarkerTransform.position,
                    targetWorldPoint,
                    CalibrationMoveSpeed * Time.unscaledDeltaTime);
                OrientDepthCalibrationMarkerToCamera();
                yield return null;
            }

            depthCalibrationMarkerTransform.position = targetWorldPoint;
            OrientDepthCalibrationMarkerToCamera();
        }

        private IEnumerator AnimateDepthCalibrationMarkerScaleCue()
        {
            yield return AnimateDepthCalibrationMarkerScale(CalibrationMarkerBaseScale, CalibrationMarkerTargetScale, CalibrationScaleDurationSeconds);
            yield return AnimateDepthCalibrationMarkerScale(CalibrationMarkerTargetScale, CalibrationMarkerBaseScale, CalibrationScaleDurationSeconds);
            yield return new WaitForSecondsRealtime(CalibrationPostScaleDelaySeconds);
        }

        private IEnumerator AnimateDepthCalibrationMarkerScale(float startScale, float endScale, float durationSeconds)
        {
            float elapsedSeconds = 0f;

            while (elapsedSeconds < durationSeconds)
            {
                float t = elapsedSeconds / durationSeconds;
                SetDepthCalibrationMarkerScale(Mathf.Lerp(startScale, endScale, t));
                elapsedSeconds += Time.unscaledDeltaTime;
                OrientDepthCalibrationMarkerToCamera();
                yield return null;
            }

            SetDepthCalibrationMarkerScale(endScale);
            OrientDepthCalibrationMarkerToCamera();
        }

        private void SetDepthCalibrationMarkerScale(float normalizedScale)
        {
            if (depthCalibrationMarkerTransform == null)
            {
                return;
            }

            if (!GazeViewportPointUtility.IsFinite(normalizedScale) || normalizedScale <= 0f)
            {
                return;
            }

            float worldScale = DepthCalibrationMarkerWorldScale * normalizedScale;
            depthCalibrationMarkerTransform.localScale = Vector3.one * worldScale;
        }

        private void SetDepthCalibrationMarkerWorldPosition(Vector3 worldPosition)
        {
            if (depthCalibrationMarkerTransform == null)
            {
                return;
            }

            if (!GazeViewportPointUtility.IsFiniteVector3(worldPosition))
            {
                statusMessage = "Depth calibration marker received a non-finite world position and was not moved.";
                return;
            }

            depthCalibrationMarkerTransform.position = worldPosition;
        }

        private void SetDepthCalibrationMarkerVisible(bool visible)
        {
            if (depthCalibrationMarkerRenderer == null)
            {
                return;
            }

            depthCalibrationMarkerRenderer.enabled = visible;
        }

        private void OrientDepthCalibrationMarkerToCamera()
        {
            Transform facingTransform = displayModeController != null
                ? displayModeController.ResolveCalibrationFacingTransform()
                : mainCamera != null ? mainCamera.transform : null;

            if (depthCalibrationMarkerTransform == null || facingTransform == null)
            {
                return;
            }

            if (!GazeViewportPointUtility.IsFiniteVector3(depthCalibrationMarkerTransform.position) ||
                !GazeViewportPointUtility.IsFiniteVector3(facingTransform.position))
            {
                return;
            }

            Vector3 facingDirection = depthCalibrationMarkerTransform.position - facingTransform.position;
            if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            depthCalibrationMarkerTransform.forward = facingDirection;
        }

        private void SetCalibrationMarkerAnchoredPosition(Vector2 anchoredPosition)
        {
            if (calibrationMarkerRect == null)
            {
                return;
            }

            calibrationMarkerRect.anchoredPosition = anchoredPosition;
        }

        private void SetCalibrationMarkerScale(float scale)
        {
            if (calibrationMarkerRect == null)
            {
                return;
            }

            calibrationMarkerRect.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetCalibrationMarkerVisible(bool visible)
        {
            if (calibrationMarkerImage == null)
            {
                return;
            }

            calibrationMarkerImage.enabled = visible;
        }

        private Vector2 ResolveCalibrationMarkerSdkPoint()
        {
            if (calibrationMarkerRect == null)
            {
                return screenCalibrationPointRunner.GetSdkPoint(
                    screenCalibrationPointRunner.GetAnchoredPosition(currentPointIndex));
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, calibrationMarkerRect.position);
            return new Vector2(
                Mathf.Clamp01(screenPoint.x / Screen.width),
                Mathf.Clamp01(1f - (screenPoint.y / Screen.height)));
        }

        private void EnsureGuiResources()
        {
            if (panelTexture == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.04f, 0.05f, 0.08f, 0.90f));
                panelTexture.Apply();
            }

            if (solidTexture == null)
            {
                solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                solidTexture.SetPixel(0, 0, Color.white);
                solidTexture.Apply();
            }

            if (calibrationPointTexture == null)
            {
                calibrationPointTexture = Resources.Load<Texture2D>("InvensunA8/Calibration_Point");
            }

            if (postureWindowTexture == null)
            {
                postureWindowTexture = Resources.Load<Texture2D>("InvensunA8/window");
            }

            if (postureCatTexture == null)
            {
                postureCatTexture = Resources.Load<Texture2D>("InvensunA8/cat");
            }

            if (postureCatAlertTexture == null)
            {
                postureCatAlertTexture = Resources.Load<Texture2D>("InvensunA8/catred");
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = Color.white;
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    wordWrap = true
                };
                bodyStyle.normal.textColor = new Color(0.92f, 0.95f, 1f);
            }

            if (warningStyle == null)
            {
                warningStyle = new GUIStyle(bodyStyle);
                warningStyle.normal.textColor = new Color(1f, 0.82f, 0.36f);
            }

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(18, 18, 18, 18)
                };
                panelStyle.normal.background = panelTexture;
            }
        }

        private static void OnImageCallback(int eye, IntPtr image, int size, int width, int height, long timestamp, IntPtr context)
        {
        }

        private static void OnGazeCallback(ref InvensunA8EyeDataFrame eyes, IntPtr context)
        {
            if (!TryResolveController(context, out var controller))
            {
                return;
            }

            controller.HandleGazeFrame(eyes);
        }

        private static void OnCalibrationProcessCallback(int index, int percent, IntPtr context)
        {
            if (!TryResolveController(context, out var controller))
            {
                return;
            }

            controller.HandleCalibrationProcess(percent);
        }

        private static void OnCalibrationFinishCallback(int index, int error, IntPtr context)
        {
            if (!TryResolveController(context, out var controller))
            {
                return;
            }

            controller.HandleCalibrationFinish(error);
        }

        private static bool TryResolveController(IntPtr context, out InvensunA8CalibrationSceneController controller)
        {
            controller = null;

            if (context == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var handle = GCHandle.FromIntPtr(context);
                controller = handle.Target as InvensunA8CalibrationSceneController;
                return controller != null;
            }
            catch
            {
                return false;
            }
        }

        private enum CalibrationSceneState
        {
            Checking,
            StartingRuntime,
            AligningPosture,
            Calibrating,
            DepthCalibrating,
            Completed,
            Review,
            Error,
            Launching
        }
    }
}
