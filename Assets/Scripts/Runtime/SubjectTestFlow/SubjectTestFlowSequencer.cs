using ProjectGaze.Calibration;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.SubjectTest
{
    public enum SubjectTestFlowStage
    {
        Idle,
        Calibration,
        LayeredPages,
        LoadingDepthGatedAgent,
        DepthGatedAgent,
        Completed
    }

    public sealed class SubjectTestFlowSequencer
    {
        public const string FlowSceneName = "SubjectTestFlowScene";
        public const string CalibrationSceneName = CalibrationSceneFlow.CalibrationSceneName;
        public const string LayeredPagesSceneName = LayeredPagesDemo.SceneName;
        public const string DepthGatedAgentSceneName = DepthGatedAgentDemo.SceneName;
        public const float FormalSceneDurationSeconds = 120.0f;

        public SubjectTestFlowStage Stage { get; private set; } = SubjectTestFlowStage.Idle;

        public float RemainingSeconds { get; private set; }

        public void StartCalibration()
        {
            Stage = SubjectTestFlowStage.Calibration;
            RemainingSeconds = 0f;
        }

        public void NotifySceneLoaded(string sceneName)
        {
            if (string.Equals(sceneName, LayeredPagesSceneName, System.StringComparison.Ordinal))
            {
                Stage = SubjectTestFlowStage.LayeredPages;
                RemainingSeconds = FormalSceneDurationSeconds;
                return;
            }

            if (string.Equals(sceneName, DepthGatedAgentSceneName, System.StringComparison.Ordinal))
            {
                Stage = SubjectTestFlowStage.DepthGatedAgent;
                RemainingSeconds = FormalSceneDurationSeconds;
                return;
            }

            if (string.Equals(sceneName, CalibrationSceneName, System.StringComparison.Ordinal))
            {
                Stage = SubjectTestFlowStage.Calibration;
                RemainingSeconds = 0f;
            }
        }

        public string Tick(float deltaSeconds)
        {
            if (Stage != SubjectTestFlowStage.LayeredPages &&
                Stage != SubjectTestFlowStage.DepthGatedAgent)
            {
                return null;
            }

            RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Mathf.Max(0f, deltaSeconds));
            if (RemainingSeconds > 0f)
            {
                return null;
            }

            if (Stage == SubjectTestFlowStage.LayeredPages)
            {
                Stage = SubjectTestFlowStage.LoadingDepthGatedAgent;
                return DepthGatedAgentSceneName;
            }

            Stage = SubjectTestFlowStage.Completed;
            return null;
        }
    }
}
