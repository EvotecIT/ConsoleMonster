using System;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Interactive.App {
    [Collection("Live sessions")]
    public sealed class ExternalCaptureTests {
        [Fact]
        public void CaptureStorageRootIsIsolatedAndExplicitStorageStillWins() {
            string? previous = Environment.GetEnvironmentVariable("CONSOLEMONSTER_STORAGE_ROOT");
            string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cm-presets-" + Guid.NewGuid().ToString("N"));
            try {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_STORAGE_ROOT", root);
                Assert.Equal(System.IO.Path.Combine(root, "sample"), new ChoiceStore("sample").Folder);
                string explicitRoot = System.IO.Path.Combine(root, "explicit");
                Assert.Equal(System.IO.Path.Combine(explicitRoot, "sample"), new ChoiceStore("sample", explicitRoot).Folder);
                Assert.False(System.IO.Directory.Exists(root));
            } finally { Environment.SetEnvironmentVariable("CONSOLEMONSTER_STORAGE_ROOT", previous); }
        }

        [Fact]
        public void RecordingTranscriptRetainsPrintedCellsAndStylesSeparatelyFromScreens() {
            var target = new RecordingTarget(ConsoleMonster.Capabilities.TerminalProfile.Fixed(40, 12));
            using (Terminal.Use(target)) { Terminal.Write(new ConsoleMonster.Widgets.TextBlock("[fail]sample[/]")); }
            string output = target.Output;
            var frame = target.Transcript();
            Assert.Equal("sample", frame.GetRowText(0).TrimEnd());
            Assert.False(frame[0, 0].Style.Color.IsDefault);
            Assert.Empty(target.Screens.Frames);
            Assert.Equal(output, target.Output);
        }

        [Fact]
        public void EnvironmentInputAnswersSuccessivePromptsAndExplicitInputWins() {
            string? previous = Environment.GetEnvironmentVariable("CONSOLEMONSTER_INPUT");
            IInputSource? oldOverride = ConsoleInput.Override;
            try {
                ConsoleInput.Override = null;
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_INPUT", "\"first\" Enter \"second\" Enter");
                var options = new InteractiveOptions { Mode = InteractionMode.Rich, Capture = new ScreenCapture(80, 24) };
                Assert.Equal("first", new TextPrompt("First").Show(options));
                Assert.Equal("second", new TextPrompt("Second").Show(options));
                using var explicitInput = new ScriptedInput("\"explicit\" Enter");
                ConsoleInput.Override = explicitInput;
                Assert.Equal("explicit", new TextPrompt("Third").Show(options));
                Assert.False(ConsoleInput.IsAcquired);
                ConsoleInput.Override = null;
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_INPUT", null);
                Assert.Null(ConsoleInput.Override);
            } finally {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_INPUT", previous);
                ConsoleInput.Override = oldOverride;
            }
        }

        [Fact]
        public void RecordingTargetCapturesOrdinaryAppWithoutOptionsAndRestoresDefaults() {
            var target = TestTerminal.Target(80, 24, keys: "\"sql\" Escape");
            InteractiveOptions previous = InteractiveOptions.Default;
            var defaults = new InteractiveOptions();
            try {
                InteractiveOptions.Default = defaults;
                using (Terminal.Use(target)) {
                    var state = ConsoleApp.Run(new Rows(new TextInput("Query")));
                    Assert.Equal("sql", state.Get<string>("Query"));
                }
                Assert.Contains("sql", target.Screens.LastFrame!.ToPlainText());
                Assert.Null(defaults.Capture);
                Assert.Null(defaults.Input);
                Assert.False(ConsoleInput.IsAcquired);
            } finally { InteractiveOptions.Default = previous; }
        }

        [Fact]
        public void SnapshotEnvironmentInitializesAppButDoesNotOverrideScriptedKeys() {
            string? previous = Environment.GetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT");
            try {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", "1");
                var snapshot = TestTerminal.Target(80, 24);
                using (Terminal.Use(snapshot)) {
                    var state = ConsoleApp.Run(new TextInput("Query", "initial"));
                    Assert.Equal("initial", state.Get<string>("Query"));
                    Assert.False(state.ExitRequested);
                }
                Assert.Single(snapshot.Screens.Frames);
                var scripted = TestTerminal.Target(80, 24, keys: "\"edited\" Escape");
                using (Terminal.Use(scripted)) {
                    Assert.Equal("edited", ConsoleApp.Run(new TextInput("Query")).Get<string>("Query"));
                }
            } finally { Environment.SetEnvironmentVariable("CONSOLEMONSTER_SNAPSHOT", previous); }
        }
    }
}
