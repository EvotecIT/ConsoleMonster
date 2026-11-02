using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Changes;
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
                new Dictionary<string, object?> { ["Name"] = "dc01", ["Port"] = 80, ["Site"] = "HQ", ["Optional"] = "", ["EmptyRecord"] = new Dictionary<string, object?>() },
                new Dictionary<string, object?> { ["Name"] = "dc02", ["Port"] = 443, ["Site"] = "HQ", ["Optional"] = null },
                new Dictionary<string, object?> { ["Name"] = "dc03", ["Port"] = 443, ["Site"] = "HQ" }
            };
            var options = new DiffOptions();
            options.Ignore.Add("Name");
            BaselineComparison comparison = ObjectDiff.CompareBaseline(objects, options, baselineIndex: 1);
            Assert.Equal(new[] { "dc01", "dc02", "dc03" }, comparison.Names);
            Assert.Equal(1, comparison.BaselineIndex);
            Assert.DoesNotContain(comparison.Rows, row => row.Path == "Site");
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
            string compact = TestTerminal.RenderText(new BaselineView(comparison), 48, 0, GlyphTier.Ascii);
            Assert.All(compact.Split('\n'), line => Assert.True(line.Length <= 48));
            options.IncludeUnchanged = true;
            Assert.Contains(ObjectDiff.CompareBaseline(objects, options).Rows, row => row.Path == "Site" && !row.HasDifferences);
            Assert.Throws<ArgumentOutOfRangeException>(() => ObjectDiff.CompareBaseline(objects, baselineIndex: 3));
        }
    }
}
