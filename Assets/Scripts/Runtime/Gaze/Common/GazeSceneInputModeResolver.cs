using ProjectGaze.Gaze.Providers;
using ProjectGaze.Hardware.ThinkVision;

namespace ProjectGaze.Gaze
{
    public static class GazeSceneInputModeResolver
    {
        public static bool ShouldUseStereoGazeInput(
            IThinkVisionDisplayBridge displayBridge,
            string persistentDataPath,
            bool forceMouseFallback)
        {
            return displayBridge != null &&
                   displayBridge.IsStereoDisplayActive &&
                   !forceMouseFallback &&
                   InvensunA8CalibrationPersistenceUtility.TryLoadAcceptedCalibration(
                       persistentDataPath,
                       out _);
        }
    }
}
