using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using SiteFrames.Frames;

namespace SiteFrames.Capture {
    /// <summary>Fail-closed comparison of independently produced receipts; completion never implies full-suite coverage.</summary>
    internal static partial class CaptureComparison {
        private sealed record Capture(string Path, byte[] Bytes, int Width, int Height, string Category, string? Screen,
            List<CellBuffer> Frames, string? Source, string[] ScreenHashes, string ReadingHolds,
            CaptureClasses.LiveMeasured? Measured, double DurationSeconds);

        internal static int Run(string left, string right, string? report = null) {
            var failures = new JsonArray();
            var allowed = new JsonArray();
            var coverage = new JsonObject { ["scope"] = "produced-only", ["fullSuiteVerified"] = false };
            var a = Read(left, "left", failures, coverage, out string producerA);
            var b = Read(right, "right", failures, coverage, out string producerB);
            coverage["fullSuiteVerified"] = coverage["left"]?["allSetsCompleted"]?.GetValue<bool>() == true
                && coverage["right"]?["allSetsCompleted"]?.GetValue<bool>() == true;
            bool samePlatform = producerA == producerB;
            bool validReceipts = failures.Count == 0;
            int compared = 0;
            foreach (string file in a.Keys.Union(b.Keys).OrderBy(f => f, StringComparer.Ordinal)) {
                bool hasA = a.TryGetValue(file, out Capture? ca), hasB = b.TryGetValue(file, out Capture? cb);
                if (!hasA || !hasB) {
                    string present = hasA ? producerA : producerB, missing = hasA ? producerB : producerA;
                    if (!samePlatform && CapturePlatforms.IsPs5(file) && present == "windows" && missing is "macos" or "linux") {
                        allowed.Add(new JsonObject { ["file"] = file, ["reason"] = "Windows-only ps5 production; retained files still require render validation",
                            ["leftPath"] = ca?.Path, ["rightPath"] = cb?.Path, ["changedRows"] = new JsonArray() });
                    } else { failures.Add(file + ": missing capture manifest entry in one run"); }
                    continue;
                }
                Capture x = ca!, y = cb!;
                compared++;
                if (x.Width != y.Width || x.Height != y.Height) { failures.Add(file + ": terminal dimensions differ"); }
                if (x.Source != y.Source) { failures.Add(file + ": capture source fingerprints differ"); }
                if (x.Screen != y.Screen) { failures.Add(file + ": declared terminal defaults differ or are missing in one run"); }
                if (x.Measured != y.Measured) { failures.Add(file + ": capture class, reason or bounds differ"); }
                bool readingHoldsEqual = x.ReadingHolds == y.ReadingHolds;
                if (!readingHoldsEqual) { failures.Add(file + ": scripted reading holds differ"); }
                bool bytesEqual = x.Bytes.AsSpan().SequenceEqual(y.Bytes);
                if (validReceipts && x.Measured != null && x.Measured == y.Measured && x.Source == y.Source
                    && x.Screen == y.Screen && x.Width == y.Width && x.Height == y.Height && readingHoldsEqual) {
                    allowed.Add(new JsonObject {
                        ["file"] = file, ["class"] = "live-measured", ["reason"] = x.Measured.Reason,
                        ["bytesEqual"] = bytesEqual, ["leftPath"] = x.Path, ["rightPath"] = y.Path,
                        ["leftSha256"] = CaptureRun.Hash(x.Bytes), ["rightSha256"] = CaptureRun.Hash(y.Bytes),
                        ["leftFrames"] = x.Frames.Count, ["rightFrames"] = y.Frames.Count,
                        ["leftDurationSeconds"] = x.DurationSeconds, ["rightDurationSeconds"] = y.DurationSeconds,
                        ["policy"] = x.Measured.Json()
                    });
                    continue;
                }
                int changedRowCount = 0;
                long changedCellCount = 0;
                JsonArray rows = bytesEqual && x.Width == y.Width && x.Height == y.Height ? new JsonArray()
                    : ChangedRows(x, y, out changedRowCount, out changedCellCount);
                JsonArray screens = ChangedScreenHashes(x, y);
                if (samePlatform && !bytesEqual) { failures.Add(file + ": repeated capture is not byte-identical (including measurements)"); }
                if (x.Frames.Count != y.Frames.Count) { failures.Add(file + ": frame counts differ (" + x.Frames.Count + " / " + y.Frames.Count + ")"); }
                if (rows.Count > 0 || screens.Count > 0) {
                    // Only the three tagged native profiles have a parsed value policy; every other category stays strict.
                    if (!samePlatform && validReceipts && ProfileDifferences(file, x, y, out JsonArray values)) {
                        allowed.Add(new JsonObject { ["file"] = file, ["category"] = x.Category,
                            ["reason"] = "documented native profile values; stable fields, layout and styles checked in every complete frame",
                            ["leftPath"] = x.Path, ["rightPath"] = y.Path, ["valueDifferences"] = values,
                            ["leftSha256"] = CaptureRun.Hash(x.Bytes), ["rightSha256"] = CaptureRun.Hash(y.Bytes),
                            ["changedRows"] = rows, ["changedRowCount"] = changedRowCount, ["changedCellCount"] = changedCellCount,
                            ["omittedChangedRows"] = changedRowCount - rows.Count, ["changedScreenHashes"] = screens });
                    } else {
                        failures.Add(new JsonObject { ["file"] = file, ["category"] = x.Category,
                            ["reason"] = "capture cells, styles or screen hashes differ", ["changedRows"] = rows,
                            ["changedRowCount"] = changedRowCount, ["changedCellCount"] = changedCellCount,
                            ["omittedChangedRows"] = changedRowCount - rows.Count, ["changedScreenHashes"] = screens });
                    }
                } else if (!samePlatform && !bytesEqual && readingHoldsEqual && x.Source == y.Source && x.Screen == y.Screen && x.Width == y.Width && x.Height == y.Height && x.Frames.Count == y.Frames.Count) {
                    allowed.Add(new JsonObject { ["file"] = file, ["category"] = x.Category,
                        ["reason"] = "transport encoding/timing, declared terminal defaults or invisible space foreground only; all visible cells, backgrounds, decorations, links and screen hashes match",
                        ["leftSha256"] = CaptureRun.Hash(x.Bytes), ["rightSha256"] = CaptureRun.Hash(y.Bytes), ["changedRows"] = rows });
                }
            }
            if (compared == 0) { failures.Add("No common valid captures were compared."); }
            var document = new JsonObject { ["compared"] = compared, ["samePlatform"] = samePlatform,
                ["coverage"] = coverage, ["allowed"] = allowed, ["failures"] = failures };
            string json = document.ToJsonString(new JsonSerializerOptions { WriteIndented = true,
                TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() });
            if (report != null) {
                try { File.WriteAllText(report, json + "\n"); }
                catch (Exception ex) when (InputError(ex)) { Console.Error.WriteLine("Cannot write comparison report: " + ex.Message); return 1; }
            }
            Console.WriteLine(report == null ? json : compared + " captures compared; " + failures.Count + " failures; " + allowed.Count + " allowed differences. Report: " + Path.GetFullPath(report));
            return failures.Count == 0 ? 0 : 1;
        }

