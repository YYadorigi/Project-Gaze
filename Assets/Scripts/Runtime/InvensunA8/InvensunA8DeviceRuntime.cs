using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ProjectGaze.Gaze.Providers
{
    public sealed class InvensunA8DeviceRuntime : MonoBehaviour
    {
        private const float RetryIntervalSeconds = 1.0f;

        private static readonly InvensunA8Native.ImageCallback ImageCallbackDelegate = OnImageCallback;
        private static readonly InvensunA8Native.GazeCallback GazeCallbackDelegate = OnGazeCallback;

        private readonly object sampleLock = new();
        private readonly Queue<(LogType logType, string message)> pendingLogs = new();

        private GCHandle callbackHandle;
        private InvensunA8RawGazeSample latestSample;
        private bool initialized;
        private bool runtimeStarted;
        private bool trackingStarted;
        private bool hasSample;
        private float nextRuntimeAttemptAt;
        private float nextTrackingAttemptAt;
        private string lastStatus = "Starting 7Invensun A8 runtime...";

        public bool IsConnected => trackingStarted;

        public string LastStatus => lastStatus;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            nextRuntimeAttemptAt = 0f;
            nextTrackingAttemptAt = 0f;
        }

        internal bool TryGetLatestSample(out InvensunA8RawGazeSample sample)
        {
            lock (sampleLock)
            {
                sample = latestSample;
                return hasSample;
            }
        }

        private void Update()
        {
            FlushPendingLogs();

            if (!initialized)
            {
                return;
            }

            if (!runtimeStarted && Time.unscaledTime >= nextRuntimeAttemptAt)
            {
                TryStartRuntime();
                return;
            }

            if (runtimeStarted && !trackingStarted && Time.unscaledTime >= nextTrackingAttemptAt)
            {
                TryStartTracking();
            }
        }

        private void OnDestroy()
        {
            StopRuntime();
        }

        private void OnApplicationQuit()
        {
            StopRuntime();
        }

        private void TryStartRuntime()
        {
            nextRuntimeAttemptAt = Time.unscaledTime + RetryIntervalSeconds;

            if (!TryPrepareRuntimeLayout(out string configPath, out string layoutStatus))
            {
                lastStatus = layoutStatus;
                EnqueueLog(LogType.Error, lastStatus);
                return;
            }

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
                lastStatus = $"7Invensun callback registration failed. image={imageRet}, gaze={gazeRet}";
                EnqueueLog(LogType.Error, lastStatus);
                return;
            }

            int startRet;
            try
            {
                startRet = InvensunA8Native.Start(configPath);
            }
            catch (Exception exception)
            {
                lastStatus = $"7Invensun runtime start threw an exception: {exception.Message}";
                EnqueueLog(LogType.Error, lastStatus);
                return;
            }

            if (startRet != 0)
            {
                lastStatus = InvensunA8NativeErrorMessages.DescribeRuntimeStartFailure(startRet, configPath);
                EnqueueLog(LogType.Error, lastStatus);
                return;
            }

            runtimeStarted = true;
            nextTrackingAttemptAt = 0f;
            lastStatus = $"7Invensun A8 runtime started. Config path: {configPath}";
            EnqueueLog(LogType.Log, lastStatus);
        }

        private void TryStartTracking()
        {
            nextTrackingAttemptAt = Time.unscaledTime + RetryIntervalSeconds;

            var coefficient = LoadCoefficient(out string coefficientSource);

            int trackingRet;
            try
            {
                trackingRet = InvensunA8Native.StartTracking(ref coefficient);
            }
            catch (Exception exception)
            {
                lastStatus = $"7Invensun start tracking threw an exception: {exception.Message}";
                EnqueueLog(LogType.Error, lastStatus);
                return;
            }

            if (trackingRet != 0)
            {
                lastStatus = $"7Invensun start tracking failed with code {trackingRet}.";
                EnqueueLog(LogType.Error, $"{lastStatus} Coefficient source: {coefficientSource}");
                return;
            }

            trackingStarted = true;

            lock (sampleLock)
            {
                latestSample = default;
                hasSample = false;
            }

            lastStatus = $"7Invensun A8 tracking started. Coefficient source: {coefficientSource}";
            EnqueueLog(LogType.Log, lastStatus);
        }

        private void StopRuntime()
        {
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
            lastStatus = "7Invensun A8 runtime stopped.";

            lock (sampleLock)
            {
                latestSample = default;
                hasSample = false;
            }

            if (callbackHandle.IsAllocated)
            {
                callbackHandle.Free();
            }
        }

        private void HandleGazeCallback(InvensunA8EyeDataFrame frame)
        {
            Vector2 recommendedPoint = new(frame.RecommendedGaze.GazePoint.X, frame.RecommendedGaze.GazePoint.Y);
            bool recommendedPointValid = InvensunA8BitMaskUtility.IsFlagSet(frame.RecommendedGaze.GazeBitMask, InvensunA8GazeValidityBit.GazePoint);
            long timestamp = unchecked((long)frame.Timestamp);
            var binocularGaze = InvensunA8BinocularGazeSampleUtility.CreateSample(
                frame,
                recommendedPoint,
                recommendedPointValid,
                timestamp);
            var sample = new InvensunA8RawGazeSample(
                recommendedPoint,
                recommendedPointValid,
                InvensunA8BitMaskUtility.IsFlagSet(frame.LeftGaze.GazeBitMask, InvensunA8GazeValidityBit.GazePoint),
                InvensunA8BitMaskUtility.IsFlagSet(frame.RightGaze.GazeBitMask, InvensunA8GazeValidityBit.GazePoint),
                frame.LeftEyeExtra.Blink != 0,
                frame.RightEyeExtra.Blink != 0,
                frame.LeftEyeExtra.Openness,
                frame.RightEyeExtra.Openness,
                timestamp,
                binocularGaze);

            lock (sampleLock)
            {
                latestSample = sample;
                hasSample = true;
            }
        }

        private void FlushPendingLogs()
        {
            while (true)
            {
                (LogType logType, string message) pendingLog;

                lock (pendingLogs)
                {
                    if (pendingLogs.Count == 0)
                    {
                        break;
                    }

                    pendingLog = pendingLogs.Dequeue();
                }

                switch (pendingLog.logType)
                {
                    case LogType.Warning:
                        Debug.LogWarning(pendingLog.message);
                        break;

                    case LogType.Error:
                        Debug.LogError(pendingLog.message);
                        break;

                    default:
                        Debug.Log(pendingLog.message);
                        break;
                }
            }
        }

        private void EnqueueLog(LogType logType, string message)
        {
            lock (pendingLogs)
            {
                pendingLogs.Enqueue((logType, message));
            }
        }

        private void EnsureCallbackHandle()
        {
            if (!callbackHandle.IsAllocated)
            {
                callbackHandle = GCHandle.Alloc(this);
            }
        }

        private bool TryPrepareRuntimeLayout(out string runtimeConfigPath, out string statusMessage)
        {
            string sourceRootPath = Path.Combine(Application.streamingAssetsPath, "7ia8");

            return InvensunA8RuntimeLayoutUtility.TryStageRuntimeAssetsForNativeSdk(
                sourceRootPath,
                Application.persistentDataPath,
                ResolveNativeSdkWorkingDirectory(),
                out runtimeConfigPath,
                out _,
                out statusMessage);
        }

        private static string ResolveRuntimeRootPath()
        {
            return InvensunA8RuntimeLayoutUtility.BuildRuntimeRootPath(Application.persistentDataPath);
        }

        private static string ResolveVendorStyleRuntimeRootPath()
        {
            return InvensunA8RuntimeLayoutUtility.BuildVendorStyleRuntimeRootPath(ResolveNativeSdkWorkingDirectory());
        }

        private static string ResolveNativeSdkWorkingDirectory()
        {
            return InvensunA8RuntimeLayoutUtility.ResolveNativeSdkWorkingDirectory(
                Application.dataPath,
                Environment.CurrentDirectory);
        }

        private static IEnumerable<string> ResolveCoefficientCandidatePaths()
        {
            if (InvensunA8CalibrationPersistenceUtility.TryResolveAcceptedCoefficientPath(
                    Application.persistentDataPath,
                    out string acceptedCoefficientPath,
                    out _))
            {
                yield return acceptedCoefficientPath;
            }

            string demoStyleCoefficientPath = Path.Combine(Application.dataPath, "coefficient");
            if (File.Exists(demoStyleCoefficientPath))
            {
                yield return demoStyleCoefficientPath;
            }

            yield return InvensunA8RuntimeLayoutUtility.BuildRuntimeCoefficientPath(ResolveVendorStyleRuntimeRootPath());
            yield return InvensunA8RuntimeLayoutUtility.BuildRuntimeCoefficientPath(ResolveRuntimeRootPath());
        }

        private static InvensunA8Coefficient LoadCoefficient(out string coefficientSource)
        {
            var rejectedSources = new List<string>();

            foreach (string coefficientPath in ResolveCoefficientCandidatePaths())
            {
                if (InvensunA8CalibrationPersistenceUtility.TryReadCoefficient(
                        coefficientPath,
                        out byte[] coefficientBuffer,
                        out string failureReason))
                {
                    coefficientSource = $"{coefficientPath} ({coefficientBuffer.Length} bytes)";
                    return new InvensunA8Coefficient
                    {
                        Buffer = coefficientBuffer
                    };
                }

                rejectedSources.Add(failureReason);
            }

            coefficientSource = rejectedSources.Count > 0
                ? "null coefficient buffer. Rejected sources: " + string.Join(" | ", rejectedSources)
                : "null coefficient buffer; no coefficient candidates were found.";
            return new InvensunA8Coefficient();
        }

        private static void OnImageCallback(int eye, IntPtr image, int size, int width, int height, long timestamp, IntPtr context)
        {
        }

        private static void OnGazeCallback(ref InvensunA8EyeDataFrame eyes, IntPtr context)
        {
            if (!TryResolveRuntime(context, out var runtime))
            {
                return;
            }

            try
            {
                runtime.HandleGazeCallback(eyes);
            }
            catch (Exception exception)
            {
                runtime.EnqueueLog(LogType.Error, $"7Invensun gaze callback failed: {exception.Message}");
            }
        }

        private static bool TryResolveRuntime(IntPtr context, out InvensunA8DeviceRuntime runtime)
        {
            runtime = null;

            if (context == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var handle = GCHandle.FromIntPtr(context);
                runtime = handle.Target as InvensunA8DeviceRuntime;
                return runtime != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
