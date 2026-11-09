using System;
using System.Collections;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Themes {
    /// <summary>
    /// A theme's style beside its colours: theme files carry it, default frames and status markers follow it, the profile's
    /// Border and StatusMarkers choices beat it, a widget's own frame beats both, and exports can use the theme in use.
    /// </summary>
    public sealed class ThemeAppearanceTests {
        private static string Text(IWidget widget, ITheme theme, TerminalProfile? profile = null) {
            RenderContext context = Terminal.CreateContext(profile ?? TestTerminal.Profile(40), theme);
            return FrameComposer.RenderBlock(widget, 40, context).ToPlainText();
        }

        [Fact]
        public void ThemeFile_ReadsStyleFromVersion2_InheritsTheRest_AndKeepsVersion1ForDefaultStyle() {
            Hashtable data = new Hashtable {
                ["SchemaVersion"] = 2, ["Name"] = "Team", ["BaseTheme"] = "Light", ["Tokens"] = new Hashtable(),
                ["Style"] = new Hashtable { ["border"] = "single", ["StatusMarkers"] = "Words" }
            };
            Theme team = ThemeFile.Import(data).ToTheme();
            Assert.Equal(new ThemeAppearance(BoxStyle.Single, StatusMarkerStyle.Words, ThemeBackground.Light), team.Appearance);
            Assert.Equal(team.Appearance, ThemeFile.Parse(ThemeFile.FromTheme(team).ToJson()).ToTheme().Appearance);

            data["SchemaVersion"] = 1;
            Assert.Contains("SchemaVersion 2", Assert.Throws<FormatException>(() => ThemeFile.Import(data)).Message);
            data["SchemaVersion"] = 2;
            data["Style"] = new Hashtable { ["Border"] = "None" };
            Assert.Throws<FormatException>(() => ThemeFile.Import(data));

            string dark = ThemeFile.FromTheme(Theme.Dark).ToJson();
            Assert.Contains("\"SchemaVersion\": 1", dark);
            Assert.DoesNotContain("\"Style\"", dark);
        }

        [Fact]
        public void DefaultFrames_FollowTheThemeAndProfile_ButAWidgetsOwnFrameStays() {
            Theme single = Theme.Dark.WithAppearance(new ThemeAppearance(BoxStyle.Single));
            Assert.StartsWith("╭", Text(new Panel(new TextBlock("x")), Theme.Dark));
            Assert.StartsWith("┌", Text(new Panel(new TextBlock("x")), single));
            Assert.StartsWith("┌", Text(new Table().AddColumn("A").AddRow("1"), single));
            Assert.StartsWith("┏", Text(new Panel(new TextBlock("x")), single, TestTerminal.Profile(40).WithBorder(BoxStyle.Heavy)));
            Assert.StartsWith("╔", Text(new Panel(new TextBlock("x")) { Border = BoxStyle.Double }, single));
            // The ASCII tier still draws ASCII frames.
            Assert.StartsWith("+", Text(new Panel(new TextBlock("x")), single, TestTerminal.Profile(40, glyphs: GlyphTier.Ascii)));
        }

        [Fact]
        public void WordMarkers_ReplaceSymbolsWithoutRepeatingTheStatusWord() {
            StatusList list = new StatusList().Add("Build", StatusKind.Pass).Add("Deploy", StatusKind.Fail);
            Assert.Contains("✓ Build", Text(list, Theme.Dark));
            string words = Text(list, Theme.Dark.WithAppearance(new ThemeAppearance(statusMarkers: StatusMarkerStyle.Words)));
            Assert.Contains("[OK]", words);
            Assert.Contains("[FAIL]", words);
            Assert.DoesNotContain("✓", words);

            Table table = new Table().AddColumn(new TableColumn("State") { Presenter = Presenters.CellPresenters.Status }).AddRow(TableCell.FromValue(StatusKind.Fail));
            string cell = Text(table, Theme.Dark, TestTerminal.Profile(40, culture: CultureInfo.GetCultureInfo("en-US")).WithStatusMarkers(StatusMarkerStyle.Words));
            Assert.Contains("[FAIL]", cell);
            Assert.Matches(@"\[FAIL\]\s*│", cell);
        }

        [Collection("Live sessions")]
        public sealed class Exports {
            [Fact]
            public void Current_DrawsTheThemeInUseOnTheBackgroundItIsMadeFor() {
                try {
                    Terminal.Theme = Theme.ColorBlindSafeLight;
                    string html = ConsoleExporter.ToHtml(new TextBlock("[pass]ok[/]"), new ExportOptions { Theme = ExportTheme.Current, Width = 20 });
                    string pass = Theme.ColorBlindSafeLight.Get(ThemeToken.Pass).Color!.Value.TrueColor.ToString().ToLowerInvariant();
                    Assert.Contains(pass, html.ToLowerInvariant());
                    Assert.Contains(ExportPalette.Light.Background, html);
                    Assert.DoesNotContain("prefers-color-scheme", html);
                } finally {
                    Terminal.ResetSettings(theme: true);
                }
            }
        }
    }
}