        private static Dictionary<string, Capture> Read(string root, string side, JsonArray failures, JsonObject coverage, out string producer) {
            var result = new Dictionary<string, Capture>(StringComparer.Ordinal);
            producer = string.Empty;
            try {
                JsonObject manifest = JsonNode.Parse(File.ReadAllText(Contained(root, "run.json")))!.AsObject();
                if (manifest["version"]?.GetValue<int>() != 1) { throw new InvalidDataException("Unsupported receipt version."); }
                producer = manifest["producer"]!.GetValue<string>();
                if (!CapturePlatforms.All.Contains(producer, StringComparer.Ordinal)) { throw new InvalidDataException("Unsupported producer platform."); }
                bool? complete = manifest["complete"]?.GetValue<bool>();
                JsonObject files = manifest["files"]!.AsObject();
                Dictionary<string, CaptureClasses.LiveMeasured> classes = CaptureClasses.Read(root);
                coverage[side] = new JsonObject { ["producer"] = producer, ["complete"] = complete,
                    ["allSetsCompleted"] = manifest["allSetsCompleted"]?.GetValue<bool>() == true,
                    ["requestedSet"] = manifest["requestedSet"]?.DeepClone(), ["filters"] = manifest["filters"]?.DeepClone(),
                    ["listedFiles"] = files.Count, ["paths"] = new JsonArray(files.Select(f => (JsonNode?)JsonValue.Create(f.Key)).ToArray()) };
                if (complete != true) { failures.Add(side + ": capture invocation incomplete or completion evidence missing"); }
                if (manifest["scope"] != null && manifest["scope"]!.GetValue<string>() != "produced-only") { throw new InvalidDataException("Unsupported coverage scope."); }
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in files) {
                    try {
                        string logical = CapturePlatforms.LogicalName(entry.Key);
                        if (!names.Add(logical)) { throw new InvalidDataException("Duplicate logical capture name: " + logical); }
                        JsonObject metadata = entry.Value!.AsObject();
                        if (metadata["error"] != null) { throw new InvalidDataException("Producer recorded failure: " + metadata["error"]!.GetValue<string>()); }
                        string platform = metadata["platform"]!.GetValue<string>();
                        if (platform != producer || (CapturePlatforms.IsPs5(entry.Key) && producer != "windows") ||
                            (CapturePlatforms.Tag(entry.Key) is string tag && tag != producer)) {
                            throw new InvalidDataException("Capture platform/tag does not match producer.");
                        }
                        string category = metadata["category"]!.GetValue<string>();
                        if (category != CaptureRun.Category(logical)) { throw new InvalidDataException("Capture category differs from its declared policy."); }
                        int width = metadata["width"]!.GetValue<int>(), height = metadata["height"]!.GetValue<int>();
                        // Bound allocation before any parser constructs a screen from an external receipt.
                        if (width <= 0 || height <= 0 || width > 4096 || height > 4096 || (long)width * height > 4_194_304) {
                            throw new InvalidDataException("Invalid or unrecorded terminal geometry; regenerate this set.");
                        }
                        string extension = Path.GetExtension(entry.Key);
                        if (extension is not (".ansi" or ".cast" or ".txt")) { throw new InvalidDataException("Unsupported capture type: " + extension); }
                        byte[] bytes = File.ReadAllBytes(Contained(root, entry.Key));
                        string hash = metadata["sha256"]!.GetValue<string>();
                        if (!Hex(hash, 64) || !string.Equals(CaptureRun.Hash(bytes), hash, StringComparison.OrdinalIgnoreCase)) { throw new InvalidDataException("File does not match its producer receipt."); }
                        string text = new UTF8Encoding(false, true).GetString(bytes);
                        if (text.Length > 0 && text[0] == '\uFEFF') { text = text[1..]; }
                        string? source = null;
                        string[] hashes = Array.Empty<string>();
                        double duration = 0;
                        List<CellBuffer> frames = extension == ".txt" ? Descriptions(text, width, height, out source, out hashes) : Frames(text, extension, width, height, out source, out duration);
                        classes.TryGetValue(logical, out var measured);
                        CaptureClasses.LiveMeasured? recorded = metadata["comparison"] is JsonObject comparison ? CaptureClasses.Parse(comparison) : null;
                        if (recorded != measured) { throw new InvalidDataException("Receipt capture class must match its declared metadata, reason and bounds."); }
                        if (measured != null) {
                            if (extension != ".cast" || source == null) { throw new InvalidDataException("Live-measured output needs a stamped cast."); }
                            if (frames.Count < measured.MinFrames || frames.Count > measured.MaxFrames
                                || duration < measured.MinDurationSeconds || duration > measured.MaxDurationSeconds) {
                                throw new InvalidDataException("Live-measured frame count or duration is outside its declared bounds: " + frames.Count + " frames, " + duration + " seconds.");
                            }
                        }
                        string? screen = metadata["screen"]?.GetValue<string>();
                        if (screen != null && screen is not ("dark" or "light")) { throw new InvalidDataException("Unsupported terminal defaults."); }
                        if (extension == ".ansi") {
                            JsonObject sources = JsonNode.Parse(File.ReadAllText(Contained(root, "sources.json")))!.AsObject();
                            source = sources[entry.Key]?.GetValue<string>();
                            if (source == null || !Hex(source, 16)) { throw new InvalidDataException("Missing or invalid ANSI source fingerprint."); }
                        }
                        for (int frame = 0; frame < frames.Count; frame++) {
                            foreach (string issue in Quality(frames[frame])) { failures.Add(side + ": " + entry.Key + ": frame " + frame + ": " + issue); }
                        }
                        if (screen != null) {
                            Screen defaults = screen == "dark" ? SiteFrames.Frames.Screen.Dark : SiteFrames.Frames.Screen.Light;
                            var resolved = new List<CellBuffer>();
                            foreach (CellBuffer frame in frames) {
                                CellBuffer next = FrameDefaults.Resolve(frame, defaults);
                                if (resolved.Count == 0 || !SameFrame(resolved[^1], next)) { resolved.Add(next); }
                            }
                            frames = resolved;
                        }
                        result.TryAdd(logical, new Capture(entry.Key, bytes, width, height, category, screen, frames, source, hashes, ReadingHolds(text, extension), measured, duration));
                    } catch (Exception ex) when (InputError(ex)) { failures.Add(side + ": " + entry.Key + ": " + ex.Message); }
                }
            } catch (Exception ex) when (InputError(ex)) { failures.Add(side + ": invalid run.json: " + ex.Message); }
            return result;
        }

