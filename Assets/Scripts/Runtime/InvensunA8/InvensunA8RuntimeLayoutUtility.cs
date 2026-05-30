using System;
using System.IO;

namespace ProjectGaze.Gaze.Providers
{
    public static class InvensunA8RuntimeLayoutUtility
    {
        private const string VendorRuntimeDirectoryName = "7ia8";

        public static string BuildRuntimeRootPath(string persistentDataPath)
        {
            return Path.Combine(persistentDataPath ?? string.Empty, "7ia8-runtime");
        }

        public static string BuildVendorStyleRuntimeRootPath(string currentDirectory)
        {
            return Path.Combine(currentDirectory ?? string.Empty, VendorRuntimeDirectoryName);
        }

        public static string BuildRuntimeConfigPath(string runtimeRootPath)
        {
            return Path.Combine(runtimeRootPath ?? string.Empty, "config");
        }

        public static string BuildRuntimeCoefficientPath(string runtimeRootPath)
        {
            return Path.Combine(runtimeRootPath ?? string.Empty, "coefficient.dat");
        }

        public static string BuildCalibrationMetadataPath(string runtimeRootPath)
        {
            return Path.Combine(runtimeRootPath ?? string.Empty, "calibration-state.json");
        }

        public static string BuildCalibrationPersistenceRootPath(string persistentDataPath)
        {
            return Path.Combine(persistentDataPath ?? string.Empty, "7ia8-calibration");
        }

        public static string ResolveNativeSdkWorkingDirectory(string applicationDataPath, string currentDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(applicationDataPath))
            {
                try
                {
                    DirectoryInfo dataPathParent = Directory.GetParent(applicationDataPath);
                    if (dataPathParent != null)
                    {
                        return dataPathParent.FullName;
                    }
                }
                catch
                {
                }
            }

            return string.IsNullOrWhiteSpace(currentDirectory)
                ? Environment.CurrentDirectory
                : currentDirectory;
        }

        public static string BuildCalibrationCoefficientPath(string calibrationRootPath)
        {
            return Path.Combine(calibrationRootPath ?? string.Empty, "coefficient.dat");
        }

        public static bool TryStageRuntimeAssets(
            string sourceRootPath,
            string runtimeRootPath,
            out string runtimeConfigPath,
            out string statusMessage)
        {
            runtimeConfigPath = BuildRuntimeConfigPath(runtimeRootPath);

            if (string.IsNullOrWhiteSpace(runtimeRootPath))
            {
                statusMessage = "7Invensun runtime staging failed: runtime root path is empty.";
                return false;
            }

            if (!Directory.Exists(sourceRootPath))
            {
                statusMessage = $"7Invensun runtime template path is missing: {sourceRootPath}";
                return false;
            }

            try
            {
                if (AreSamePath(sourceRootPath, runtimeRootPath))
                {
                    statusMessage = $"7Invensun runtime template already available at {runtimeRootPath}";
                    return Directory.Exists(runtimeConfigPath);
                }

                if (Directory.Exists(runtimeRootPath))
                {
                    Directory.Delete(runtimeRootPath, recursive: true);
                }

                CopyTemplateDirectory(sourceRootPath, runtimeRootPath);
                statusMessage = $"7Invensun runtime assets staged at {runtimeRootPath}";
                return true;
            }
            catch (Exception exception)
            {
                statusMessage = $"7Invensun runtime staging failed: {exception.Message}";
                return false;
            }
        }

        public static bool TryStageRuntimeAssetsForNativeSdk(
            string sourceRootPath,
            string persistentDataPath,
            out string runtimeConfigPath,
            out string runtimeRootPath,
            out string statusMessage)
        {
            return TryStageRuntimeAssetsForNativeSdk(
                sourceRootPath,
                persistentDataPath,
                Environment.CurrentDirectory,
                out runtimeConfigPath,
                out runtimeRootPath,
                out statusMessage);
        }

