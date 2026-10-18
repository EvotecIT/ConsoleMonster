using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Localization;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Progress {
    /// <summary>Translated built-in labels: every language complete, placeholders intact, the language switch honoured.</summary>
    public sealed class LabelLocalizationTests {
        private static readonly Regex Placeholder = new Regex(@"\{\d+\}");

        public static IEnumerable<object[]> Languages() => LabelCatalog.Languages.Select(language => new object[] { language });

        private static IEnumerable<KeyValuePair<string, string>> English() {
            foreach (PropertyInfo property in typeof(PromptLabels).GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite)) {
                yield return new KeyValuePair<string, string>("Prompt." + property.Name, (string)property.GetValue(new PromptLabels())!);
            }
            foreach (PropertyInfo property in typeof(WidgetLabels).GetProperties().Where(p => p.PropertyType == typeof(string) && p.CanWrite)) {
                yield return new KeyValuePair<string, string>("Widget." + property.Name, (string)property.GetValue(new WidgetLabels())!);
            }
        }

        [Theory]
        [MemberData(nameof(Languages))]
        public void Catalog_HasEveryLabelWithTheSamePlaceholders(string language) {
            IReadOnlyDictionary<string, string> entries = LabelCatalog.Entries(language);
            List<string> problems = new List<string>();
            HashSet<string> known = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> label in English()) {
                known.Add(label.Key);
                if (!entries.TryGetValue(label.Key, out string? text) || text.Trim().Length == 0) {
                    problems.Add("missing " + label.Key);
                    continue;
                }
                string expected = string.Join(",", Placeholder.Matches(label.Value).Cast<Match>().Select(m => m.Value).OrderBy(v => v));
                string actual = string.Join(",", Placeholder.Matches(text).Cast<Match>().Select(m => m.Value).OrderBy(v => v));
                if (expected != actual) {
                    problems.Add("placeholders " + label.Key + ": " + actual + " instead of " + expected);
                }
            }
            problems.AddRange(entries.Keys.Where(key => !known.Contains(key)).Select(key => "unknown " + key));
            Assert.True(problems.Count == 0, language + ": " + string.Join("; ", problems));
        }

        [Theory]
        [InlineData("pl", "Tak", "Pokazano 2 z 5", "OK")]
        [InlineData("de", "Ja", "2 von 5 angezeigt", "OK")]
        [InlineData("es", "Sí", "Mostrando 2 de 5", "OK")]
        [InlineData("fr", "Oui", "2 affichés sur 5", "OK")]
        [InlineData("it", "Sì", "Visualizzati 2 di 5", "OK")]
        [InlineData("en", "Yes", "Showing 2 of 5", "PASS")]
        [InlineData("ja", "Yes", "Showing 2 of 5", "PASS")]
        public void Labels_FollowTheProfileLanguage(string language, string yes, string showing, string pass) {
            CultureInfo culture = CultureInfo.GetCultureInfo(language);
            Assert.Equal(yes, PromptLabels.For(culture).Yes);
            Table table = new Table { MaxRows = 2 }.AddColumn("Server").AddColumn("State");
            for (int i = 0; i < 5; i++) {
                table.AddValues("SRV0" + i, StatusKind.Pass);
            }
            string text = TestTerminal.RenderText(table, 60, culture: culture);
            Assert.Contains(showing, text);
            Assert.Contains(pass, text);
        }

        [Fact]
        public void Prompt_FooterFollowsTheTargetLanguage() {
            RecordingTarget target = TestTerminal.Target(60, 12, culture: CultureInfo.GetCultureInfo("pl-PL"), keys: "Enter");
            Prompt.Select("Serwer", new[] { "dc01", "dc02" }, options: new InteractiveOptions { Target = target });
            Assert.NotNull(target.Screens.FindFrame("pisz, aby filtrować"));
        }

        [Fact]
        public void WidgetLabels_OverrideReplacesEveryLanguage() {
            WidgetLabels custom = WidgetLabels.For(CultureInfo.InvariantCulture).Clone();
            custom.Showing = "{0}/{1} rows";
            try {
                WidgetLabels.Override = custom;
                Assert.Same(custom, WidgetLabels.For(CultureInfo.GetCultureInfo("de")));
            } finally {
                WidgetLabels.Override = null;
            }
            Assert.Equal("Showing {0} of {1}", WidgetLabels.For(CultureInfo.InvariantCulture).Showing);
        }

        [Fact]
        public void LiveSession_StreamLinesUseTheSessionLanguage() {
            RecordingTarget target = TestTerminal.Target(70, culture: CultureInfo.GetCultureInfo("de-DE"));
            Lanes lanes = new Lanes();
            using (ConsoleMonster.Live.LiveSession session = new ConsoleMonster.Live.LiveSession(lanes, new ConsoleMonster.Live.LiveSessionOptions { Target = target, CaptureConsole = false, HandleCancelKey = false, StreamTimestamps = false })) {
                session.Start();
                LanesHandle handle = lanes.Handle(session);
                handle.Add("srv01", "SRV01", 1);
                handle.Start("srv01");
                handle.Complete("srv01", StatusKind.Pass);
            }
            Assert.Contains("SRV01 gestartet", target.Text);
            Assert.Contains("SRV01 OK", target.Text);
        }
    }
}
