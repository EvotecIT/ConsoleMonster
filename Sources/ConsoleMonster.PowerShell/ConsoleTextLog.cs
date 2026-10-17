using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// The log file side of Write-ConsoleText: Text (<c>[time] [Level] [Category] message</c>, the Write-Color layout),
    /// Csv (a header row, then Time, Level, Category, Message with a configurable separator) or JsonLines (one object per
    /// line). Writes append under a file lock and are retried like Write-Color does, with a warning per failed attempt.
    /// </summary>
    internal static class ConsoleTextLog {
        /// <summary>One log record.</summary>
        internal sealed class Record {
            public Record(DateTime time, string message, string? level, string? category) {
                Time = time;
                Message = message;
                Level = level;
                Category = category;
            }

            public DateTime Time { get; }

            public string Message { get; }

            public string? Level { get; }

            public string? Category { get; }
        }

        /// <summary>Appends a record; returns false (after warnings) when every attempt failed.</summary>
        internal static bool Write(PSCmdlet cmdlet, string path, Record record, string format, string separator, string encodingName, int retries, bool logTime, string timeFormat) {
            Encoding encoding = ResolveEncoding(encodingName);
            int attempts = Math.Max(1, retries);
            for (int attempt = 1; attempt <= attempts; attempt++) {
                try {
                    bool isNew = !File.Exists(path) || new FileInfo(path).Length == 0;
                    string text = Format(record, format, separator, logTime, timeFormat, isNew);
                    using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)) {
                        byte[] preamble = isNew ? encoding.GetPreamble() : Array.Empty<byte>();
                        byte[] bytes = encoding.GetBytes(text);
                        stream.Write(preamble, 0, preamble.Length);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    return true;
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is System.Security.SecurityException) {
                    if (attempt == attempts) {
                        cmdlet.WriteWarning("Write-ConsoleText - Couldn't write to log file " + ex.Message + ". Tried (" + attempt.ToString(CultureInfo.InvariantCulture) + "/" + attempts.ToString(CultureInfo.InvariantCulture) + ")");
                        return false;
                    }
                    cmdlet.WriteWarning("Write-ConsoleText - Couldn't write to log file " + ex.Message + ". Retrying... (" + attempt.ToString(CultureInfo.InvariantCulture) + "/" + attempts.ToString(CultureInfo.InvariantCulture) + ")");
                    Thread.Sleep(100 * attempt);
                }
            }
            return false;
        }

        /// <summary>The text appended for a record (including the CSV header for a new file).</summary>
        internal static string Format(Record record, string format, string separator, bool logTime, string timeFormat, bool isNewFile) {
            string time = record.Time.ToString(timeFormat, CultureInfo.InvariantCulture);
            switch ((format ?? "Text").ToLowerInvariant()) {
                case "csv":
                    string sep = string.IsNullOrEmpty(separator) ? "," : separator;
                    StringBuilder csv = new StringBuilder();
                    if (isNewFile) {
                        csv.Append(string.Join(sep, new[] { "Time", "Level", "Category", "Message" })).Append(Environment.NewLine);
                    }
                    csv.Append(Csv(logTime ? time : string.Empty, sep)).Append(sep)
                        .Append(Csv(record.Level ?? string.Empty, sep)).Append(sep)
                        .Append(Csv(record.Category ?? string.Empty, sep)).Append(sep)
                        .Append(Csv(record.Message, sep)).Append(Environment.NewLine);
                    return csv.ToString();
                case "jsonlines":
                case "json":
                    StringBuilder json = new StringBuilder("{");
                    if (logTime) {
                        json.Append("\"time\":").Append(Json(time)).Append(',');
                    }
                    if (record.Level != null) {
                        json.Append("\"level\":").Append(Json(record.Level)).Append(',');
                    }
                    if (record.Category != null) {
                        json.Append("\"category\":").Append(Json(record.Category)).Append(',');
                    }
                    json.Append("\"message\":").Append(Json(record.Message)).Append('}').Append(Environment.NewLine);
                    return json.ToString();
                default:
                    StringBuilder line = new StringBuilder();
                    if (logTime) {
                        line.Append('[').Append(time).Append("] ");
                    }
                    if (record.Level != null) {
                        line.Append('[').Append(record.Level).Append("] ");
                    }
                    if (record.Category != null) {
                        line.Append('[').Append(record.Category).Append("] ");
                    }
                    // Following lines of a split message align under its first line (the Write-Color issue #7 layout).
                    string indent = new string(' ', line.Length);
                    string[] messageLines = record.Message.Replace("\r\n", "\n").Split('\n');
                    line.Append(messageLines[0]);
                    for (int i = 1; i < messageLines.Length; i++) {
                        line.Append(Environment.NewLine).Append(indent).Append(messageLines[i]);
                    }
                    return line.Append(Environment.NewLine).ToString();
            }
        }

        /// <summary>
        /// The log encoding. Write-Color's names are accepted; utf8 (the default) writes no byte order mark, utf8BOM does;
        /// unicode, string and unknown are UTF-16 LE (Out-File's "Unicode"); utf7 (obsolete) writes UTF-8.
        /// </summary>
        internal static Encoding ResolveEncoding(string? name) {
            switch ((name ?? "utf8").ToLowerInvariant()) {
                case "unicode":
                case "string":
                case "unknown":
                    return new UnicodeEncoding(false, true);
                case "bigendianunicode":
                    return new UnicodeEncoding(true, true);
                case "utf32":
                    return new UTF32Encoding(false, true);
                case "ascii":
                    return new ASCIIEncoding();
                case "utf8bom":
                    return new UTF8Encoding(true);
                case "default":
                    return Encoding.Default;
                case "oem":
                    try {
                        return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                    } catch (Exception) {
                        return new UTF8Encoding(false);
                    }
                default:
                    return new UTF8Encoding(false);
            }
        }

        private static string Csv(string value, string separator) {
            bool quote = value.Contains(separator) || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0
                || (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])));
            return quote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        private static string Json(string value) {
            StringBuilder sb = new StringBuilder("\"");
            foreach (char c in value) {
                switch (c) {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20) {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        } else {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
