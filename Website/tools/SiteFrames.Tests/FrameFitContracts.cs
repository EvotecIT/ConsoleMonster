using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Styling;
using SiteFrames.Replay;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>
    /// Frames fit their content: wide docs frames and recordings are cut to the columns and rows they use, and a recording
    /// that opens on its final screen plays from its first full screen, never from an empty prompt.
    /// </summary>
    public sealed class FrameFitContracts {
        private static CellBuffer Frame(int width, int height, params string[] rows) {
            CellBuffer frame = new CellBuffer(width, height);
            for (int y = 0; y < rows.Length; y++) {
                frame.Write(0, y, rows[y], Style.Plain);
            }
            return frame;
        }

        [Fact]
        public void Fit_WideRenderings_AreCutToTheWiderContentOfBoth() {
            CellBuffer dark = Frame(120, 4, "Host  Role", "WEB01 Web");
            CellBuffer light = Frame(120, 4, "Host  Role", "SQL01 Database");
            (CellBuffer fitDark, CellBuffer? fitLight) = Site.Fit(dark, light);
            Assert.Equal(14, fitDark.Width);
            Assert.Equal(14, fitLight!.Width);
            Assert.Equal("WEB01 Web", fitDark.GetRowText(1).TrimEnd());
        }

        [Fact]
        public void Fit_BackgroundCells_CountAsContent() {
            CellBuffer frame = Frame(120, 2, "abc");
            frame.Set(30, 1, " ", Style.Plain.WithBackground(Color.FromRgb(10, 20, 30)));
            (CellBuffer fit, CellBuffer? _) = Site.Fit(frame, null);
            Assert.Equal(31, fit.Width);
        }

        [Fact]
        public void FrameAt_NarrowWidth_KeepsTheDeclaredWidthAndWideWidthIsTrimmed() {
            Site site = new Site(WebsiteRoot());
            CellBuffer narrow = Frame(60, 2, "Host  Role");
            CellBuffer wide = Frame(120, 2, "Host  Role");
            Assert.Equal(60, site.FrameAt(60, narrow, null)["w"]!.GetValue<int>());
            Assert.Equal(10, site.FrameAt(120, wide, null)["w"]!.GetValue<int>());
            Assert.True(Site.FitFrom > 60 && Site.FitFrom <= 120);
        }

        [Fact]
        public void Trim_Recording_IsCutToTheRowsAndColumnsAnyFrameUses() {
            List<TimedFrame> frames = new List<TimedFrame> {
                new TimedFrame(0, Frame(100, 40, "PS> Import-Module ConsoleMonster")),
                new TimedFrame(500, Frame(100, 40, "PS> Get-Process", "", "Name  CPU", "pwsh  12.5", "", "a longer line further down the screen"))
            };
            List<TimedFrame> trimmed = ReplayFit.Trim(frames);
            Assert.All(trimmed, f => Assert.Equal(37, f.Frame.Width));
            Assert.All(trimmed, f => Assert.Equal(6, f.Frame.Height));
            Assert.Equal("PS> Import-Module ConsoleMonster", trimmed[0].Frame.GetRowText(0).TrimEnd());
        }

        [Fact]
        public void Hero_SessionThatStartsWithOnePromptLine_PlaysFromTheFirstFullScreen() {
            List<TimedFrame> frames = new List<TimedFrame> {
                new TimedFrame(0, Frame(40, 12, "PS> Import-Module ConsoleMonster")),
                new TimedFrame(1200, Frame(40, 12, "PS> Import-Module ConsoleMonster", "PS> Get-Process")),
                new TimedFrame(2400, Frame(40, 12, "PS>", "PS> Get-Process", "Name", "a", "b", "c")),
                new TimedFrame(4000, Frame(40, 12, "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"))
            };
            Assert.Equal(2400, ReplayFit.Hero(frames));
            Assert.Equal(0, ReplayFit.Hero(new List<TimedFrame> { new TimedFrame(0, Frame(10, 2, "full", "full")) }));
        }

        [Fact]
        public void Presentation_RunOnlyBlocksAndShellLabelledDotnet_AreRejected() {
            Assert.NotEmpty(CodeGuard.Presentation("powershell", "./live-filter.ps1"));
            Assert.NotEmpty(CodeGuard.Presentation("shell", "dotnet run"));
            Assert.NotEmpty(CodeGuard.Presentation("powershell", "pwsh -NoProfile -File ./report.ps1\n# then look at the output"));
            Assert.NotEmpty(CodeGuard.Presentation("bash", "dotnet new console -n Demo\ncd Demo\ndotnet add package ConsoleMonster"));
            Assert.NotEmpty(CodeGuard.Presentation("powershell", "dotnet new console -n Demo\nSet-Location Demo"));
            Assert.Empty(CodeGuard.Presentation("terminal", "dotnet new console -n Demo\ncd Demo\ndotnet add package ConsoleMonster"));
            Assert.Empty(CodeGuard.Presentation("powershell", "Install-Module ConsoleMonster -Scope CurrentUser\n./live-filter.ps1 -AppOptions @{ StaticSnapshot = $true }\nGet-Process"));
            Assert.Empty(CodeGuard.Presentation("powershell", "Enable-ConsoleFormatting -Feature Errors"));
        }

        [Fact]
        public void Presentation_EveryDocsAndGuidePage_PassesTheRules() {
            string content = Path.Combine(WebsiteRoot(), "content");
            List<string> problems = new List<string>();
            foreach (string page in Directory.EnumerateFiles(content, "*.md", SearchOption.AllDirectories)) {
                bool walkthrough = !Path.GetRelativePath(content, page).StartsWith("pages", StringComparison.Ordinal);
                foreach ((int line, string language, string code) in CodeGuard.Blocks(Site.ReadText(page))) {
                    problems.AddRange(CodeGuard.Presentation(language, code, walkthrough).Select(p => Path.GetFileName(page) + ":" + line + " " + p));
                }
            }
            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        private static string WebsiteRoot() {
            DirectoryInfo? repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository != null && !File.Exists(Path.Combine(repository.FullName, "ConsoleMonster.sln"))) {
                repository = repository.Parent;
            }
            Assert.NotNull(repository);
            return Path.Combine(repository!.FullName, "Website");
        }
    }
}
