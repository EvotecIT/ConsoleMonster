using System;
using System.IO;
using System.Xml.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using SiteFrames;
using SiteFrames.Capture;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>The shown C# programs compile as a reader's project would, and only clean programs make frames.</summary>
    [Collection("Interactive process defaults")]
    public sealed class ProgramRunnerContracts : IDisposable {
        private readonly string _folder = Path.Combine(Path.GetTempPath(), "siteframes-programs-" + Guid.NewGuid().ToString("N"));

        public ProgramRunnerContracts() => Directory.CreateDirectory(_folder);

        public void Dispose() => Directory.Delete(_folder, true);

        private string Program(string name, string code) {
            string path = Path.Combine(_folder, name + ".cs");
            File.WriteAllText(path, code);
            return path;
        }

        [Fact]
        public void CompactErrorProgramCompilesAndExportsAllReviewSizesAndThemes() {
            // CSharpRunner owns process-wide console writers. The collection also keeps unrelated README diagnostics
            // from being captured as stray program output while these executable examples run.
            string directory = Path.Combine(_folder, "error-frames");
            Site site = new Site(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..")));
            ErrorFigures.ExportReview(site, directory);
            Assert.Equal(4, Directory.GetFiles(directory, "*.html").Length);
            Assert.Equal(4, Directory.GetFiles(directory, "*.txt").Length);
            string[] exported = Directory.GetFiles(directory, "*.svg");
            Assert.Equal(4, exported.Length);
            foreach (string file in exported) {
                XDocument svg = XDocument.Load(file);
                XNamespace ns = "http://www.w3.org/2000/svg";
                Assert.Equal(ns + "svg", svg.Root!.Name);
                Assert.Contains(svg.Descendants(ns + "text"), text => text.Value.Contains("^^"));
            }
            foreach (int width in new[] { 60, 120 }) {
                string text = ErrorFigures.Run(site, width, Theme.Dark).ToPlainText();
                Assert.StartsWith("Get-Item: Cannot load the inventory", text, StringComparison.Ordinal);
                Assert.Contains("deploy.ps1:12:1", text, StringComparison.Ordinal);
                Assert.Contains("+2 inner exceptions · Get-Error for details", text, StringComparison.Ordinal);
                Assert.Contains("ObjectNotFound", text, StringComparison.Ordinal);
                Assert.Contains("./inventory.json", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Caused by", text, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Run_CleanProgram_PrintsItsWidget() {
            string path = Program("rule", "using ConsoleMonster;\nusing ConsoleMonster.Widgets;\n\nTerminal.Write(new Rule(\"Hello\"));\n");
            string text = CSharpRunner.Run(path, 30, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark).GetRowText(0);
            Assert.Contains("Hello", text, StringComparison.Ordinal);
        }

        [Fact]
        public void Run_ImplicitUsings_AreAvailableAsInDotnetNewConsole() {
            string path = Program("implicit", "using ConsoleMonster;\nusing ConsoleMonster.Widgets;\n\nList<string> names = new() { \"a\" };\nTerminal.Write(new Rule(names.First()));\n");
            Assert.Contains("a", CSharpRunner.Run(path, 20, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark).GetRowText(0), StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("unused", "using ConsoleMonster;\nusing ConsoleMonster.Layout;\nusing ConsoleMonster.Widgets;\n\nTerminal.Write(new Rule());\n")]
        [InlineData("warning", "using ConsoleMonster;\nusing ConsoleMonster.Widgets;\n\nint unused;\nTerminal.Write(new Rule());\n")]
        [InlineData("examples", "using ConsoleMonster;\n\nTerminal.Write(ConsoleMonster.Examples.Showcases.ToolboxData.TaskPreview(null!));\n")]
        public void Compile_ProgramAReaderCouldNotBuildCleanly_Throws(string name, string code) {
            Assert.Throws<InvalidOperationException>(() => CSharpRunner.Compile(Program(name, code)));
        }

        [Theory]
        [InlineData("throws", "using ConsoleMonster;\nusing ConsoleMonster.Widgets;\n\nTerminal.Write(new Rule());\nthrow new InvalidOperationException(\"boom\");\n")]
        [InlineData("console", "using ConsoleMonster;\nusing ConsoleMonster.Widgets;\n\nTerminal.Write(new Rule());\nConsole.WriteLine(\"stray\");\n")]
        public void Run_ProgramThatFailsOrBypassesTheTerminal_Throws(string name, string code) {
            Assert.Throws<InvalidOperationException>(() => CSharpRunner.Run(Program(name, code), 30, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark));
        }

        [Fact]
        public void Execute_FileArguments_ReachTheCompleteProgram_AndDefaultToEmpty() {
            string data = Path.Combine(_folder, "a theme file.txt");
            File.WriteAllText(data, "Loaded team palette");
            string path = Program("arguments", "using ConsoleMonster;\nusing ConsoleMonster.Widgets;\n\nTerminal.Write(new TextBlock(args.Length == 0 ? \"Current palette\" : File.ReadAllText(args[0])));\n");
            var profile = CSharpRunner.Profile(40, 10, ColorDepth.TrueColor, GlyphTier.Full);
            Assert.Contains("Loaded team palette", CSharpRunner.Execute(path, profile, Theme.Dark, null, new[] { data }).Output, StringComparison.Ordinal);
            Assert.Contains("Current palette", CSharpRunner.Execute(path, profile, Theme.Dark, null).Output, StringComparison.Ordinal);
        }

        [Fact]
        public void Program_ShortForm_LeadsWithStatements() {
            (string shortForm, string full) = SourceSnippets.Program("using ConsoleMonster;\n\nTerminal.Write(x);");
            Assert.Equal("Terminal.Write(x);", shortForm);
            Assert.Equal("using ConsoleMonster;\n\nTerminal.Write(x);", full);
            Assert.Equal("Write-ConsoleRule", SourceSnippets.Program("Import-Module ConsoleMonster\n\nWrite-ConsoleRule").Short);
            Assert.Equal("Buffer stream = new();", SourceSnippets.Program("global using System.IO;\nusing Buffer = System.IO.MemoryStream;\n\nBuffer stream = new();").Short);
        }

        [Theory]
        [InlineData("using (var stream = new MemoryStream()) { stream.WriteByte(1); }")]
        [InlineData("using MemoryStream stream = new MemoryStream();")]
        [InlineData("using var stream = new MemoryStream();")]
        public void Program_ShortForm_PreservesResourceLifetimeStatements(string statement) {
            string program = "using System.IO;\n\n" + statement;
            (string shortForm, string full) = SourceSnippets.Program(program);
            Assert.Equal(statement, shortForm);
            Assert.Equal(program, full);
        }
    }
}
