using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.Live.Tails;
using ConsoleMonster.Styling;
using SiteFrames.Capture;
using SiteFrames.Frames;

namespace SiteFrames.Pages {
    internal static partial class TailsPage {
        /// <summary>Captures native PowerShell navigation and writes real engine frames as HTML, SVG and text.</summary>
        public static int Review(Site site, string output) {
            Directory.CreateDirectory(output);
            Capture(site);
            foreach (int width in Widths) {
                foreach ((string name, ITheme theme, ExportTheme exportTheme) in new[] {
                    ("dark", (ITheme)Theme.Dark, ExportTheme.Dark), ("light", (ITheme)Theme.Light, ExportTheme.Light)
                }) {
                    Dictionary<string, CellBuffer> frames = Run(site, width, theme);
                    foreach (KeyValuePair<string, CellBuffer> frame in ReviewStates(width, theme)) { frames.Add(frame.Key, frame.Value); }
                    foreach (PowerShellHost host in PowerShellHost.Available) {
                        string path = Path.Combine(output, "tails-" + host.Id + "-" + width + "-" + name + ".ansi");
                        string arguments = "-Example '" + Example(site, "file.ps1").Replace("'", "''")
                            + "' -Columns " + width + " -Appearance " + name + " -Navigation -Receipt '" + (path + ".json").Replace("'", "''") + "'";
                        File.WriteAllBytes(path, PowerShellRunner.Bytes(PowerShellRunner.Run(site, host, Driver(site), width, Rows, PowerShellRunner.Environment(name),
                            timeout: TimeSpan.FromSeconds(30), arguments: arguments, hostedOutput: true)));
                        Dictionary<string, CellBuffer> native = FindFrames(File.ReadAllText(path), width);
                        foreach (string state in new[] { "following", "paused", "resumed" }) {
                            if (!native.ContainsKey(state)) { throw new InvalidOperationException(host.Id + " did not capture " + state); }
                            ParityResult parity = Parity.Compare(frames[state], native[state]);
                            if (!parity.Identical) { throw new InvalidOperationException(host.Id + " " + state + ": " + parity.Detail); }
                            frames.Add(host.Id + "-" + state, native[state]);
                        }
                    }
                    foreach (KeyValuePair<string, CellBuffer> frame in frames) {
                        WriteReviewFrame(output, "tails-" + frame.Key + "-" + width + "-" + name, frame.Value, exportTheme);
                    }
                }
            }
            Console.WriteLine("Wrote offline tails review to " + Path.GetFullPath(output));
            return 0;
        }

        // Synthetic source fixtures exercise applied arrivals and timestamp policy with the same public view as live runs.
        internal static Dictionary<string, CellBuffer> ReviewStates(int width, ITheme theme) {
            Dictionary<string, CellBuffer> frames = new Dictionary<string, CellBuffer>();
            LogTailView view = new LogTailView();
            DateTimeOffset source = new DateTimeOffset(2026, 10, 22, 23, 59, 59, TimeSpan.Zero);
            Append(new TailedLogEntry("2026-10-22T23:59:59Z INFO database ready", "info", source, "database ready"));
            view.Apply(new LiveEvent(view.WidgetId, "home"));
            Append(new TailedLogEntry("2026-10-22T23:59:59Z WARN database retry", "warn", source, "database retry\nretry scheduled"));
            frames.Add("paused-new-lines", Draw());
            view.Apply(new LiveEvent(view.WidgetId, "follow"));
            Append(new TailedLogEntry("2026-10-23T00:00:00Z INFO database connected", "info", source.AddSeconds(1), "database connected"));
            frames.Add("source-days", Draw());
            view.TimestampFormat = "yyyy-MM-dd HH:mm:ss zzz";
            frames.Add("timestamp-override", Draw());
            return frames;

            void Append(TailedLogEntry entry) => view.Apply(new LiveEvent(view.WidgetId, "append") { Payload = entry });
            CellBuffer Draw() {
                CellBuffer frame = new CellBuffer(width, 6);
                view.Render(new Region(frame), new RenderContext(TerminalProfile.Fixed(width, 6), theme));
                return frame;
            }
        }

        internal static void WriteReviewFrame(string output, string id, CellBuffer frame, ExportTheme theme) {
            ExportOptions options = new ExportOptions { Theme = theme, Title = id, WindowChrome = true };
            string path = Path.Combine(output, id);
            File.WriteAllText(path + ".html", ConsoleExporter.ToHtml(frame, options), new UTF8Encoding(false));
            File.WriteAllText(path + ".svg", ConsoleExporter.ToSvg(frame, options), new UTF8Encoding(false));
            File.WriteAllText(path + ".txt", ConsoleExporter.ToText(frame), new UTF8Encoding(false));
        }
    }
}
