using System;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames;
using SiteFrames.Capture;
using SiteFrames.Pages;
using System.Text.Json.Nodes;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Generated tabs remain runnable translations and preserve observable initializer behavior.</summary>
    [Collection("Interactive process defaults")]
    public sealed class FluentSourceContracts : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "cm-fluent-source-" + Guid.NewGuid().ToString("N"));
        public FluentSourceContracts() => Directory.CreateDirectory(_folder);
        public void Dispose() => Directory.Delete(_folder, true);

        [Theory]
        [InlineData("example")]
        [InlineData("more")]
        [InlineData("hidden")]
        public void FluentChainsKeepRegionAndOrdinaryCommentsBetweenStatements(string region) {
            string path = Source($$"""
                using ConsoleMonster.Interactive;
                var menu = new Menu("Fleet");
                // <{{region}}>
                menu.Item("First", item => item.Description("First item"));
                // Explain the next item.
                menu.Item("Second", item => item.Description("Second item"));
                menu.Separator();
                menu.Exit("Quit");
                // </{{region}}>
                // Keep the fixed subtitle after the example.
                menu.Subtitle = "Fixed time";
                """);

            string fluent = CSharpRunner.FluentSource(path);
            CSharpRunner.Check(path, fluent);
            string[] ordered = { "// <" + region + ">", ".Item(\"First\"", "// Explain the next item.",
                ".Item(\"Second\"", ".Separator()", ".Exit(\"Quit\")", "// </" + region + ">",
                "// Keep the fixed subtitle after the example.", ".Subtitle(\"Fixed time\")" };
            int position = -1;
            foreach (string text in ordered) {
                int next = fluent.IndexOf(text, position + 1, StringComparison.Ordinal);
                Assert.True(next > position, $"Missing or misplaced '{text}' in:\n{fluent}");
                position = next;
            }
            Assert.Contains("menu.Item(\"First\"", fluent, StringComparison.Ordinal);
            Assert.Contains("menu.Subtitle(\"Fixed time\")", fluent, StringComparison.Ordinal);
            if (region != "example") {
                var snippets = SourceSnippets.Program(fluent);
                Assert.DoesNotContain("// <" + region + ">", snippets.Full, StringComparison.Ordinal);
                Assert.Contains(".Exit(\"Quit\")", snippets.Full, StringComparison.Ordinal);
                Assert.Contains(".Subtitle(\"Fixed time\")", snippets.Short, StringComparison.Ordinal);
            }
        }

        [Theory]
        [InlineData("int n = 0; Terminal.Write(new Rows(new TextBlock((++n).ToString()), new TextBlock(\"B\")) { Gap = n });")]
        [InlineData("Terminal.Write(new Panel(new TextBlock(\"Ready\"), ConsoleMonster.Text.RichText.Plain(\"Build\")));")]
        [InlineData("Terminal.Write(new Rows(new TextBlock(markup: \"Ready\"), new Rule(ConsoleMonster.Text.RichText.Plain(\"Build\"))));")]
        [InlineData("string? title = null; Terminal.Write(new Rows(new Rule(title)));")]
        [InlineData("var rows = new Rows(new ConsoleMonster.Interactive.AppBinding(state => new Panel(new TextBlock(\"Inner\"), \"Build\"))); if (rows.Children[0] is not ConsoleMonster.Interactive.AppBinding) throw new System.InvalidOperationException(\"Lost binding\"); Terminal.Write(new TextBlock(\"Binding retained\"));")]
        [InlineData("Terminal.Write(new Rows().Content(c => c.Add(new Panel(new TextBlock(\"Fill\"), \"Build\").Fill(2))));")]
        [InlineData("Terminal.Write(new Rows(new Panel(new Rows(new Panel(new TextBlock(\"Nested\"), \"Inner\")), \"Outer\")));")]
        [InlineData("string cText = \"Text\"; Terminal.Write(new Rows(new TextBlock(cText)));")]
        [InlineData("Terminal.Write(new Rows(new ProgressBar(\"Build\", \"build\") { Value = 5, Total = 10 }));")]
        [InlineData("Terminal.Write(new ProgressBar(\"Build\").CountColumn(column => column.MinWidth(7)));")]
        [InlineData("Terminal.Write(new Rows().Content(c => c.Add(new ProgressBar())));")]
        [InlineData("var command = new ConsoleMonster.Interactive.AppCommand(\"run\", \"Run\", (_, __) => System.Threading.Tasks.Task.CompletedTask); var button = new ConsoleMonster.Interactive.Button(command); if (button.Key != \"run\") throw new System.InvalidOperationException(\"Lost command key\"); Terminal.Write(new Rows(button, new TextBlock(\"Command\")));")]
        [InlineData("var prompt = new ConsoleMonster.Interactive.SelectPrompt<int>(\"Pick\", new[] { 1 }, group: _ => \"Group\"); if (prompt.Choices[0].Group != \"Group\" || prompt.Choices[0].Label != \"1\") throw new System.InvalidOperationException(\"Lost grouping\"); Terminal.Write(new Rows(new TextBlock(\"Grouped\")));")]
        [InlineData("var prompt = new ConsoleMonster.Interactive.MultiSelectPrompt<int>(\"Pick\", new[] { 1 }, group: _ => \"Group\"); if (prompt.Choices[0].Group != \"Group\" || prompt.Choices[0].Label != \"1\") throw new System.InvalidOperationException(\"Lost grouping\"); Terminal.Write(new Rows(new TextBlock(\"Grouped\")));")]
        [InlineData("int n = 0; IWidget child = new TextBlock(\"Child\"); var dashboard = new Dashboard((++n).ToString(), child, (++n).ToString()); if (dashboard.Title != \"1\" || dashboard.WidgetId != \"2\") throw new System.InvalidOperationException(\"Reordered identity\"); Terminal.Write(new Rows(new TextBlock(\"Order retained\")));")]
        [InlineData("var data = new ConsoleMonster.Dates.CalendarData(); var calendar = new Calendar(2026, 10, data); if (!object.ReferenceEquals(calendar.Data, data)) throw new System.InvalidOperationException(\"Copied shared model\"); Terminal.Write(new Rows(calendar));")]
        [InlineData("string trace = \"\"; Func<int, string> Display() { trace += \"d\"; return _ => \"Label\"; } Func<int, string?> Group() { trace += \"g\"; return _ => \"Group\"; } var prompt = new ConsoleMonster.Interactive.SelectPrompt<int>(\"Pick\", new[] { 1 }, group: Group(), display: Display()); if (trace != \"gd\") throw new System.InvalidOperationException(trace); Terminal.Write(new Rows(new TextBlock(prompt.Choices[0].Label)));")]
        [InlineData("IWidget? child = null; Terminal.Write(new Rows(new Panel(child, \"Empty\"), new TextBlock(\"Empty\")));")]
        [InlineData("IWidget[]? children = null; Terminal.Write(new Rows(new Rows(children!), new TextBlock(\"Empty\")));")]
        [InlineData("int columns = 2; Terminal.Write(new Grid(columns, TextBlock.Plain(\"First\"), TextBlock.Plain(\"Second\")));")]
        [InlineData("Terminal.Write(new Rows(new Calendar(2026, 10, marks: null)));")]
        [InlineData("System.Collections.Generic.IEnumerable<ConsoleMonster.Dates.CalendarMark>? marks = null; Terminal.Write(new Rows(new Calendar(2026, 10, marks)));")]
        [InlineData("string MakeTitle(out string title) { title = \"Title\"; return title; } string MakeId() => \"id\"; var dashboard = new Dashboard(MakeTitle(out var title), new TextBlock(\"Child\"), MakeId()); Terminal.Write(new Rows(new TextBlock(title)));")]
        [InlineData("string Make(out string text) { text = \"Scope\"; return text; } var rows = new Rows(new TextBlock(Make(out var text))); Terminal.Write(new Rows(rows, new TextBlock(text)));")]
        [InlineData("var tree = new Tree(\"Root\"); var parent = tree.Add(\"Parent\"); parent.Add(\"First\"); parent.Add(\"Second\").Label(\"Last\"); Terminal.Write(new Rows(tree));")]
        public void ContainerTranslationPreservesEvaluationAndSelectedOverloads(string body) {
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\n" + body);
            string fluent = CSharpRunner.FluentSource(path);
            try { CSharpRunner.Check(path, fluent); } catch (InvalidOperationException error) { throw new InvalidOperationException(fluent, error); }
            var profile = CSharpRunner.Profile(40, 20, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(CSharpRunner.Execute(path, profile, Theme.Dark, null).Output, CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void ContainerConstructorsBecomeNestedContentBuildersAndBooleanSwitches() {
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\nvar table = new Table { Expand = true };\ntable.AddColumn(\"Build\").AddRow(\"Passed\");\nvar report = new Rows(new Rule(\"Nightly\"), new Panel(table, \"Jobs\"), new TextBlock(\"Ready\")) { Gap = 1 };\nTerminal.Write(report);\n");
            string fluent = CSharpRunner.FluentSource(path);
            Assert.Contains(".Expand()", fluent, StringComparison.Ordinal);
            Assert.Contains(".Content(c =>", fluent, StringComparison.Ordinal);
            Assert.Contains(".Rule(\"Nightly\")", fluent, StringComparison.Ordinal);
            Assert.Contains(".Markup(\"Ready\")", fluent, StringComparison.Ordinal);
            CSharpRunner.Check(path, fluent);
            var profile = CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(CSharpRunner.Execute(path, profile, Theme.Dark, null).Output, CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void FluentConfigurationStaysChainedAndOperationCallbacksKeepDefaultsAndExecutionOrder() {
            string path = Source("""
                using ConsoleMonster;
                using ConsoleMonster.Live;
                using ConsoleMonster.Layout;
                using ConsoleMonster.Widgets;
                var table = new Table();
                table.Expand();
                table.AddColumn("Build").AddRow("Passed");
                var rows = new Rows(table, new TextBlock("[bold]Ready[/]"));
                var result = await ConsoleRun.ForEachAsync(new[] { 1 }, (item, _) => Task.FromResult(item), new RunOptions { ThrottleLimit = 2, Title = "Run", Live = false, Print = _ => { } });
                if (result.Count != 1) throw new System.InvalidOperationException("Lost operation");
                Terminal.Write(rows);
                """);
            string fluent = CSharpRunner.FluentSource(path);
            CSharpRunner.Check(path, fluent);
            Assert.Contains("Ui.Table().Expand().AddColumn", fluent, StringComparison.Ordinal);
            Assert.DoesNotContain("table.Expand", fluent, StringComparison.Ordinal);
            Assert.Contains("options =>", fluent, StringComparison.Ordinal);
            Assert.DoesNotContain("configure:", fluent, StringComparison.Ordinal);
            Assert.Contains("options.ThrottleLimit(2).Title(\"Run\").Live(false)",
                Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseCompilationUnit(fluent).NormalizeWhitespace().ToFullString(), StringComparison.Ordinal);
            Assert.DoesNotContain("Ui.Options", fluent, StringComparison.Ordinal);
            Assert.Contains(".Markup(\"[bold]Ready[/]\")", fluent, StringComparison.Ordinal);
            var profile = CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(CSharpRunner.Execute(path, profile, Theme.Dark, null).Output, CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Theory]
        [InlineData("ReadTitle()", ", Next()", "TC")]
        [InlineData("await Task.FromResult(ReadTitle())", "", "T")]
        public void OperationTranslationRetainsCallerArgumentAndAwaitBoundaries(string title, string trailing, string expected) {
            string next = trailing.Length == 0 ? string.Empty : "System.Threading.CancellationToken Next() { trace += \"C\"; return default; }";
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Live;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\n"
                + "string trace = \"\"; string ReadTitle() { trace += \"T\"; return \"Run\"; } " + next
                + " var result = await ConsoleRun.ForEachAsync(new[] { 1 }, (item, _) => Task.FromResult(item), new RunOptions { Title = " + title + ", Live = false, Print = _ => { } }" + trailing + ");"
                + " if (trace != \"" + expected + "\" || result.Count != 1) throw new System.InvalidOperationException(trace); Terminal.Write(new Rows(new TextBlock(trace)));\n");
            string fluent = CSharpRunner.FluentSource(path);
            CSharpRunner.Check(path, fluent);
            Assert.Contains("new RunOptions()", fluent, StringComparison.Ordinal);
            var profile = CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(CSharpRunner.Execute(path, profile, Theme.Dark, null).Output, CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Theory]
        [InlineData("app")]
        [InlineData("export")]
        public void PositionalConfigureCallbacksPreserveStateAndFormatEvaluationOrder(string operation) {
            string output = Path.Combine(_folder, "export.txt").Replace("\\", "\\\\", StringComparison.Ordinal);
            string body = operation == "app" ? """
                IWidget Layout() { trace += "L"; return new Rows(new TextBlock("Ready")); }
                AppState State() { trace += "S"; return new AppState(); }
                int Width() { trace += "W"; return 60; }
                ConsoleApp.Run(Layout(), State(), new ConsoleAppOptions { StaticSnapshot = true, ReflowBelow = Width() });
                if (trace != "LSW") throw new InvalidOperationException(trace);
                """ : $$"""
                string PathFor() { trace += "P"; return "{{output}}"; }
                string FormatFor() { trace += "F"; return "Text"; }
                int Width() { trace += "W"; return 60; }
                new TextBlock("Ready").Export(PathFor(), FormatFor(), new ExportOptions { Width = Width() });
                if (trace != "PFW" || !File.ReadAllText("{{output}}").Contains("Ready")) throw new InvalidOperationException(trace);
                File.Delete("{{output}}");
                """;
            string imports = "using ConsoleMonster;\nusing ConsoleMonster.Widgets;\n"
                + (operation == "app" ? "using ConsoleMonster.Layout;\nusing ConsoleMonster.Interactive;\n" : "using ConsoleMonster.Export;\n");
            string path = Source(imports + "string trace = \"\";\n" + body + "\nTerminal.Write(new TextBlock(trace));\n");
            string fluent = CSharpRunner.FluentSource(path);
            CSharpRunner.Check(path, fluent);
            Assert.DoesNotContain(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(fluent).GetRoot()
                .DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax>(), argument => argument.NameColon != null);
            Assert.Contains("options =>", fluent, StringComparison.Ordinal);
            var profile = CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(CSharpRunner.Execute(path, profile, Theme.Dark, null).Output,
                CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void ContentArraysKeepEachChildAndTheOuterGap() {
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\nIWidget[] children = { new TextBlock(\"First\"), new TextBlock(\"Second\") };\nTerminal.Write(new Rows(children) { Gap = 2 });\n");
            string fluent = CSharpRunner.FluentSource(path);
            CSharpRunner.Check(path, fluent);
            var profile = CSharpRunner.Profile(40, 20, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(CSharpRunner.Execute(path, profile, Theme.Dark, null).Output, CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void ExplicitContentInitializerStillOverridesConstructorContent() {
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\nTerminal.Write(new Panel(new TextBlock(\"Old\"), \"Build\") { Content = new TextBlock(\"Current\") });\n");
            string fluent = CSharpRunner.FluentSource(path);
            CSharpRunner.Check(path, fluent);
            var profile = CSharpRunner.Profile(40, 20, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Equal(CSharpRunner.Execute(path, profile, Theme.Dark, null).Output, CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void ImplicitCreationAndNestedInitializersBecomeCompilableChainsWithTheSameOutput() {
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\nPanel panel = new() { Title = \"Build\", Content = new TextBlock(\"Passed\") };\nTerminal.Write(panel);\n");
            string fluent = CSharpRunner.FluentSource(path);
            Assert.Contains(".Title(\"Build\")", fluent, StringComparison.Ordinal);
            Assert.Contains(".Content(", fluent, StringComparison.Ordinal);
            Assert.Contains("panel.Show()", fluent, StringComparison.Ordinal);
            CSharpRunner.Check(path, fluent);
            Assert.Equal(CSharpRunner.Execute(path, CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark, null).Output,
                CSharpRunner.Execute(path, CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full), Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void MixedInitializersKeepCallbackAndPropertyOrder() {
            string path = Source("using ConsoleMonster.Interactive;\nvar item = new MenuItem(\"Build\") { Detail = () => \"Ready\", Action = _ => { }, Description = \"Run\" };\n");
            string fluent = CSharpRunner.FluentSource(path);
            Assert.Contains("Detail = () => \"Ready\"", fluent, StringComparison.Ordinal);
            Assert.Contains("Action = _ =>", fluent, StringComparison.Ordinal);
            Assert.Contains(".Configure(", fluent, StringComparison.Ordinal);
            Assert.True(fluent.IndexOf("Detail =", StringComparison.Ordinal) < fluent.IndexOf("Action =", StringComparison.Ordinal));
            Assert.True(fluent.IndexOf("Action =", StringComparison.Ordinal) < fluent.IndexOf(".Description(", StringComparison.Ordinal));
            Assert.DoesNotContain(".Detail(\"Ready\")", fluent, StringComparison.Ordinal);
            CSharpRunner.Check(path, fluent);
        }

        [Fact]
        public void NestedCallbacksCompileWithoutCapturingEachOthersConfigurationParameter() {
            string path = Source("using ConsoleMonster;\nusing ConsoleMonster.Interactive;\nusing ConsoleMonster.Widgets;\nvar item = new MenuItem(\"Build\") { Action = context => { var child = new MenuItem(\"Child\") { Action = childContext => Terminal.Write(new TextBlock(\"Ran\")) }; child.Action!(null!); } };\nitem.Action!(null!);\n");
            string fluent = CSharpRunner.FluentSource(path);
            CSharpRunner.Check(path, fluent);
            var profile = CSharpRunner.Profile(60, 20, ColorDepth.TrueColor, GlyphTier.Full);
            string original = CSharpRunner.Execute(path, profile, Theme.Dark, null).Output;
            Assert.Contains("Ran", original, StringComparison.Ordinal);
            Assert.Equal(original, CSharpRunner.Execute(path, profile, Theme.Dark, null, fluent: true).Output);
        }

        [Fact]
        public void WidgetProgramsHaveCompilableFluentTabs() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            foreach (string path in Directory.GetFiles(Path.Combine(website, "examples", "widgets"), "*.cs")) {
                CSharpRunner.Check(path, CSharpRunner.FluentSource(path));
            }
        }

        [Fact]
        public void StandardProgramsHaveCompiledTranslationsAndResolvedMappingNotes() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            foreach (string folder in new[] { "widgets", "home", "tables", "docs", "errors", "themes", "interactive", "app", "tails", "recording", "migration" }) {
                foreach (string path in Directory.GetFiles(Path.Combine(website, "examples", folder), "*.cs")) {
                    var entry = SourceStyles.Add(site, new JsonObject(), path);
                    Assert.Equal(Path.GetRelativePath(site.Repository, path).Replace('\\', '/'), entry["csharpRepo"]!.GetValue<string>());
                    Assert.False(string.IsNullOrWhiteSpace(entry["csharpFluent"]!.GetValue<string>()));
                    Assert.NotEmpty(entry["apiMap"]!.AsArray());
                }
            }
        }

        [Fact]
        public void HostedPowerShellExamplesCompileAgainstTheirActualModuleOwnersInBothCSharpStyles() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            foreach (string path in Directory.GetFiles(Path.Combine(website, "examples", "demos"), "*.host.cs")) {
                CSharpRunner.Check(path, Site.ReadText(path));
                Assert.StartsWith("Invoke-", CSharpRunner.HostCommand(path), StringComparison.Ordinal);
                var entry = SourceStyles.Add(site, new JsonObject(), path);
                Assert.NotEmpty(entry["apiMap"]!.AsArray());
            }
        }

        [Fact]
        public void MultiFileExamplesPublishCompiledFluentCompanions() {
            string main = Source("using ConsoleMonster;\nTerminal.Write(Factory.Build());\n");
            string companion = Source("using ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\nstatic class Factory { public static Panel Build() => new Panel { Title = \"Build\", Content = new TextBlock(\"Passed\") }; }\n");
            var sources = CSharpRunner.FluentSources(main, companion);
            CSharpRunner.CheckSources(main, sources, companion);
            Assert.Contains(".Title(\"Build\")", sources[companion], StringComparison.Ordinal);
            Assert.DoesNotContain("Title =", sources[companion], StringComparison.Ordinal);
        }

        [Fact]
        public void StandaloneDemosAndTheFleetProgramHaveCompiledThreeStyleSources() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            foreach (string file in new[] { "watch", "errors", "login", "parallel", "threads-naive", "threads-live", "record", "session" }) {
                string path = Path.Combine(website, "examples", "demos", file + ".cs");
                CSharpRunner.Check(path, Site.ReadText(path));
                SourceStyles.Add(site, new JsonObject(), path);
            }
            SourceStyles.Add(site, new JsonObject(), Path.Combine(website, "examples", "demos", "fleet.cs"),
                Path.Combine(site.Repository, "ConsoleMonster.Examples", "Showcases", "FleetHealth.cs"),
                Path.Combine(site.Repository, "ConsoleMonster.Examples", "Showcases", "FleetData.cs"));
        }

        [Fact]
        public void IntegrationProjectsCompileBothStylesAndDocumentEveryAddonOption() {
            string website = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
            Site site = new Site(website);
            var map = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Repository, "Tests", "Api", "round2-parity.json")))!;
            foreach (var (folder, projectName) in new[] { ("hosting", "Orders"), ("logging", "Shop") }) {
                string source = Path.Combine(website, "examples", "demos", folder, "Program.cs");
                string project = Path.Combine(website, "examples", "demos", folder, projectName + ".csproj");
                SourceStyles.AddProject(site, new JsonObject(), source, project);
                foreach (var option in map["IntegrationOptions"]!.AsArray()) {
                    if (option!["Package"]!.GetValue<string>() != (folder == "hosting" ? "ConsoleMonster.Hosting" : "ConsoleMonster.Extensions.Logging")) { continue; }
                    var actual = CSharpRunner.ProjectOptionMembers(source, project, option["CSharpType"]!.GetValue<string>());
                    Assert.Equal(option["Members"]!.AsArray().Select(member => member!.GetValue<string>()).OrderBy(name => name, StringComparer.Ordinal), actual);
                    Assert.False(string.IsNullOrWhiteSpace(option["Reason"]!.GetValue<string>()));
                }
            }
            SourceStyles.AddProject(site, new JsonObject(),
                Path.Combine(site.Repository, "Templates", "ConsoleMonster.Templates", "content", "ConsoleMonsterApp", "Program.cs"),
                Path.Combine(website, "tools", "TemplateDemo", "TemplateDemo.csproj"));
        }

        private string Source(string text) {
            string path = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".cs");
            File.WriteAllText(path, text);
            return path;
        }
    }
}
