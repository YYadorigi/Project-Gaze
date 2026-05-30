using ProjectGaze.Gaze.Providers;
using UnityEngine;

namespace ProjectGaze.Calibration
{
    internal sealed class ScreenCalibrationPointRunner
    {
        public int PointCount => InvensunA8CalibrationUtility.CalibrationPointCount;

        public Vector2 GetAnchoredPosition(int pointIndex)
        {
            return InvensunA8CalibrationUtility.GetDemoStyleCalibrationAnchoredPosition(pointIndex);
        }

        public Vector2 GetSdkPoint(Vector2 anchoredPosition)
        {
            return InvensunA8CalibrationUtility.ConvertAnchoredPositionToSdkPoint(
                anchoredPosition,
                InvensunA8CalibrationUtility.VendorCalibrationReferenceResolution);
        }
    }
}
