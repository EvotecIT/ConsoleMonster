using ConsoleMonster.Export;
using ConsoleMonster.Sessions;

var options = new SessionRecordingOptions
{
    Path = "command-review.cast",
    Shell = "pwsh",
    Width = 88,
    Height = 10
};
options.Arguments.Add("-NoLogo");
options.Arguments.Add("-NoProfile");
options.Arguments.Add("-Command");
options.Arguments.Add("git --version; Write-Host 'Review complete' -ForegroundColor Green");
using (var recorder = SessionRecorder.Start(options, Console.Write))
{
    recorder.AddMarker("Inspect");
    recorder.WaitForExit();
}

var recording = SessionRecording.Read(options.Path);
var export = new RecordingExportOptions
{
    WindowChrome = true,
    Title = "Command review"
};
File.WriteAllText("command-review.html", recording.Export(ExportFormat.Html, export));