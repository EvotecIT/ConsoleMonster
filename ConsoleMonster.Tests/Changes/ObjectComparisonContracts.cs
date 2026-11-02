using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Changes;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Styling;
using ConsoleMonster.Testing;
using Xunit;

namespace ConsoleMonster.Tests.Changes {
    public sealed class ObjectComparisonContracts {
        [Fact]
        public void CaseRule_AppliesToKeysValuesAndArrays_WithoutHidingCaseOnlyDifferences() {
            var before = new object[] { new { Name = "dc01", Site = "HQ", Roles = new[] { "DNS" } } };
            var after = new object[] { new { Name = "DC01", Site = "hq", Roles = new[] { "dns" } } };
            ChangeSet insensitive = ObjectDiff.Compare(before, after);
            Assert.False(insensitive.HasChanges);
            Assert.Equal(1, insensitive.Unchanged);
            Assert.Equal(3, insensitive.CaseOnly);
            ObjectChange same = Assert.Single(insensitive.Changes);
            Assert.Equal(ChangeKind.Unchanged, same.Kind);
            Assert.All(same.Properties, property => Assert.True(property.IsCaseOnly));
            foreach (ChangeViewMode mode in new[] { ChangeViewMode.Full, ChangeViewMode.Summary, ChangeViewMode.Table, ChangeViewMode.SideBySide, ChangeViewMode.Unified }) {
                Assert.Contains("case only", TestTerminal.RenderText(new ChangeView(insensitive, mode), 100, 0, GlyphTier.Ascii));
            }
            ChangeSet sensitive = ObjectDiff.Compare(before, after, new DiffOptions { CaseSensitive = true });
            Assert.Equal(1, sensitive.Added);
            Assert.Equal(1, sensitive.Removed);
            Assert.Single(ObjectDiff.CompareObjects(new { Site = "HQ" }, new { Site = "hq" }, new DiffOptions { CaseSensitive = true }));
        }

        [Fact]
        public void MissingProperties_PreservePresenceAndRenderInEveryDetailedView() {
            var before = new Dictionary<string, object?> { ["Name"] = "dc01", ["Empty"] = "", ["Null"] = null, ["EmptyRecord"] = new Dictionary<string, object?>() };
            var after = new Dictionary<string, object?> { ["Name"] = "dc01", ["Address"] = new { City = "Warsaw" } };
            ChangeSet comparison = ObjectDiff.Compare((object)before, after);
            Assert.Equal(1, comparison.Changed);
            var properties = Assert.Single(comparison.Changes).Properties;
            Assert.Equal(new[] { "Empty", "Null", "EmptyRecord", "Address.City" }, properties.Select(p => p.Path));
            Assert.All(properties.Take(3), property => { Assert.True(property.HasBefore); Assert.False(property.HasAfter); });
            Assert.False(properties[3].HasBefore);
            Assert.True(properties[3].HasAfter);
            {
                foreach (ChangeViewMode mode in new[] { ChangeViewMode.Full, ChangeViewMode.Table, ChangeViewMode.SideBySide, ChangeViewMode.Unified }) {
                    string rendered = TestTerminal.RenderText(new ChangeView(comparison, mode), 100, 0, GlyphTier.Ascii, CultureInfo.GetCultureInfo("pl"));
                    Assert.Contains("(brak)", rendered);
                    Assert.Contains("Warsaw", rendered);
                }
            }
        }

        [Fact]
        public void Normalization_UsesExactNestedPathsAndOriginalValues_InBothComparisons() {
            var options = new DiffOptions();
            options.Normalize["Settings.Spn"] = value => ((string)value!).Replace("dc01", "HOST").Replace("dc02", "HOST");
            object first = new { Name = "dc01", Settings = new { Spn = "HOST/dc01.example", Port = 80 } };
            object second = new { Name = "dc02", Settings = new { Spn = "HOST/dc02.example", Port = 443 } };
            options.Ignore.Add("Name");
            var changes = ObjectDiff.CompareObjects(first, second, options);
            Assert.Equal("Settings.Port", Assert.Single(changes).Path);
            options.IncludeUnchanged = true;
            BaselineComparison matrix = ObjectDiff.CompareBaseline(new[] { first, second }, options);
            BaselineRow spn = matrix.Rows.Single(r => r.Path == "Settings.Spn");
            Assert.False(spn.HasDifferences);
            Assert.Equal("HOST/dc02.example", spn.Cells[1].Value);
            Assert.Equal(ChangeKind.Changed, matrix.Rows.Single(r => r.Path == "Settings.Port").Cells[1].Kind);
            options.Normalize["Settings.Port"] = value => Convert.ToInt32(value) / 10;
            PropertyChange port = Assert.Single(ObjectDiff.CompareObjects(first, second, options));
            Assert.Equal(80, port.Before);
            Assert.Equal(443, port.After);
        }

