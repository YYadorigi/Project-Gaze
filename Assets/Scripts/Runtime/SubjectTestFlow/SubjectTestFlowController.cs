using ProjectGaze.Calibration;
using ProjectGaze.Gaze;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGaze.SubjectTest
{
    public enum SubjectTestFlowDataCaptureMode
    {
        EnvironmentVariable = 0,
        ForceEnabled = 1,
        ForceDisabled = 2
    }

    public sealed class SubjectTestFlowController : MonoBehaviour
    {
        private static SubjectTestFlowController activeController;

        [SerializeField]
        [Tooltip("Controls ExperimentData logging for this full subject-test flow. EnvironmentVariable follows PROJECT_GAZE_EXPERIMENT_DATA.")]
        private SubjectTestFlowDataCaptureMode dataCaptureMode = SubjectTestFlowDataCaptureMode.EnvironmentVariable;

        private readonly SubjectTestFlowSequencer sequencer = new();
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle panelStyle;
        private Texture2D panelTexture;
        private bool flowStarted;
        private bool quitRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoadedForBootstrap;
            SceneManager.sceneLoaded += OnSceneLoadedForBootstrap;
        }

        private static void OnSceneLoadedForBootstrap(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (activeController != null)
            {
                return;
            }

            SceneComponentBootstrapUtility.EnsureSceneComponent<SubjectTestFlowController>(
                scene,
                SubjectTestFlowSequencer.FlowSceneName,
                "SubjectTestFlowController");
        }

        private void Awake()
        {
            if (activeController != null && activeController != this)
            {
                Destroy(gameObject);
                return;
            }

            activeController = this;
            ApplyDataCaptureMode();
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            StartFlow();
        }

        private void Update()
        {
            string sceneToLoad = sequencer.Tick(Time.unscaledDeltaTime);
            if (!string.IsNullOrWhiteSpace(sceneToLoad))
            {
                SceneManager.LoadScene(sceneToLoad);
                return;
            }

            if (sequencer.Stage == SubjectTestFlowStage.Completed && !quitRequested)
            {
                quitRequested = true;
                QuitApplication();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (sequencer.Stage == SubjectTestFlowStage.LayeredPages ||
                sequencer.Stage == SubjectTestFlowStage.DepthGatedAgent ||
                sequencer.Stage == SubjectTestFlowStage.Calibration)
            {
                DrawStatusOverlay();
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (activeController == this)
            {
                GazeExperimentDataCapture.SetEnabledOverride(null);
                activeController = null;
            }

            if (panelTexture != null)
            {
                UnityObjectLifecycleUtility.DestroyObject(panelTexture);
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && activeController == this)
            {
                ApplyDataCaptureMode();
            }
        }

        public static bool? ResolveDataCaptureOverride(SubjectTestFlowDataCaptureMode mode)
        {
            switch (mode)
            {
                case SubjectTestFlowDataCaptureMode.ForceEnabled:
                    return true;

                case SubjectTestFlowDataCaptureMode.ForceDisabled:
                    return false;

                default:
                    return null;
            }
        }

        private void ApplyDataCaptureMode()
        {
            GazeExperimentDataCapture.SetEnabledOverride(ResolveDataCaptureOverride(dataCaptureMode));
        }

        private void StartFlow()
        {
            if (flowStarted)
            {
                return;
            }

            flowStarted = true;
            sequencer.StartCalibration();
            CalibrationSceneFlow.RequestSceneAfterCalibration(SubjectTestFlowSequencer.LayeredPagesSceneName);
            SceneManager.LoadScene(SubjectTestFlowSequencer.CalibrationSceneName);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            sequencer.NotifySceneLoaded(scene.name);
        }

        private void DrawStatusOverlay()
        {
            const float width = 360f;
            const float height = 126f;
            var rect = new Rect(Screen.width - width - 18f, 18f, width, height);
            GUI.Box(rect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, width - 32f, 28f), "Subject Test Flow", titleStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 46f, width - 32f, 72f), BuildStatusText(), bodyStyle);
        }

        private string BuildStatusText()
        {
            string captureStatus = BuildCaptureStatusText();

            switch (sequencer.Stage)
            {
                case SubjectTestFlowStage.Calibration:
                    return $"Stage 1/3: eye tracking calibration and temporary depth validation.\n{captureStatus}";

                case SubjectTestFlowStage.LayeredPages:
                    return $"Stage 2/3: layered pages. Remaining {Mathf.CeilToInt(sequencer.RemainingSeconds)}s.\n{captureStatus}";

                case SubjectTestFlowStage.DepthGatedAgent:
                    return $"Stage 3/3: AI logo panel. Remaining {Mathf.CeilToInt(sequencer.RemainingSeconds)}s.\n{captureStatus}";

                default:
                    return $"Preparing next stage...\n{captureStatus}";
            }
        }

        private string BuildCaptureStatusText()
        {
            string modeText;
            switch (dataCaptureMode)
            {
                case SubjectTestFlowDataCaptureMode.ForceEnabled:
                    modeText = "forced on";
                    break;

                case SubjectTestFlowDataCaptureMode.ForceDisabled:
                    modeText = "forced off";
                    break;

                default:
                    modeText = "environment/default";
                    break;
            }

            return GazeExperimentDataCapture.IsEnabled
                ? $"Data capture: on ({modeText})"
                : $"Data capture: off ({modeText})";
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
                titleStyle.normal.textColor = Color.white;
            }

            if (bodyStyle == null)
            {
                bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    wordWrap = true
                };
                bodyStyle.normal.textColor = new Color(0.92f, 0.95f, 1f);
            }

            if (panelTexture == null)
            {
                panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                panelTexture.SetPixel(0, 0, new Color(0.05f, 0.08f, 0.12f, 0.86f));
                panelTexture.Apply();
            }

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box);
                panelStyle.normal.background = panelTexture;
            }
        }
    }
}
