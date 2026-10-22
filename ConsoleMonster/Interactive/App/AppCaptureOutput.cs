using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.Interactive {
    /// <summary>Lends the existing interactive screen recorder to the live session engine.</summary>
    internal sealed class AppCaptureOutput : ITerminalOutput {
        private readonly ScreenCapture _capture;
        internal AppCaptureOutput(ScreenCapture capture, TerminalProfile profile) { _capture = capture; Profile = profile; }
        public TerminalProfile Profile { get; }
        public bool SupportsLiveFrames => true;
        public void WriteBlock(CellBuffer buffer) => _capture.Surface.Present(buffer, null);
        public void RenderFrame(CellBuffer frame) => _capture.Surface.Present(frame, null);
        public void WriteLine(StyledLine line) { }
        public void WriteInline(StyledLine line) { }
        public void BeginLive() { }
        public void Invalidate() { }
        public void EndLive() { }
        public void Dispose() { }
    }
}
