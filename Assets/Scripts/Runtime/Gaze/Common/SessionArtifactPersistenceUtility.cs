using System.IO;

namespace ProjectGaze.Gaze
{
    internal static class SessionArtifactPersistenceUtility
    {
        public static void WriteJsonCsvArtifacts(
            string rootPath,
            string json,
            string csv,
            string sessionJsonPath,
            string sessionCsvPath,
            string latestJsonPath,
            string latestCsvPath)
        {
            Directory.CreateDirectory(rootPath);
            File.WriteAllText(sessionJsonPath, json);
            File.WriteAllText(sessionCsvPath, csv);
            File.WriteAllText(latestJsonPath, json);
            File.WriteAllText(latestCsvPath, csv);
        }
    }
}