        [Fact]
        public void BaselineMatrix_UsesSelectedReferenceAndInputOrder_WithMissingAndEqualRows() {
            object[] objects = {
                new Dictionary<string, object?> { ["Name"] = "dc01", ["Port"] = 80, ["Site"] = "HQ", ["Optional"] = "", ["EmptyRecord"] = new Dictionary<string, object?>(), ["Spn"] = "ldap/dc01.example", ["CaseValue"] = "BASE" },
                new Dictionary<string, object?> { ["Name"] = "dc02", ["Port"] = 443, ["Site"] = "HQ", ["Optional"] = null, ["Spn"] = "ldap/dc02.example", ["CaseValue"] = "BASE" },
                new Dictionary<string, object?> { ["Name"] = "dc03", ["Port"] = 443, ["Site"] = "HQ", ["Spn"] = "ldap/dc03.example", ["CaseValue"] = "base" }
            };
            var options = new DiffOptions { Baseline = 1 };
            options.Ignore.Add("Name");
            options.Normalize["Spn"] = value => System.Text.RegularExpressions.Regex.Replace((string)value!, "/[^.]+", "/HOST");
            BaselineComparison comparison = ObjectDiff.CompareBaseline(objects, options);
            Assert.Equal(new[] { "dc01", "dc02", "dc03" }, comparison.Names);
            Assert.Equal(1, comparison.BaselineIndex);
            Assert.DoesNotContain(comparison.Rows, row => row.Path == "Site");
            Assert.DoesNotContain(comparison.Rows, row => row.Path == "Spn");
            Assert.Equal(2, comparison.HiddenMatchingCount);
            Assert.Equal(new[] { ChangeKind.Changed, ChangeKind.Unchanged, ChangeKind.Unchanged }, comparison.Rows.Single(r => r.Path == "Port").Cells.Select(c => c.Kind));
            BaselineRow optional = comparison.Rows.Single(r => r.Path == "Optional");
            Assert.Equal(1, optional.MissingCount);
            Assert.True(optional.Cells[0].IsPresent);
            Assert.True(optional.Cells[1].IsPresent);
            Assert.False(optional.Cells[2].IsPresent);
            Assert.Equal(2, comparison.Rows.Single(r => r.Path == "EmptyRecord").MissingCount);
            string rendered = TestTerminal.RenderText(new BaselineView(comparison), 100, 0, GlyphTier.Ascii);
            Assert.Contains("dc02 (baseline)", rendered);
            Assert.Contains("missing on 1", rendered);
            Assert.Contains("(missing)", rendered);
            Assert.Contains("differs", rendered);
            Assert.EndsWith("| missing on 1 |", rendered.Split('\n').Single(line => line.Contains("| Optional")).TrimEnd());
            Assert.Contains("4 properties shown, 2 matching hidden", rendered);
            string compact = TestTerminal.RenderText(new BaselineView(comparison), 48, 0, GlyphTier.Ascii);
            Assert.All(compact.Split('\n'), line => Assert.True(line.Length <= 48));
            Assert.DoesNotContain("records", compact);
            Assert.Contains("4 properties shown", compact);
            Assert.Contains("(empty)", compact);
            Assert.DoesNotContain("columns (", TestTerminal.RenderText(new BaselineView(comparison), 60, 0, GlyphTier.Ascii));
            string translated = TestTerminal.RenderText(new BaselineView(comparison), 100, 0, GlyphTier.Basic, CultureInfo.GetCultureInfo("pl"));
            Assert.Contains("Pokazano 4 właściwości, ukryto 2 zgodnych", translated);
            options.IncludeUnchanged = true;
            BaselineComparison all = ObjectDiff.CompareBaseline(objects, options);
            Assert.Contains(all.Rows, row => row.Path == "Site" && !row.HasDifferences);
            Assert.Equal(0, all.HiddenMatchingCount);
            string allText = TestTerminal.RenderText(new BaselineView(all), 100, 0, GlyphTier.Ascii);
            Assert.Contains("6 properties shown", allText);
            Assert.DoesNotContain("hidden", allText);
            foreach (ITheme theme in new ITheme[] { Theme.Dark, Theme.Light }) {
                CellBuffer frame = ConsoleExporter.Render(new BaselineView(all), new ExportOptions { Width = 140, ColorDepth = ColorDepth.TrueColor, Glyphs = GlyphTier.Ascii }, theme);
                string[] lines = frame.ToPlainText().Split('\n');
                Cell At(string row, string value) {
                    int y = Array.FindIndex(lines, line => line.Contains(row));
                    Assert.InRange(y, 0, lines.Length - 1);
                    int x = lines[y].IndexOf(value, StringComparison.Ordinal);
                    Assert.InRange(x, 0, frame.Width - 1);
                    return frame[x, y];
                }
                Assert.Equal(theme.Resolve(ThemeToken.Warn, ColorDepth.TrueColor).Color, At("Port", "differs").Style.Color);
                Assert.Equal(theme.Resolve(ThemeToken.Fail, ColorDepth.TrueColor).Color, At("Optional", "missing on 1").Style.Color);
                Assert.Equal(theme.Resolve(ThemeToken.Warn, ColorDepth.TrueColor).Color, At("CaseValue", "case only").Style.Color);
                Assert.Equal(theme.Resolve(ThemeToken.Muted, ColorDepth.TrueColor).Color, At("Site", "same").Style.Color);
                Assert.Equal(theme.Resolve(ThemeToken.Accent, ColorDepth.TrueColor).Color, At("Property", "dc02").Style.Color);
                Cell baseline = At("Port", "443");
                Assert.Equal(theme.Resolve(ThemeToken.Accent, ColorDepth.TrueColor).Color, baseline.Style.Color);
                Assert.Equal(TextDecoration.Bold, baseline.Style.Decoration & TextDecoration.Bold);
                Assert.Equal(theme.Resolve(ThemeToken.Text, ColorDepth.TrueColor).Color, At("Site", "HQ").Style.Color);
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => ObjectDiff.CompareBaseline(objects, new DiffOptions { Baseline = 3 }));
        }