        private static bool InputError(Exception ex) => ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException
            or ArgumentException or NullReferenceException or FormatException or OverflowException or KeyNotFoundException or IndexOutOfRangeException;

        private static bool Hex(string value, int length) => value.Length == length && value.All(Uri.IsHexDigit);

        private static string Contained(string root, string relative) {
            if (Path.IsPathRooted(relative) || relative.Contains('\\') || relative.Contains(':') || relative.Split('/').Any(p => p is "" or "." or "..")) {
                throw new InvalidDataException("Capture path must be a canonical relative path.");
            }
            string directory = Path.GetFullPath(root), path = directory;
            foreach (string part in relative.Split('/')) {
                path = Path.Combine(path, part);
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) { throw new InvalidDataException("Capture path traverses a symbolic link."); }
            }
            return path;
        }

        /// <summary>Descriptions contain a source fingerprint and form-feed-separated styled-screen hashes, never VT.</summary>
        private static List<CellBuffer> Descriptions(string text, int width, int height, out string? source, out string[] hashes) {
            string[] parts = text.Replace("\r\n", "\n").Split('\f');
            if (parts.Length < 2 || !parts[0].StartsWith("source ", StringComparison.Ordinal) || !Hex(parts[0][7..], 16)) {
                throw new InvalidDataException("Invalid styled-screen description source header.");
            }
            source = parts[0];
            hashes = new string[parts.Length - 1];
            var frames = new List<CellBuffer>();
            for (int index = 1; index < parts.Length; index++) {
                int newline = parts[index].IndexOf('\n');
                if (newline != 16 || !Hex(parts[index][..newline], 16)) { throw new InvalidDataException("Invalid styled-screen hash."); }
                hashes[index - 1] = parts[index][..newline];
                string[] rows = parts[index][(newline + 1)..].Split('\n');
                if (rows.Length > height) { throw new InvalidDataException("Screen description exceeds recorded height."); }
                var frame = new CellBuffer(width, rows.Length);
                for (int row = 0; row < rows.Length; row++) {
                    if (rows[row].Any(char.IsControl) || DisplayWidth.Measure(rows[row]) > width) { throw new InvalidDataException("Screen description has controls or exceeds recorded width."); }
                    frame.Write(0, row, rows[row], Style.Plain);
                }
                frames.Add(frame);
            }
            return frames;
        }

        private static List<CellBuffer> Frames(string text, string extension, int width, int height, out string? source, out double duration) {
            source = null;
            duration = 0;
            if (extension == ".ansi") {
                var ansi = new ConsoleMonster.Sessions.VtScreen(width, height);
                var frames = new List<CellBuffer>();
                ansi.SynchronizedFrameCompleted = () => {
                    CellBuffer frame = ansi.Snapshot();
                    if (frames.Count == 0 || !SameFrame(frames[^1], frame)) { frames.Add(frame); }
                };
                ansi.Feed(text);
                if (ansi.HasIncompleteSequence) { throw new InvalidDataException("ANSI capture ended during an incomplete VT sequence."); }
                if (ansi.SynchronizedOutput) { throw new InvalidDataException("ANSI capture ended during an uncommitted synchronized frame."); }
                CellBuffer visible = ansi.Snapshot(), transcript = ansi.Transcript();
                if (frames.Count == 0 || !SameFrame(frames[^1], visible)) { frames.Add(visible); }
                if (!SameFrame(frames[^1], transcript)) { frames.Add(transcript); }
                return frames;
            }
            using var lines = new StringReader(text);
            JsonObject header = JsonNode.Parse(lines.ReadLine()!)!.AsObject();
            if (header["version"]?.GetValue<int>() != 2 || header["width"]?.GetValue<int>() != width || header["height"]?.GetValue<int>() != height) {
                throw new InvalidDataException("Cast header version/geometry does not match receipt.");
            }
            if (header["cm_source"] != null) {
                source = header["cm_source"]!.GetValue<string>();
                if (!Hex(source, 16)) { throw new InvalidDataException("Invalid cast source fingerprint."); }
            }
            var output = new StringBuilder();
            double previous = -1;
            string? line;
            while ((line = lines.ReadLine()) != null) {
                if (line.Length == 0) { continue; }
                JsonArray entry = JsonNode.Parse(line)!.AsArray();
                if (entry.Count != 3) { throw new InvalidDataException("Invalid cast event shape."); }
                double timestamp = entry[0]!.GetValue<double>();
                if (!double.IsFinite(timestamp) || timestamp < 0 || timestamp < previous) { throw new InvalidDataException("Invalid cast event time."); }
                previous = timestamp;
                string kind = entry[1]!.GetValue<string>(), data = entry[2]!.GetValue<string>();
                if (kind == "o") { output.Append(data); duration = timestamp; }
                else if (kind is not ("i" or "m")) { throw new InvalidDataException("Unsupported cast event type: " + kind); }
            }
            // Arrival times and packet boundaries are transport. Commit complete lines and synchronized frames in order.
            text = output.ToString();
            var screen = new ConsoleMonster.Sessions.VtScreen(width, height);
            var result = new List<CellBuffer>();
            const string commit = "\u001b[?2026l";
            int offset = 0;
            foreach (Rune rune in text.EnumerateRunes()) {
                // Keep Unicode scalar boundaries intact while feeding decoded cast output.
                string scalar = rune.ToString();
                screen.Feed(scalar);
                offset += scalar.Length;
                bool boundary = rune.Value == '\n' || (offset >= commit.Length && text.AsSpan(offset - commit.Length, commit.Length).SequenceEqual(commit.AsSpan()));
                if (boundary && !screen.SynchronizedOutput) {
                    CellBuffer frame = screen.Snapshot();
                    if (result.Count == 0 || !SameFrame(result[^1], frame)) { result.Add(frame); }
                }
            }
            if (screen.SynchronizedOutput) { throw new InvalidDataException("Cast ended during an uncommitted synchronized frame."); }
            if (screen.HasIncompleteSequence) { throw new InvalidDataException("Cast ended during an incomplete VT sequence."); }
            CellBuffer final = screen.Transcript();
            if (result.Count == 0 || !SameFrame(result[^1], final)) { result.Add(final); }
            return result;
        }

        private static bool SameFrame(CellBuffer a, CellBuffer b) => a.Width == b.Width && a.Height == b.Height &&
            Enumerable.Range(0, a.Height).All(y => Enumerable.Range(0, a.Width).All(x => SameCell(a[x, y], b[x, y])));

        private static bool SameCell(Cell a, Cell b) => a.Width == b.Width && a.Glyph == b.Glyph && Normal(a).Equals(Normal(b));
        private static Style Normal(Cell cell) {
            Style value = FrameHtml.Reduce(cell.Style, ColorDepth.TrueColor);
            // ConPTY can retain a foreground on spaces while Unix preserves the writer's reset.
            // Underline, strike and reverse paint that foreground; all decoration flags and links stay strict.
            Color foreground = cell.Glyph == " " && cell.Width == 1 && (value.Decoration & (TextDecoration.Underline | TextDecoration.Strikethrough | TextDecoration.Reverse)) == 0
                ? Color.Default : Rgb(value.Color);
            return new Style(foreground, Rgb(value.BackgroundColor), value.Decoration, value.Link);
        }
        private static Color Rgb(Color color) {
            if (color.IsDefault) { return color; }
            color.ToRgb(out byte r, out byte g, out byte b);
            return Color.FromRgb(r, g, b);
        }

        private static JsonArray ChangedRows(Capture a, Capture b, out int changedRowCount, out long changedCellCount) {
            var rows = new JsonArray();
            changedRowCount = 0;
            changedCellCount = 0;
            for (int frame = 0; frame < Math.Max(a.Frames.Count, b.Frames.Count); frame++) {
                CellBuffer? x = frame < a.Frames.Count ? a.Frames[frame] : null, y = frame < b.Frames.Count ? b.Frames[frame] : null;
                for (int row = 0; row < Math.Max(x?.Height ?? 0, y?.Height ?? 0); row++) {
                    var cells = new JsonArray();
                    int rowChanges = 0;
                    for (int col = 0; col < Math.Max(x?.Width ?? 0, y?.Width ?? 0); col++) {
                        Cell? cellA = x != null && row < x.Height && col < x.Width ? x[col, row] : null;
                        Cell? cellB = y != null && row < y.Height && col < y.Width ? y[col, row] : null;
                        if (!cellA.HasValue || !cellB.HasValue || !SameCell(cellA.Value, cellB.Value)) {
                            rowChanges++;
                            if (rows.Count < 32 && cells.Count < 8) {
                                cells.Add(new JsonObject { ["column"] = col, ["left"] = CellDetail(cellA), ["right"] = CellDetail(cellB) });
                            }
                        }
                    }
                    if (rowChanges == 0) { continue; }
                    changedRowCount++;
                    changedCellCount += rowChanges;
                    if (rows.Count < 32) {
                        rows.Add(new JsonObject { ["frame"] = frame, ["row"] = row,
                            ["left"] = x != null && row < x.Height ? x.GetRowText(row) : null,
                            ["right"] = y != null && row < y.Height ? y.GetRowText(row) : null,
                            ["changedCellCount"] = rowChanges, ["omittedChangedCells"] = rowChanges - cells.Count,
                            ["cells"] = cells });
                    }
                }
            }
            return rows;
        }

        private static JsonArray ChangedScreenHashes(Capture a, Capture b) {
            var screens = new JsonArray();
            for (int frame = 0; frame < Math.Max(a.ScreenHashes.Length, b.ScreenHashes.Length); frame++) {
                string? x = frame < a.ScreenHashes.Length ? a.ScreenHashes[frame] : null, y = frame < b.ScreenHashes.Length ? b.ScreenHashes[frame] : null;
                if (x != y) {
                    screens.Add(new JsonObject { ["frame"] = frame, ["left"] = x, ["right"] = y,
                        ["reason"] = "Styled-screen hash differs; descriptions cannot identify the affected style rows." });
                }
            }
            return screens;
        }

        private static JsonObject? CellDetail(Cell? cell) => cell.HasValue ? new JsonObject {
            ["glyph"] = cell.Value.Glyph, ["width"] = cell.Value.Width,
            ["foreground"] = Rgb(cell.Value.Style.Color).ToString(), ["background"] = Rgb(cell.Value.Style.BackgroundColor).ToString(),
            ["decoration"] = cell.Value.Style.Decoration.ToString(), ["link"] = cell.Value.Style.Link
        } : null;

        internal static IEnumerable<string> Quality(CellBuffer frame) {
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    Cell cell = frame[x, y];
                    if (cell.Glyph.Contains('\uFFFD')) { yield return "replacement glyph at " + x + "," + y; }
                    if (!cell.IsContinuation && cell.Width != 2 && DisplayWidth.Measure(cell.Glyph) == 2) { yield return "truncated wide glyph at " + x + "," + y; }
                    if (cell.Width == 2 && (x + 1 >= frame.Width || !frame[x + 1, y].IsContinuation)) { yield return "truncated wide cell at " + x + "," + y; }
                    if (cell.IsContinuation && (cell.Glyph.Length != 0 || x == 0 || frame[x - 1, y].Width != 2)) { yield return "orphan wide-cell continuation at " + x + "," + y; }
                    if (!Segments(cell, out int up, out int right, out int down, out int left)) { continue; }
                    if (x + 1 < frame.Width && Segments(frame[x + 1, y], out int nu, out int nr, out int nd, out int nl) && (right > 0) != (nl > 0) &&
                        !(up > 0 && down > 0 && right == 0 && left == 0) && !(nu > 0 && nd > 0 && nr == 0 && nl == 0)) {
                        yield return "disconnected horizontal border from " + x + "," + y + " (" + cell.Glyph + ") to " + (x + 1) + "," + y + " (" + frame[x + 1, y].Glyph + ")";
                    }
                    if (y + 1 < frame.Height && Segments(frame[x, y + 1], out int bu, out int br, out int bd, out int bl) && (down > 0) != (bu > 0) &&
                        !(left > 0 && right > 0 && up == 0 && down == 0) && !(bl > 0 && br > 0 && bu == 0 && bd == 0)) {
                        yield return "disconnected vertical border from " + x + "," + y + " (" + cell.Glyph + ") to " + x + "," + (y + 1) + " (" + frame[x, y + 1].Glyph + ")";
                    }
                }
            }
        }

        private static bool Segments(Cell cell, out int up, out int right, out int down, out int left) {
            up = right = down = left = 0;
            return cell.Glyph.Length == 1 && GlyphPaint.TrySegments(cell.Glyph[0], out up, out right, out down, out left);
        }
    }
}
