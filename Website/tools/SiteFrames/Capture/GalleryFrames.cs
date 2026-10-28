using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using SiteFrames.Pages;

namespace SiteFrames.Capture {
    /// <summary>Drives ordinary programs using capture recipes kept outside the copyable source.</summary>
    internal static class GalleryFrames {
        internal static CellBuffer Run(GalleryPage.Demo demo, int step, bool fluent = false) {
            JsonNode state = demo.Metadata["capture"]!["states"]![step]!;
            string? ready = state["ready"]?.GetValue<string>() ?? demo.Metadata["capture"]!["ready"]?.GetValue<string>();
            string? actionClock = demo.Metadata["capture"]!["actionClock"]?.GetValue<string>();
            if (actionClock != null && actionClock != "Frozen") { throw new InvalidDataException(demo.Slug + ": unsupported actionClock " + actionClock); }
            var profile = CSharpRunner.Profile(demo.Width, demo.Height, ColorDepth.TrueColor, GlyphTier.Full);
            var target = new RecordingTarget(profile, ready == null ? state["keys"]?.GetValue<string>() : string.Empty) {
                Theme = Theme.Dark, SkipDelays = true,
                Checkpoints = state["checkpoint"] == null ? Array.Empty<int>() : new[] { state["checkpoint"]!.GetValue<int>() }
            };
            if (target.Input is ConsoleMonster.Input.ScriptedInput input) {
                input.RealTime = state["realTime"]?.GetValue<bool>() ?? false;
                if (ready != null) {
                    input.WaitForOutput(target.Screens, ready, TimeSpan.FromSeconds(30));
                    input.Append(state["keys"]?.GetValue<string>() ?? string.Empty);
                }
            }
            string? previousStorage = Environment.GetEnvironmentVariable("CONSOLEMONSTER_STORAGE_ROOT");
            string storage = Path.Combine(Path.GetTempPath(), "cm-gallery-presets-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_STORAGE_ROOT", storage);
            try {
                CSharpRunner.Execute(fluent ? demo.CSharpFluent : demo.CSharp, profile, Theme.Dark, new InteractiveOptions {
                    Target = target, Input = target.Input, Capture = target.Screens, Profile = profile, Theme = Theme.Dark,
                    ActionClock = actionClock == "Frozen" ? new ConsoleMonster.Live.SimulatedClock().Read : null,
                    Mode = target.Input == null ? InteractionMode.None : InteractionMode.Rich
                }, arguments: state["arguments"]?.AsArray().Select(a => a!.GetValue<string>()).ToArray(), recording: target, allowIncompleteInput: state["incomplete"]?.GetValue<bool>() ?? false);
            } finally {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_STORAGE_ROOT", previousStorage);
                Site.RemoveTemporary(storage);
            }
            if (state["checkpoint"] is JsonNode checkpoint && target.CheckpointCount <= checkpoint.GetValue<int>()) {
                throw new InvalidDataException(demo.Slug + " never reached checkpoint " + checkpoint);
            }
            CellBuffer frame;
            if (state["contains"] is JsonNode contains) {
                string text = contains.GetValue<string>();
                var matches = target.Screens.Frames.Where(f => f.ToPlainText().Contains(text, StringComparison.Ordinal));
                frame = ((state["first"]?.GetValue<bool>() ?? false) ? matches.FirstOrDefault() : matches.LastOrDefault())
                    ?? throw new InvalidDataException(demo.Slug + " never reached '" + text + "'.");
            } else if (state["outputStart"] == null && target.Screens.LastFrame != null) {
                frame = target.Screens.LastFrame;
            } else {
                frame = target.Transcript();
                if (state["outputStart"] is JsonNode outputStart) {
                    string text = outputStart.GetValue<string>();
                    int[] starts = Enumerable.Range(0, frame.Height).Where(row => frame.GetRowText(row).Contains(text, StringComparison.Ordinal)).ToArray();
                    int occurrence = state["occurrence"]?.GetValue<int>() ?? starts.Length - 1;
                    if (occurrence < 0 || occurrence >= starts.Length) throw new InvalidDataException(demo.Slug + " never printed '" + text + "'.");
                    int start = starts[occurrence];
                    int rows = state["rows"]?.GetValue<int>() ?? frame.Height - start;
                    var selected = new CellBuffer(frame.Width, rows);
                    selected.Blit(frame, 0, start, frame.Width, rows, 0, 0);
                    frame = selected;
                }
            }
            return Parity.Trim(frame);
        }
    }
}
