using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Testing;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Highlighting {
    /// <summary>The highlighting engine: token goldens per language, smart values, detection, safety and the code view.</summary>
    public sealed class HighlighterTests {
        private static readonly Dictionary<string, string> Samples = new Dictionary<string, string> {
            ["json"] = "{\n  \"name\": \"ConsoleMonster\",\n  \"version\": 2.0,\n  \"tags\": [\"console\", \"tui\"],\n  \"stable\": false,\n  \"license\": null,\n  oops\n}",
            ["powershell"] = "# Deploy the agent\nfunction Publish-Agent {\n    param([string] $Path = 'C:\\Agent', [int] $Retries = 3)\n    <# block #>\n    if ($Retries -gt 0 -and $env:CI) {\n        Get-ChildItem -Path $Path | Where-Object { $_.Length -gt 10MB }\n        Write-Host \"Copied $Path to `\"$Target`\"\" -ForegroundColor Green\n    }\n    return $true\n}",
            ["xml"] = "<?xml version=\"1.0\"?>\n<!-- settings -->\n<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net8.0</TargetFramework>\n    <Note><![CDATA[a < b]]> &amp; more</Note>\n  </PropertyGroup>\n</Project>",
            ["yaml"] = "---\n# pipeline\nname: build\non: [push]\njobs:\n  test:\n    runs-on: windows-latest\n    retries: 3\n    enabled: true  # comment\n    steps:\n      - uses: actions/checkout@v4\n      - run: \"dotnet test\"\n    base: &defaults\n    copy: *defaults",
            ["log"] = "2026-10-17 14:03:11.204 INFO  [Agent] Started on 10.0.0.12 in 412ms\n2026-10-17 14:03:12.010 WARN  [Disk] C:\\Data is 91% full\n2026-10-17 14:03:13.555 ERROR [Http] GET https://example.com/api failed: \"timeout\"\n14:03:14 DEBUG id=3f2504e0-4f89-11d3-9a0c-0305e82c3301 ok=true",
            ["diff"] = "diff --git a/app.ps1 b/app.ps1\nindex 3b18e51..a9c2f04 100644\n--- a/app.ps1\n+++ b/app.ps1\n@@ -1,4 +1,4 @@ function Start-App\n Import-Module App\n-Start-App -Port 80\n+Start-App -Port 8080\n \\ No newline at end of file",
            ["csv"] = "Name,Count,Status\nalpha,7,pass\n\"beta, gamma\",1234,fail",
            ["csharp"] = "// entry point\npublic static int Main(string[] args) {\n    var count = args.Length * 2; /* twice */\n    string name = $\"run {count}\";\n    return count > 0 ? 0 : 1;\n}",
            ["sql"] = "-- active users\nSELECT u.[Name], COUNT(*) AS Logins\nFROM dbo.Users u\nWHERE u.Active = 1 AND u.Created > '2026-01-01' AND @Region IS NOT NULL\nGROUP BY u.[Name];",
            ["ini"] = "; settings\n[server]\nhost = \"example.local\"\nport = 8080\nsecure = true",
            ["markdown"] = "# Title\nSome *emphasis*, **strong** and `code`.\n- item with [link](https://example.com)\n> quote\n```powershell"
        };

        private static string Dump(string text, ITextHighlighter highlighter) {
            StringBuilder sb = new StringBuilder();
            int position = 0;
            foreach (HighlightToken token in highlighter.Tokenize(text)) {
                sb.Append(text, position, token.Start - position);
                sb.Append('«').Append(token.Role).Append(':').Append(text, token.Start, token.Length).Append('»');
                position = token.Start + token.Length;
            }
            sb.Append(text, position, text.Length - position);
            return sb.ToString().Replace("\r", string.Empty) + "\n";
        }

        public static IEnumerable<object[]> Languages() => Samples.Keys.Select(k => new object[] { k });

        [Theory]
        [MemberData(nameof(Languages))]
        public void Tokens_MatchGolden(string language) {
            ITextHighlighter highlighter = Highlighters.Get(language)!;
            Assert.NotNull(highlighter);
            Golden.Match("highlight/tokens." + language + ".txt", Dump(Samples[language], highlighter));
        }

        [Fact]
        public void SmartValues_MatchGolden() {
            string text = string.Join("\n", new[] {
                "Visit https://example.com/docs?x=1, or mail ops@example.com.",
                "id 3F2504E0-4F89-11D3-9A0C-0305E82C3301 at 192.168.1.10/24 and fe80::1ff:fe23:4567:890a",
                "path C:\\Program Files\\Agent\\agent.exe and /var/log/agent/today.log",
                "user CN=Jane Doe,OU=Admins,DC=ad,DC=example,DC=com changed 2026-10-17T14:03:11Z",
                "took 412ms, 91%, -3.5 and 1e6; enabled=true, owner=null, name='alpha' \"beta\"",
                "version2 and abc123 stay plain, 12:30:45 is a time"
            });
            Golden.Match("highlight/smart.txt", Dump(text, Highlighters.Smart));
        }

        [Theory]
        [InlineData(GlyphTier.Full)]
        [InlineData(GlyphTier.Ascii)]
        public void CodeView_LineNumbersMarksAndTruncation_MatchGolden(GlyphTier tier) {
            CodeView view = new CodeView(Samples["powershell"], Highlighters.Get("powershell")) { LineNumbers = true, Title = "Publish-Agent.ps1", MaxLines = 8 }.HighlightLines(5, 6);
            Golden.Match("highlight/codeview." + tier.ToString().ToLowerInvariant() + ".txt", TestTerminal.RenderText(view, 60, 0, tier));
        }

        [Fact]
        public void CodeView_Wrap_KeepsTheGutterOnContinuationRows() {
            CodeView view = new CodeView("short\n" + new string('x', 70), null) { LineNumbers = true, Wrap = true };
            string[] lines = TestTerminal.RenderText(view, 40).TrimEnd('\n').Split('\n');
            Assert.Equal(3, lines.Length);
            Assert.StartsWith(" 2 │ xxx", lines[1]);
            Assert.StartsWith("   │ xxx", lines[2]);
        }

        [Theory]
        [InlineData("settings.json", "{}", "json")]
        [InlineData("deploy.ps1", "", "powershell")]
        [InlineData("app.csproj", "", "xml")]
        [InlineData(".editorconfig", "", "ini")]
        [InlineData(null, "  {\n \"a\": 1 }", "json")]
        [InlineData(null, "<root/>", "xml")]
        [InlineData(null, "diff --git a/x b/x", "diff")]
        [InlineData(null, "2026-10-17 14:03:11 INFO started", "log")]
        [InlineData(null, "param($Path)\nGet-Item $Path", "powershell")]
        [InlineData(null, "name: build\nruns-on: windows\nsteps:\n  - run: x", "yaml")]
        public void Detect_UsesTheExtensionThenTheContent(string? path, string text, string expected) {
            Assert.Equal(expected, Highlighters.Detect(text, path)?.Name);
        }

        [Fact]
        public void Detect_PlainTextStaysPlain() {
            Assert.Null(Highlighters.Detect("Just a sentence, nothing more."));
        }

        [Fact]
        public void Text_IsLiteral_MarkupIsNotParsedAndControlsAreDropped() {
            string text = "[red]not markup[/] \u001b[31mesc\u0007 bell\tTab";
            string rendered = TestTerminal.RenderText(new CodeView(text, Highlighters.Smart), 80);
            Assert.Contains("[red]not markup[/]", rendered);
            Assert.False(rendered.Contains("\u001b"), string.Join(",", rendered.Select(ch => ((int)ch).ToString())));
            Assert.False(rendered.Contains("\u0007"), string.Join(",", rendered.Select(ch => ((int)ch).ToString())));
            Assert.DoesNotContain("\t", rendered);
            Assert.Contains("bell Tab", rendered); // the tab reaches the next multiple of four (column 32)
        }

        [Fact]
        public void LargeInput_HighlightsInLinearTime() {
            string json = "[" + string.Join(",", Enumerable.Range(0, 20000).Select(i => "{\"id\":" + i + ",\"name\":\"item " + i + "\",\"ok\":true}")) + "]";
            string log = string.Join("\n", Enumerable.Range(0, 5000).Select(i => "2026-10-17 14:03:11 INFO [Svc] request " + i + " from 10.0.0." + (i % 250) + " took " + i + "ms"));
            string hostile = new string('a', 50000) + "@" + new string('b', 50000);
            Stopwatch watch = Stopwatch.StartNew();
            Assert.NotEmpty(Highlighters.Get("json")!.Tokenize(json));
            Assert.NotEmpty(Highlighters.Get("log")!.Tokenize(log));
            Highlighters.Smart.Tokenize(hostile);
            Highlighters.Get("powershell")!.Tokenize(new string('[', 20000));
            Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), "took " + watch.Elapsed);
            CodeView view = new CodeView(json.Replace(",{", ",\n{"), Highlighters.Get("json")) { MaxLines = 50 };
            Assert.Contains("+ 19950 more lines", TestTerminal.RenderText(view, 80));
        }

        [Fact]
        public void Register_AddsALanguage() {
            Highlighters.Register(new RegexHighlighter("demo-lang").Words(HighlightRole.Keyword, "alpha").Rule(@"\d+", HighlightRole.Number), "demo");
            Assert.Equal("«Keyword:alpha» beta «Number:42»\n", Dump("alpha beta 42", Highlighters.ForPath("x.demo")!));
            Assert.Contains("demo-lang", Highlighters.Names);
        }
    }
}
