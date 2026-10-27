using System;
using System.IO;
using ConsoleMonster.Demos;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Sessions;

// dotnet run --project Website/examples/presenter-kit -- /path/to/output [--present]
string output = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(Environment.CurrentDirectory, "talk-output");
Directory.CreateDirectory(output);
DemoScript script = DemoScript.Parse("""
# demo: setup
function prompt { 'demo λ> ' }
# demo: wpm 180
# demo: chapter Calculate
Write-Host ('The answer is ' + (6 * 7)) -ForegroundColor Cyan
# demo: pause 1
# demo: chapter Report
'Compile: passed', 'Tests: 24 passed', 'Package: ready' | ForEach-Object { Write-Host $_ -ForegroundColor Green }
# demo: pause 2
""");
DemoOptions options = new DemoOptions {
    Seed = 42,
    Output = Console.Write,
    Recording = new SessionRecordingOptions
    {
        Path = Path.Combine(output, "talk.cast"),
        Shell = "pwsh",
        Width = 80,
        Height = 12
    }
};
if (Array.IndexOf(args, "--present") >= 0) {
    ConsolePresentation.Present(
        script,
        options,
        stepChanged: (step, total) => Console.Error.WriteLine("[demo " + step + "/" + total + "]"));
} else {
    ConsolePresentation.Record(script, options);
}
SessionRecording recording = SessionRecording.Read(options.Recording.Path);
var graphics = new GraphicsExportOptions {
    Preset = GraphicsPreset.Slide16x9,
    Frame = new ExportOptions
    {
        Theme = ExportTheme.Dark,
        WindowChrome = true,
        Title = "A repeatable demo"
    }
};
recording.SaveGraphics(Path.Combine(output, "talk.gif"), GraphicsFormat.Gif, graphics);
recording.SaveChapterPngs(Path.Combine(output, "slides"), graphics);
Console.WriteLine("Saved " + output);
