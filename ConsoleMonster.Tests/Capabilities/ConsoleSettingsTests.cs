using System;
using System.Collections.Generic;
using System.IO;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Capabilities {
    /// <summary>
    /// The settings resolver: this process above environment variables above the settings file above detection, with
    /// unusable file entries ignored and saving that keeps what it does not own.
    /// </summary>
    // Settings and environment variables are process-wide, so these run alone.
    [Collection("Live sessions")]
    public sealed class ConsoleSettingsTests : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-settings-" + Guid.NewGuid().ToString("N"));
        private readonly string? _filePath = ConsoleSettings.FilePath;
        private readonly string? _density = Environment.GetEnvironmentVariable("CONSOLEMONSTER_DENSITY");
        private readonly string? _glyphs = Environment.GetEnvironmentVariable("CONSOLEMONSTER_GLYPHS");
        private readonly string? _noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        private readonly string? _color = Environment.GetEnvironmentVariable("CONSOLEMONSTER_COLOR");

        public ConsoleSettingsTests() {
            Directory.CreateDirectory(_folder);
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_DENSITY", null);
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_GLYPHS", null);
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_COLOR", null);
            Environment.SetEnvironmentVariable("NO_COLOR", null);
            ConsoleSettings.Reset();
        }

        public void Dispose() {
            ConsoleSettings.Reset();
            ConsoleSettings.FilePath = _filePath;
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_GLYPHS", _glyphs);
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_DENSITY", _density);
            Environment.SetEnvironmentVariable("CONSOLEMONSTER_COLOR", _color);
            Environment.SetEnvironmentVariable("NO_COLOR", _noColor);
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }

        private string File(string json) {
            string path = Path.Combine(_folder, "settings.json");
            System.IO.File.WriteAllText(path, json);
            ConsoleSettings.FilePath = path;
            return path;
        }

        private static Dictionary<string, string> Overlaid(Dictionary<string, string> variables) {
            ConsoleSettings.Overlay(variables);
            return variables;
        }

        private static TerminalProfile DensityProfile() => new CapabilityDetector(
            new TerminalEnvironment(Overlaid(new Dictionary<string, string>()))).Detect();

        [Theory]
        [InlineData("Comfortable", ThemeDensity.Comfortable)]
        [InlineData("Compact", ThemeDensity.Compact)]
        public void StoredDensityIsAnExplicitOverrideEvenWhenItEqualsTheDefault(string value, ThemeDensity density) {
            File("{\"Density\":\"" + value + "\"}");
            Assert.Equal(density, DensityProfile().Density);
            ConsoleSettings.Set("Density", value);
            Assert.Equal(density, DensityProfile().Density);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void PickerCanApplyComfortableOverACompactTheme(bool save, bool startsCompact) {
            File("{}");
            if (startsCompact) { ConsoleSettings.Set("Density", "Compact"); }
            var chosen = new Dictionary<string, string> { ["Density"] = "Comfortable" };
            SettingsPicker.Apply(new SettingsPicker.Result(
                save ? SettingsPickerAction.Save : SettingsPickerAction.Apply, chosen));
            Assert.Equal(ThemeDensity.Comfortable, DensityProfile().Density);
            Theme theme = Theme.Dark.WithAppearance(new ThemeAppearance(ThemeDensity.Compact));
            string text = Terminal.RenderText(new Panel(new TextBlock("x")), DensityProfile(), theme);
            Assert.Contains("│ x", text);
            Assert.Equal(save ? ConsoleSettingSource.File : ConsoleSettingSource.Session, ConsoleSettings.Get("Density").Source);
        }

        [Fact]
        public void Session_BeatsEnvironment_WhichBeatsTheFile_WhichBeatsDetection() {
            File("{ \"Glyphs\": \"Basic\", \"Mouse\": false }");
            Assert.Equal(("Basic", ConsoleSettingSource.File), (ConsoleSettings.Get("Glyphs").Value, ConsoleSettings.Get("Glyphs").Source));
            Assert.Equal(("Off", ConsoleSettingSource.File), (ConsoleSettings.Get("Mouse").Value, ConsoleSettings.Get("Mouse").Source));
            Assert.Equal(("Auto", ConsoleSettingSource.Default), (ConsoleSettings.Get("Theme").Value, ConsoleSettings.Get("Theme").Source));
            // Detection reads the file's values as the variables they stand for.
            Assert.Equal("basic", Overlaid(new Dictionary<string, string>())["CONSOLEMONSTER_GLYPHS"]);
            Assert.Equal("1", Overlaid(new Dictionary<string, string>())["CONSOLEMONSTER_NOMOUSE"]);

            Environment.SetEnvironmentVariable("CONSOLEMONSTER_GLYPHS", "full");
            Assert.Equal(("Full", ConsoleSettingSource.Environment), (ConsoleSettings.Get("Glyphs").Value, ConsoleSettings.Get("Glyphs").Source));
            Assert.Equal("full", Overlaid(new Dictionary<string, string> { ["CONSOLEMONSTER_GLYPHS"] = "full" })["CONSOLEMONSTER_GLYPHS"]);

            ConsoleSettings.Set("Glyphs", "ascii");
            Assert.Equal(("Ascii", ConsoleSettingSource.Session), (ConsoleSettings.Get("Glyphs").Value, ConsoleSettings.Get("Glyphs").Source));
            Assert.Equal("ascii", Overlaid(new Dictionary<string, string> { ["CONSOLEMONSTER_GLYPHS"] = "full" })["CONSOLEMONSTER_GLYPHS"]);
            // Auto for this session clears the variable, so detection decides again.
            ConsoleSettings.Set("Glyphs", "Auto");
            Assert.False(Overlaid(new Dictionary<string, string> { ["CONSOLEMONSTER_GLYPHS"] = "full" }).ContainsKey("CONSOLEMONSTER_GLYPHS"));

            ConsoleSettings.Reset("Glyphs");
            Assert.Equal(ConsoleSettingSource.Environment, ConsoleSettings.Get("Glyphs").Source);
        }

        [Fact]
        public void UnusableFileEntries_AreIgnoredAndReported_AndNoColorKeepsColourOff() {
            File("{ \"Theme\": \"Purple\", \"Color\": \"TrueColor\", \"Culture\": \"pl-PL\" }");
            Assert.Equal(ConsoleSettingSource.Default, ConsoleSettings.Get("Theme").Source);
            Assert.Contains(ConsoleSettings.Problems, problem => problem.Contains("Theme") && problem.Contains("Purple"));
            Assert.Equal("pl-PL", ConsoleSettings.Get("Culture").Value);
            Assert.Equal("truecolor", Overlaid(new Dictionary<string, string>())["CONSOLEMONSTER_COLOR"]);

            Environment.SetEnvironmentVariable("NO_COLOR", "1");
            Assert.Equal(("None", ConsoleSettingSource.Environment), (ConsoleSettings.Get("Color").Value, ConsoleSettings.Get("Color").Source));
            Assert.False(Overlaid(new Dictionary<string, string> { ["NO_COLOR"] = "1" }).ContainsKey("CONSOLEMONSTER_COLOR"));

            File("{ not json");
            Assert.Equal(ConsoleSettingSource.Default, ConsoleSettings.Get("Culture").Source);
            Assert.Single(ConsoleSettings.Problems);
            Assert.Throws<IOException>(() => ConsoleSettings.Set("Theme", "Light", save: true));
        }

        [Fact]
        public void Saving_KeepsSectionsItDoesNotOwn_AndResetRemovesTheValue() {
            string path = File("{ \"Formatting\": { \"Include\": [ \"Help\" ] }, \"FutureKey\": true }");
            ConsoleSettings.Set("Theme", "ColorBlindSafe", save: true);
            Assert.Equal(("ColorBlindSafe", ConsoleSettingSource.File), (ConsoleSettings.Get("Theme").Value, ConsoleSettings.Get("Theme").Source));
            string saved = System.IO.File.ReadAllText(path);
            Assert.Contains("\"Include\": [\"Help\"]", saved);
            Assert.Contains("\"FutureKey\": true", saved);

            ConsoleSettings.Reset("Theme", save: true);
            Assert.Equal(ConsoleSettingSource.Default, ConsoleSettings.Get("Theme").Source);
            Assert.DoesNotContain("ColorBlindSafe", System.IO.File.ReadAllText(path));
            Assert.Throws<ArgumentException>(() => ConsoleSettings.Set("Theme", "Purple"));
        }
    }
}
