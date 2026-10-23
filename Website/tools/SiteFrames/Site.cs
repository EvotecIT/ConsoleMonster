using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using SiteFrames.Frames;

namespace SiteFrames {
    /// <summary>A colour tier the site can switch to.</summary>
    internal sealed class ColorTier {
        public ColorTier(string id, string label, ColorDepth depth) {
            Id = id;
            Label = label;
            Depth = depth;
        }

        public string Id { get; }

        public string Label { get; }

        public ColorDepth Depth { get; }

        public static readonly ColorTier[] All = {
            new ColorTier("24bit", "24-bit", ColorDepth.TrueColor),
            new ColorTier("256", "256", ColorDepth.Ansi256),
            new ColorTier("16", "16", ColorDepth.Ansi16),
            new ColorTier("none", "No colour", ColorDepth.None)
        };
    }

    /// <summary>A glyph tier the site can switch to.</summary>
    internal sealed class GlyphLevel {
        public GlyphLevel(string id, string label, GlyphTier tier) {
            Id = id;
            Label = label;
            Tier = tier;
        }

        public string Id { get; }

        public string Label { get; }

        public GlyphTier Tier { get; }

        public static readonly GlyphLevel[] All = {
            new GlyphLevel("full", "Full", GlyphTier.Full),
            new GlyphLevel("basic", "Basic", GlyphTier.Basic),
            new GlyphLevel("ascii", "ASCII", GlyphTier.Ascii)
        };
    }

    /// <summary>Paths, the shared style registry and deterministic writers for everything the tool produces.</summary>
    internal sealed class Site {
        public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };

        public Site(string website) {
            Website = Path.GetFullPath(website);
            Repository = Path.GetFullPath(Path.Combine(Website, ".."));
            if (!File.Exists(Path.Combine(Repository, "ConsoleMonster.sln"))) {
                throw new DirectoryNotFoundException("Pass the Website folder of the ConsoleMonster repository (got " + Website + ").");
            }
        }

        public string Website { get; }

        public string Repository { get; }

        public FrameStyles Styles { get; } = new FrameStyles();

        public string Tool => Path.Combine(Website, "tools", "SiteFrames");

        public string Captures => Path.Combine(Website, "captures");

        public string Data(string name) => Path.Combine(Website, "data", name + ".json");

