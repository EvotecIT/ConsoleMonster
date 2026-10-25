using System;
using System.IO;
using System.Text;
using ConsoleMonster.Export;
using ConsoleMonster.Graphics;
using ConsoleMonster.Sessions;
using ConsoleMonster.Widgets;

internal static class GraphicsSmoke {
    internal static void Run() {
        var options = new GraphicsExportOptions {
            Preset = GraphicsPreset.Docs,
            Playback = new RecordingExportOptions {
                Pace = RecordingPace.Source, HoldLastFrame = TimeSpan.Zero
            }
        };
        CheckPng(TextBlock.Plain("Native PNG").ToPng(options));
        var recording = SessionRecording.Read(new StringReader(
            "{\"version\":2,\"width\":20,\"height\":2}\n"
            + "[0,\"o\",\"Before\"]\n[0.5,\"o\",\"\\rReady\"]\n"));
        string directory = Path.Combine(Path.GetTempPath(), "ConsoleMonster-Aot-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            string gif = Path.Combine(directory, "recording.gif");
            recording.SaveGraphics(gif, GraphicsFormat.Gif, options);
            CheckHeader(File.ReadAllBytes(gif), Encoding.ASCII.GetBytes("GIF89a"));
            string apng = Path.Combine(directory, "recording.apng");
            recording.SaveGraphics(apng, GraphicsFormat.Apng, options);
            byte[] animation = File.ReadAllBytes(apng);
            CheckPng(animation);
            if (!Encoding.ASCII.GetString(animation).Contains("acTL", StringComparison.Ordinal)) {
                throw new InvalidOperationException("Native APNG output has no animation control chunk.");
            }
        } finally {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void CheckPng(byte[] bytes) => CheckHeader(bytes, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

    private static void CheckHeader(byte[] bytes, byte[] header) {
        if (bytes.Length <= header.Length) { throw new InvalidOperationException("Native graphics output is empty."); }
        for (int i = 0; i < header.Length; i++) {
            if (bytes[i] != header[i]) { throw new InvalidOperationException("Native graphics output has an invalid signature."); }
        }
    }
}
