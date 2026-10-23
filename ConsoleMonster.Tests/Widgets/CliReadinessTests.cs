using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Help;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class CliReadinessTests {
        [Fact]
        public void CommandLineHelp_ReusedViewUsesRenderLanguageAndVisibility() {
            HelpTopic topic = new HelpTopic("tool") {
                CommandLine = new CommandLineHelp { Usage = "tool [command] [options]" }
            };
            topic.CommandLine.Commands.Add(new HelpCommand("inspect", "Inspect saved data."));
            topic.CommandLine.Values.Add(new HelpValue("inventory", "Read saved inventory."));
            topic.Parameters.Add(new HelpParameter("output") {
                Type = "path", Required = true, Default = "report.html", Description = "Write a report."
            });
            topic.Examples.Add(new HelpExample(null, "tool inspect --output report.html"));
            HelpView view = new HelpView(topic);

            string english = Render(view, "en");
            Assert.Contains("Usage", english);
            Assert.Contains("Commands", english);
            string polish = Render(view, "pl");
            foreach (string label in new[] { "Użycie", "Polecenia", "Wartości", "Opcje", "Przykłady", "Wymagane." }) {
                Assert.Contains(label, polish);
            }
            Assert.Contains("[domyślnie: report.html]", Regex.Replace(polish, @"\s+", " "));
            Assert.DoesNotContain("Usage", polish);
            view.ShowParameters = false;
            view.ShowExamples = false;
            string hidden = Render(view, "pl");
            Assert.DoesNotContain("Opcje", hidden);
            Assert.DoesNotContain("Przykłady", hidden);
            Assert.Contains("inspect", hidden);
        }

        [Fact]
        public void KeepAllColumns_BandsPreserveSpanningGroupSections() {
            Table table = new Table {
                KeepAllColumns = true, WrapHeaders = true, RecordViewRows = 0,
                WrapLines = 0, GroupHeadersAsRows = true
            };
            table.AddColumn(new TableColumn("Authority") { IsKey = true, MaxWidth = 12 });
            table.AddColumn(new TableColumn("First observation") { MaxWidth = 17 });
            table.AddColumn(new TableColumn("Second observation") { MaxWidth = 18 });
            table.AddGroup("Directory");
            table.AddValues("node-1", "First complete value", "Second complete value");
            string text = Terminal.RenderText(table,
                TerminalProfile.Fixed(40, 100, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            string[] sections = text.Split('\n').Where(line => line.Contains("Directory")).ToArray();
            Assert.True(sections.Length >= 2, "Each column band must retain its group heading.");
            Assert.All(sections, line => {
                Assert.Equal(2, line.Count(character => character == '│'));
                Assert.DoesNotContain("─", line);
                Assert.Contains("· 1", line);
            });
            Assert.Contains("First complete", text);
            Assert.Contains("Second complete", text);
        }

        [Fact]
        public void WrappedHierarchy_UsesRenderLanguageForGeneratedLabelsAndCycleNotes() {
            string? previous = Environment.GetEnvironmentVariable("CONSOLEMONSTER_LANGUAGE");
            try {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_LANGUAGE", "en");
                HierarchyItem item = new HierarchyItem("Root");
                item.Children.Add(item);
                item.Children.Add(new HierarchyItem("Hidden"));
                Tree tree = new Tree { WrapLabels = true };
                TreeHierarchy.Add(tree.Root, new[] { item }, value => new TreeNode(value.Name),
                    value => value.Children, new TreeHierarchyOptions { MaxItems = 1 });
                TerminalProfile profile = TerminalProfile.Fixed(60, 40, ColorDepth.None, GlyphTier.Full);
                Assert.Contains("+1 more", Terminal.RenderText(tree, profile.WithLanguage(CultureInfo.GetCultureInfo("en")), Theme.Dark));
                string polish = Terminal.RenderText(tree, profile.WithLanguage(CultureInfo.GetCultureInfo("pl")), Theme.Dark);
                Assert.Contains("+1 więcej", polish);
                Assert.Contains("(cykl)", polish);
                Assert.DoesNotContain("(cycle)", polish);
                Assert.DoesNotContain("+1 more", polish);
            } finally {
                Environment.SetEnvironmentVariable("CONSOLEMONSTER_LANGUAGE", previous);
            }
        }

        private sealed class HierarchyItem {
            public HierarchyItem(string name) { Name = name; }
            public string Name { get; }
            public System.Collections.Generic.List<HierarchyItem> Children { get; } = new System.Collections.Generic.List<HierarchyItem>();
        }

        private static string Render(HelpView view, string language) => Terminal.RenderText(view,
            TerminalProfile.Fixed(120, 100, ColorDepth.None, GlyphTier.Full)
                .WithLanguage(CultureInfo.GetCultureInfo(language)), Theme.Dark);
    }
}