        public static bool TryStageRuntimeAssetsForNativeSdk(
            string sourceRootPath,
            string persistentDataPath,
            string nativeWorkingDirectory,
            out string runtimeConfigPath,
            out string runtimeRootPath,
            out string statusMessage)
        {
            string vendorStyleRootPath = BuildVendorStyleRuntimeRootPath(nativeWorkingDirectory);
            string persistentRootPath = BuildRuntimeRootPath(persistentDataPath);

            if (TryStageRuntimeAssets(
                    sourceRootPath,
                    vendorStyleRootPath,
                    out runtimeConfigPath,
                    out string vendorLayoutStatus))
            {
                runtimeRootPath = vendorStyleRootPath;
                statusMessage = $"{vendorLayoutStatus}. Using vendor-style native SDK layout.";
                return true;
            }

            string failedStatus = vendorLayoutStatus;
            if (!AreSamePath(vendorStyleRootPath, persistentRootPath))
            {
                if (TryStageRuntimeAssets(
                        sourceRootPath,
                        persistentRootPath,
                        out runtimeConfigPath,
                        out string persistentLayoutStatus))
                {
                    runtimeRootPath = persistentRootPath;
                    statusMessage = $"{vendorLayoutStatus} Falling back to persistent runtime layout. {persistentLayoutStatus}";
                    return true;
                }

                failedStatus = $"{vendorLayoutStatus} Persistent runtime fallback also failed. {persistentLayoutStatus}";
            }

            runtimeRootPath = persistentRootPath;
            runtimeConfigPath = BuildRuntimeConfigPath(persistentRootPath);
            statusMessage = failedStatus;
            return false;
        }

        public static bool ShouldCopyTemplateFile(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                   !filePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        public static bool ShouldResetRuntimeDirectory(string sourceDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(sourceDirectoryPath))
            {
                return false;
            }

            string directoryName = Path.GetFileName(sourceDirectoryPath);
            string parentDirectoryName = Path.GetFileName(Path.GetDirectoryName(sourceDirectoryPath) ?? string.Empty);

            return string.Equals(directoryName, "log", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(parentDirectoryName, "armeabi", StringComparison.OrdinalIgnoreCase);
        }

        private static void CopyTemplateDirectory(string sourceDirectoryPath, string destinationDirectoryPath)
        {
            Directory.CreateDirectory(destinationDirectoryPath);

            foreach (string directoryPath in Directory.GetDirectories(sourceDirectoryPath))
            {
                string destinationChildPath = Path.Combine(destinationDirectoryPath, Path.GetFileName(directoryPath));

                if (ShouldResetRuntimeDirectory(directoryPath))
                {
                    Directory.CreateDirectory(destinationChildPath);
                    continue;
                }

                CopyTemplateDirectory(directoryPath, destinationChildPath);
            }

            foreach (string filePath in Directory.GetFiles(sourceDirectoryPath))
            {
                if (!ShouldCopyTemplateFile(filePath))
                {
                    continue;
                }

                string destinationFilePath = Path.Combine(destinationDirectoryPath, Path.GetFileName(filePath));
                File.Copy(filePath, destinationFilePath, overwrite: true);
            }
        }

        private static bool AreSamePath(string firstPath, string secondPath)
        {
            if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
            {
                return false;
            }

            string firstFullPath = Path.GetFullPath(firstPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string secondFullPath = Path.GetFullPath(secondPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(firstFullPath, secondFullPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class InvensunA8NativeErrorMessages
    {
        public static string DescribeRuntimeStartFailure(int errorCode, string configPath)
        {
            string diagnosis = errorCode == -2112
                ? "The SDK reached camera startup but rejected the camera PID. Check that the 7Invensun A8 device is connected, its driver is installed, and no vendor demo or camera app is holding the device."
                : "Check the A8 device connection, driver, SDK files, and config path.";

            return $"7Invensun runtime start failed with code {errorCode}. {diagnosis} Config path: {configPath}";
        }
    }
}
