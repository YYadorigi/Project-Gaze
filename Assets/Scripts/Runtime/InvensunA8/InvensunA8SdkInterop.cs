using System;
using System.Runtime.InteropServices;

namespace ProjectGaze.Gaze.Providers
{
    public enum InvensunA8GazeValidityBit : byte
    {
        GazePoint = 0,
        RawPoint = 1,
        SmoothPoint = 2,
        GazeOrigin = 3,
        GazeDirection = 4,
        Re = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8Point2D
    {
        public float X;
        public float Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8Point3D
    {
        public float X;
        public float Y;
        public float Z;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8Coefficient
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = InvensunA8Native.CoefficientBytes)]
        public byte[] Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8GazePoint
    {
        public uint GazeBitMask;
        public InvensunA8Point3D GazePoint;
        public InvensunA8Point3D RawPoint;
        public InvensunA8Point3D SmoothPoint;
        public InvensunA8Point3D GazeOrigin;
        public InvensunA8Point3D GazeDirection;
        public float Re;
        public uint ExtendedDataBitMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public float[] ExtendedData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8PupilInfo
    {
        public uint PupilBitMask;
        public InvensunA8Point2D PupilCenter;
        public float PupilDistance;
        public float PupilDiameter;
        public float PupilDiameterMillimeters;
        public float PupilMinorAxis;
        public float PupilMinorAxisMillimeters;
        public uint ExtendedDataBitMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public float[] ExtendedData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8EyeExtraData
    {
        public uint EyeDataBitMask;
        public int Blink;
        public float Openness;
        public float EyelidUp;
        public float EyelidDown;
        public uint ExtendedDataBitMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public float[] ExtendedData;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8FixationSaccade
    {
        public long Timestamp;
        public long Duration;
        public int Count;
        public int State;
        public InvensunA8Point2D Center;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct InvensunA8EyeDataFrame
    {
        public ulong Timestamp;
        public int Recommend;
        public InvensunA8GazePoint RecommendedGaze;
        public InvensunA8GazePoint LeftGaze;
        public InvensunA8GazePoint RightGaze;
        public InvensunA8PupilInfo LeftPupil;
        public InvensunA8PupilInfo RightPupil;
        public InvensunA8EyeExtraData LeftEyeExtra;
        public InvensunA8EyeExtraData RightEyeExtra;
        public InvensunA8FixationSaccade Statistics;
    }

    public static class InvensunA8BitMaskUtility
    {
        public static bool IsFlagSet(uint bitMask, InvensunA8GazeValidityBit bit)
        {
            return ((bitMask >> (int)bit) & 1u) == 1u;
        }
    }

    internal static class InvensunA8Native
    {
        internal const int CoefficientBytes = 1024;

        internal delegate void ImageCallback(
            int eye,
            IntPtr image,
            int size,
            int width,
            int height,
            long timestamp,
            IntPtr context);

        internal delegate void GazeCallback(ref InvensunA8EyeDataFrame eyes, IntPtr context);

        internal delegate void CalibrationProcessCallback(int index, int percent, IntPtr context);

        internal delegate void CalibrationFinishCallback(int index, int error, IntPtr context);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_set_image_callback", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int SetImageCallback(IntPtr callback, IntPtr context);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_set_gaze_callback", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int SetGazeCallback(IntPtr callback, IntPtr context);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_set_smooth", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int SetSmooth(int smooth);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_start", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int Start([MarshalAs(UnmanagedType.LPStr)] string configPath);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_stop", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int Stop();

        [DllImport("aSeeX.dll", EntryPoint = "_7i_start_tracking", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int StartTracking(ref InvensunA8Coefficient coefficient);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_stop_tracking", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int StopTracking();

        [DllImport("aSeeX.dll", EntryPoint = "_7i_start_calibration", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int StartCalibration(int points);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_start_calibration_point", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int StartCalibrationPoint(
            int index,
            ref InvensunA8Point2D point,
            IntPtr processCallback,
            IntPtr processContext,
            IntPtr finishCallback,
            IntPtr finishContext);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_cancel_calibration", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int CancelCalibration();

        [DllImport("aSeeX.dll", EntryPoint = "_7i_compute_calibration", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int ComputeCalibration(ref InvensunA8Coefficient coefficient);

        [DllImport("aSeeX.dll", EntryPoint = "_7i_complete_calibration", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int CompleteCalibration();

        [DllImport("aSeeX.dll", EntryPoint = "_7i_get_calibration_score", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        internal static extern int GetCalibrationScore(ref float leftScore, ref float rightScore);
    }
}
