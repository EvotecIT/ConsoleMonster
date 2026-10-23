using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;

namespace ConsoleMonster.TestHost {
    /// <summary>A borrowed output and input barrier shared by the C# and PowerShell app contract tests.</summary>
    public sealed class RendererFailureProbe : ITerminalOutput, IInputSource {
        private readonly ManualResetEventSlim _ended = new ManualResetEventSlim();
        private int _failed;

        /// <summary>The original exception thrown on the renderer thread.</summary>
        public IOException Failure { get; } = new IOException("Synthetic renderer failure.");
        /// <inheritdoc />
        public TerminalProfile Profile { get; } = TerminalProfile.Fixed(80, 24);
        /// <inheritdoc />
        public bool SupportsLiveFrames => true;
        /// <inheritdoc />
        public string Name => "Renderer failure barrier";
        /// <summary>Whether live output cleanup completed.</summary>
        public bool Ended => _ended.IsSet;
        /// <inheritdoc />
        public void BeginLive() { }
        /// <inheritdoc />
        public void EndLive() => _ended.Set();
        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RenderFrame(CellBuffer frame) {
            // Allow the final cleanup frame so the captured exception retains its original renderer stack.
            if (Interlocked.Exchange(ref _failed, 1) == 0) { throw Failure; }
        }
        /// <inheritdoc />
        public void WriteBlock(CellBuffer buffer) { }
        /// <inheritdoc />
        public void WriteLine(StyledLine line) { }
        /// <inheritdoc />
        public void WriteInline(StyledLine line) { }
        /// <inheritdoc />
        public void Invalidate() { }
        /// <inheritdoc />
        public InputEvent Read(TimeSpan timeout, CancellationToken cancellationToken) {
            if (!_ended.Wait(TimeSpan.FromSeconds(15), cancellationToken)) {
                throw new TimeoutException("The failing renderer did not finish cleanup.");
            }
            return InputEvent.Press(ConsoleKey.Escape);
        }
        /// <inheritdoc />
        public void Dispose() => _ended.Dispose();
    }
}
