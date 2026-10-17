using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ConsoleMonster.Output {
    /// <summary>The layout of a <see cref="ConsoleLogFile"/>.</summary>
    public enum ConsoleLogFormat {
        /// <summary><c>[time] [Level] [Category] message</c>, following lines indented under the first (the Write-Color layout).</summary>
        Text,

        /// <summary>A header row, then Time, Level, Category, Message with a configurable separator.</summary>
        Csv,

        /// <summary>One JSON object per line (time, level, category, message).</summary>
        JsonLines
    }

    /// <summary>One record of a <see cref="ConsoleLogFile"/>.</summary>
    public sealed class ConsoleLogRecord {
        /// <summary>Creates a record.</summary>
        /// <param name="time">The time.</param>
        /// <param name="message">The message (may contain line breaks).</param>
        /// <param name="level">A level (Info, Warning, Error...), or null.</param>
        /// <param name="category">A category, or null.</param>
        public ConsoleLogRecord(DateTime time, string message, string? level = null, string? category = null) {
            Time = time;
            Message = message ?? string.Empty;
            Level = level;
            Category = category;
        }

        /// <summary>The time.</summary>
        public DateTime Time { get; }

        /// <summary>The message.</summary>
        public string Message { get; }

        /// <summary>The level, or null.</summary>
        public string? Level { get; }

        /// <summary>The category, or null.</summary>
        public string? Category { get; }
    }

    /// <summary>
    /// Appends console text to a log file, as Write-Color's log option does: Text, Csv or JsonLines; appends under a file
    /// lock and retries with a growing pause, reporting each failed attempt to a warning callback.
    /// </summary>
    public sealed class ConsoleLogFile {
        /// <summary>Creates a log file writer.</summary>
        /// <param name="path">The file path.</param>
        public ConsoleLogFile(string path) {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>The file path.</summary>
        public string Path { get; }

        /// <summary>The layout (default Text).</summary>
        public ConsoleLogFormat Format { get; set; } = ConsoleLogFormat.Text;

        /// <summary>The CSV separator (default a comma).</summary>
        public string Separator { get; set; } = ",";

        /// <summary>The encoding (default UTF-8 without a byte order mark); see <see cref="ResolveEncoding"/>.</summary>
        public Encoding Encoding { get; set; } = new UTF8Encoding(false);

        /// <summary>Attempts before giving up (default 2).</summary>
        public int Retries { get; set; } = 2;

        /// <summary>Whether records start with the time (default true).</summary>
        public bool IncludeTime { get; set; } = true;

        /// <summary>The time format (default yyyy-MM-dd HH:mm:ss).</summary>
        public string TimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        /// <summary>Receives a message for each failed attempt (for example a PowerShell warning), or null.</summary>
        public Action<string>? Warning { get; set; }

        /// <summary>Appends a record; returns false (after warnings) when every attempt failed.</summary>
        /// <param name="record">The record.</param>
        /// <returns>Whether the record was written.</returns>
        public bool Append(ConsoleLogRecord record) {
            if (record == null) {
                throw new ArgumentNullException(nameof(record));
            }
            int attempts = Math.Max(1, Retries);
            for (int attempt = 1; attempt <= attempts; attempt++) {
                try {
                    bool isNew = !File.Exists(Path) || new FileInfo(Path).Length == 0;
                    string text = FormatRecord(record, isNew);
                    using (FileStream stream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.Read)) {
                        byte[] preamble = isNew ? Encoding.GetPreamble() : new byte[0];
                        byte[] bytes = Encoding.GetBytes(text);
                        stream.Write(preamble, 0, preamble.Length);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    return true;
                } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is System.Security.SecurityException) {
                    string tried = attempt.ToString(CultureInfo.InvariantCulture) + "/" + attempts.ToString(CultureInfo.InvariantCulture);
                    if (attempt == attempts) {
                        Warning?.Invoke("Couldn't write to log file " + ex.Message + ". Tried (" + tried + ")");
                        return false;
                    }
                    Warning?.Invoke("Couldn't write to log file " + ex.Message + ". Retrying... (" + tried + ")");
                    Thread.Sleep(100 * attempt);
                }
            }
            return false;
        }

        /// <summary>The text appended for a record (including the CSV header for a new file).</summary>
        /// <param name="record">The record.</param>
        /// <param name="isNewFile">Whether the file is new or empty (CSV writes its header then).</param>
        /// <returns>The text.</returns>
        public string FormatRecord(ConsoleLogRecord record, bool isNewFile) {
            string time = record.Time.ToString(TimeFormat, CultureInfo.InvariantCulture);
            switch (Format) {
                case ConsoleLogFormat.Csv:
                    string sep = string.IsNullOrEmpty(Separator) ? "," : Separator;
                    StringBuilder csv = new StringBuilder();
                    if (isNewFile) {
                        csv.Append(string.Join(sep, new[] { "Time", "Level", "Category", "Message" })).Append(Environment.NewLine);
                    }
                    csv.Append(Csv(IncludeTime ? time : string.Empty, sep)).Append(sep)
                        .Append(Csv(record.Level ?? string.Empty, sep)).Append(sep)
                        .Append(Csv(record.Category ?? string.Empty, sep)).Append(sep)
                        .Append(Csv(record.Message, sep)).Append(Environment.NewLine);
                    return csv.ToString();
                case ConsoleLogFormat.JsonLines:
                    StringBuilder json = new StringBuilder("{");
                    if (IncludeTime) {
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
                    if (IncludeTime) {
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
        /// An encoding by name. Write-Color's names are accepted; utf8 (the default) writes no byte order mark, utf8BOM does;
        /// unicode, string and unknown are UTF-16 LE (Out-File's "Unicode"); utf7 (obsolete) writes UTF-8.
        /// </summary>
        /// <param name="name">The name, or null for utf8.</param>
        /// <returns>The encoding.</returns>
        public static Encoding ResolveEncoding(string? name) {
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
                    } catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException) {
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
