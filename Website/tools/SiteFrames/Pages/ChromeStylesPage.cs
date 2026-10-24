using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.Sessions;
using ConsoleMonster.Widgets;

namespace SiteFrames.Pages {
    /// <summary>Public export samples made by the core writers and the real session capture.</summary>
    internal static class ChromeStylesPage {
        public static void Render(Site site) {
            SessionRecording session = SessionRecording.Read(System.IO.Path.Combine(site.Captures, "recording", "session.cast"));
            foreach (WindowChromeStyle style in new[] { WindowChromeStyle.MacOS, WindowChromeStyle.Windows, WindowChromeStyle.Linux, WindowChromeStyle.None }) {
                foreach (ExportTheme theme in new[] { ExportTheme.Dark, ExportTheme.Light }) {
                    string stem = "recordings/chrome/" + style.ToString().ToLowerInvariant() + "-" + theme.ToString().ToLowerInvariant();
                    ExportOptions frame = new ExportOptions { Width = 36, Title = "Build report", WindowChrome = true, ChromeStyle = style, Theme = theme };
                    Panel panel = new Panel(TextBlock.Plain("8 of 8 checks passed"), "Build");
                    site.Write(site.Static(stem + ".svg"), ConsoleExporter.ToSvg(panel, frame));
                    site.Write(site.Static(stem + ".html"), ConsoleExporter.ToHtml(panel, frame));
                    RecordingExportOptions recording = new RecordingExportOptions { Title = "Console session", WindowChrome = true, ChromeStyle = style, Theme = theme };
                    site.Write(site.Static(stem + "-player.html"), session.Export(ExportFormat.Html, recording));
                    site.Write(site.Static(stem + "-animated.svg"), session.Export(ExportFormat.Svg, recording));
                    site.Write(site.Static(stem + "-final.svg"), session.ExportFinalSvg(frame));
                    frame.Width = 1; frame.FontSize = 4; frame.Padding = 0; frame.Title = "A long caption at the minimum width";
                    site.Write(site.Static(stem + "-minimum.svg"), ConsoleExporter.ToSvg(TextBlock.Plain("x"), frame));
                    site.Write(site.Static(stem + "-minimum.html"), ConsoleExporter.ToHtml(TextBlock.Plain("x"), frame));
                }
            }
        }
    }
}
