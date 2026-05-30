using System;

namespace ProjectGaze.Gaze.Providers
{
    internal interface IInvensunA8CalibrationApi
    {
        int StartCalibration(int pointCount);

        int StartCalibrationPoint(
            int pointIndex,
            ref InvensunA8Point2D point,
            IntPtr processCallback,
            IntPtr processContext,
            IntPtr finishCallback,
            IntPtr finishContext);

        int ComputeCalibration(ref InvensunA8Coefficient coefficient);

        int CompleteCalibration();

        int GetCalibrationScore(ref float leftScore, ref float rightScore);

        int CancelCalibration();

        int StartTracking(ref InvensunA8Coefficient coefficient);
    }

    internal sealed class InvensunA8CalibrationApi : IInvensunA8CalibrationApi
    {
        public int StartCalibration(int pointCount)
        {
            return InvensunA8Native.StartCalibration(pointCount);
        }

        public int StartCalibrationPoint(
            int pointIndex,
            ref InvensunA8Point2D point,
            IntPtr processCallback,
            IntPtr processContext,
            IntPtr finishCallback,
            IntPtr finishContext)
        {
            return InvensunA8Native.StartCalibrationPoint(
                pointIndex,
                ref point,
                processCallback,
                processContext,
                finishCallback,
                finishContext);
        }

        public int ComputeCalibration(ref InvensunA8Coefficient coefficient)
        {
            return InvensunA8Native.ComputeCalibration(ref coefficient);
        }

        public int CompleteCalibration()
        {
            return InvensunA8Native.CompleteCalibration();
        }

        public int GetCalibrationScore(ref float leftScore, ref float rightScore)
        {
            return InvensunA8Native.GetCalibrationScore(ref leftScore, ref rightScore);
        }

        public int CancelCalibration()
        {
            return InvensunA8Native.CancelCalibration();
        }

        public int StartTracking(ref InvensunA8Coefficient coefficient)
        {
            return InvensunA8Native.StartTracking(ref coefficient);
        }
    }
}
