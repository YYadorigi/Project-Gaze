using System.Globalization;
using System.Text;

namespace ProjectGaze.Gaze
{
    internal static class CsvPersistenceUtility
    {
        public static void AppendCsv(StringBuilder builder, string value, bool endOfLine = false)
        {
            builder.Append(EscapeCsv(value));
            builder.Append(endOfLine ? '\n' : ',');
        }

        public static void AppendCsv(StringBuilder builder, int value, bool endOfLine = false)
        {
            AppendCsv(builder, value.ToString(CultureInfo.InvariantCulture), endOfLine);
        }

        public static void AppendCsv(StringBuilder builder, long value, bool endOfLine = false)
        {
            AppendCsv(builder, value.ToString(CultureInfo.InvariantCulture), endOfLine);
        }

        public static void AppendCsv(StringBuilder builder, float value, bool endOfLine = false)
        {
            AppendCsv(builder, value.ToString("0.######", CultureInfo.InvariantCulture), endOfLine);
        }

        public static void AppendCsv(StringBuilder builder, bool value, bool endOfLine = false)
        {
            AppendCsv(builder, value ? "true" : "false", endOfLine);
        }

        public static string SanitizeFileToken(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            StringBuilder builder = new(value.Length);
            foreach (char character in value)
            {
                builder.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '-');
            }

            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            bool requiresQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            if (!requiresQuoting)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