        [Theory]
        [InlineData(null, false, 0)]
        [InlineData(0, false, 0)]
        [InlineData(1, false, 1)]
        [InlineData("DC02", false, 1)]
        [InlineData("dc02", true, 1)]
        [InlineData("1", false, 2)]
        public void BaselineSelector_UsesOneOptionForDefaultIndexOrName(object? baseline, bool caseSensitive, int expected) {
            object[] objects = { new { Port = 80 }, new { Port = 443 }, new { Port = 389 } };
            var options = new DiffOptions { Baseline = baseline, CaseSensitive = caseSensitive, IncludeUnchanged = true };
            BaselineComparison comparison = ObjectDiff.CompareBaseline(objects, options, new[] { "dc01", "dc02", "1" });
            Assert.Equal(expected, comparison.BaselineIndex);
            BaselineRow row = Assert.Single(comparison.Rows);
            Assert.Equal(ChangeKind.Unchanged, row.Cells[expected].Kind);
            Assert.Equal(2, row.Cells.Count(cell => cell.Kind == ChangeKind.Changed));
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(3, false)]
        [InlineData("missing", false)]
        [InlineData("DC02", true)]
        [InlineData("dc01", false)]
        [InlineData(1L, false)]
        [InlineData(true, false)]
        public void BaselineSelector_RejectsInvalidIndexesNamesAndKinds(object baseline, bool caseSensitive) {
            object[] objects = { new { Port = 80 }, new { Port = 443 }, new { Port = 389 } };
            var options = new DiffOptions { Baseline = baseline, CaseSensitive = caseSensitive };
            ArgumentException error = Assert.ThrowsAny<ArgumentException>(() =>
                ObjectDiff.CompareBaseline(objects, options, new[] { "dc01", "DC01", "dc02" }));
            Assert.Equal("Baseline", error.ParamName);
        }
    }
}