        public string Static(string relative) => Path.Combine(Website, "static", relative.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>Renders a widget the way a terminal of this tier shows it.</summary>
        public static CellBuffer Render(IWidget widget, int width, ColorDepth depth, GlyphTier glyphs, ITheme theme, int height = 0, TimeSpan elapsed = default) {
            ExportOptions options = new ExportOptions { Width = width, Height = height, ColorDepth = depth, Glyphs = glyphs, Culture = Culture, Elapsed = elapsed };
            return ConsoleExporter.Render(widget, options, theme);
        }

        /// <summary>A frame as page data: its size and the dark and light renderings.</summary>
        public JsonObject Frame(Func<IWidget> build, int width, ColorDepth depth = ColorDepth.TrueColor, GlyphTier glyphs = GlyphTier.Full, int height = 0, bool crop = false) {
            CellBuffer dark = Render(build(), width, depth, glyphs, Theme.Dark, height);
            CellBuffer light = Render(build(), width, depth, glyphs, Theme.Light, height);
            if (crop) {
                // Both renderings keep one width so the page does not shift when the theme changes.
                int w = Math.Max(ContentWidth(dark), ContentWidth(light));
                dark = CropTo(dark, w);
                light = CropTo(light, w);
            }
            return Frame(dark, light, depth);
        }

        public JsonObject Frame(CellBuffer dark, CellBuffer? light, ColorDepth depth = ColorDepth.TrueColor) {
            string[] darkRows = FrameHtml.Rows(dark, depth, Screen.Dark, Styles);
            Note(dark);
            JsonObject frame = new JsonObject {
                ["w"] = dark.Width,
                ["h"] = Lines(darkRows),
                ["dark"] = FrameHtml.Body(darkRows)
            };
            if (light != null) {
                string[] lightRows = FrameHtml.Rows(light, depth, Screen.Light, Styles);
                Note(light);
                frame["light"] = FrameHtml.Body(lightRows);
                frame["h"] = Math.Max(Lines(darkRows), Lines(lightRows));
            }
            return frame;
        }

        /// <summary>
        /// A frame from a program shown at a wide and a narrow width on a docs page. The narrow rendering is there to show
        /// what that width does, so it keeps its declared width; the wide one (100 columns or more) is cut to the columns its
        /// output uses, because a program rarely fills a 120-column terminal and an empty window only shrinks the text.
        /// </summary>
        /// <param name="width">The terminal width the program ran at.</param>
        /// <param name="dark">The dark rendering.</param>
        /// <param name="light">The light rendering, or null.</param>
        public JsonObject FrameAt(int width, CellBuffer dark, CellBuffer? light) {
            if (width < FitFrom) {
                return Frame(dark, light);
            }
            (CellBuffer fitDark, CellBuffer? fitLight) = Fit(dark, light);
            return Frame(fitDark, fitLight);
        }

        /// <summary>The narrowest terminal width whose frames are cut to their content by <see cref="FrameAt"/>.</summary>
        internal const int FitFrom = 100;

        /// <summary>
        /// Both renderings cut to the columns either of them uses. They keep one width, so the page does not shift when the
        /// theme changes. Rows need no cutting: trailing empty rows are left out when a frame is written.
        /// </summary>
        internal static (CellBuffer Dark, CellBuffer? Light) Fit(CellBuffer dark, CellBuffer? light) {
            int width = ContentWidth(dark);
            if (light != null) {
                width = Math.Max(width, ContentWidth(light));
            }
            return (CropTo(dark, width), light == null ? null : CropTo(light, width));
        }

        /// <summary>A copy of a captured frame cut to the columns that show anything, so the page can draw it larger.</summary>
        public static CellBuffer Crop(CellBuffer frame) => CropTo(frame, ContentWidth(frame));

        /// <summary>The column after the last one any row of the frame uses (text, a background or a decoration).</summary>
        internal static int ContentWidth(CellBuffer frame) {
            int width = 1;
            for (int y = 0; y < frame.Height; y++) {
                width = Math.Max(width, FrameHtml.ContentEnd(frame, y, ColorDepth.TrueColor));
            }
            return width;
        }

        internal static CellBuffer CropTo(CellBuffer frame, int width) {
            if (width >= frame.Width) {
                return frame;
            }
            CellBuffer cropped = new CellBuffer(width, frame.Height);
            cropped.Blit(frame, 0, 0, width, frame.Height, 0, 0);
            return cropped;
        }

        public void Note(CellBuffer frame) {
            for (int y = 0; y < frame.Height; y++) {
                for (int x = 0; x < frame.Width; x++) {
                    Cell cell = frame[x, y];
                    if (!cell.IsContinuation && !string.IsNullOrEmpty(cell.Glyph)) {
                        Styles.NoteGlyph(cell.Glyph);
                    }
                }
            }
        }

        public static int Lines(string[] rows) {
            int count = rows.Length;
            while (count > 1 && rows[count - 1].Length == 0) {
                count--;
            }
            return count;
        }

        public void WriteJson(string path, JsonNode node) {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string text = node.ToJsonString(JsonOptions).Replace("\r\n", "\n") + "\n";
            Write(path, text);
        }

        public void Write(string path, string text) {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text, new UTF8Encoding(false));
            Written.Add(Path.GetRelativePath(Website, path).Replace('\\', '/'));
        }

        public List<string> Written { get; } = new List<string>();

        /// <summary>Reads a UTF-8 text file without its byte order mark and with LF line ends.</summary>
        public static string ReadText(string path) {
            string text = File.ReadAllText(path, Encoding.UTF8);
            if (text.Length > 0 && text[0] == '﻿') {
                text = text.Substring(1);
            }
            return text.Replace("\r\n", "\n").TrimEnd('\n');
        }

        /// <summary>Deletes a generated folder's contents before it is written again, so removed items leave nothing behind.</summary>
        public static void Clean(string folder) {
            if (Directory.Exists(folder)) {
                Directory.Delete(folder, true);
            }
            Directory.CreateDirectory(folder);
        }

        /// <summary>
        /// Removes a temporary folder this tool created for one run. A file still held (a process that timed out) leaves the
        /// folder with a warning instead of hiding the run's own error.
        /// </summary>
        public static void RemoveTemporary(string folder) {
            try {
                if (Directory.Exists(folder)) {
                    Directory.Delete(folder, true);
                }
            } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                Console.Error.WriteLine("warning: could not remove " + folder + ": " + ex.Message);
            }
        }
    }
}
