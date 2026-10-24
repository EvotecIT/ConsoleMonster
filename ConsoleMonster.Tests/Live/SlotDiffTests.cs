using System;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Rendering;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Live {
    /// <summary>Replacing a live region preserves the renderer's cell-diff history.</summary>
    [Collection("Live sessions")]
    public sealed class SlotDiffTests {
        /// <summary>A one-cell region change writes only that cell, without invalidating the whole frame.</summary>
        [Fact]
        public void RegionReplacement_UsesCellDiffWithoutFullInvalidation() {
            RegionWidget slot = new RegionWidget("result", new TextBlock("count: 1"));
            using (DiffOutput output = new DiffOutput())
            using (LiveSession session = new LiveSession(slot, new LiveSessionOptions {
                Output = output, Profile = output.Profile, CaptureConsole = false, TaskbarProgress = false,
                HandleCancelKey = false, PrintFinalState = false, WindowSource = () => new[] { 0, 0, 20, 4 }
            })) {
                session.Start();
                Assert.True(output.Initial.Wait(TimeSpan.FromSeconds(20)), "Initial frame watchdog expired.");
                int invalidations = Volatile.Read(ref output.Invalidations);
                session.Region("result").Set(new TextBlock("count: 2"));
                Assert.True(output.Updated.Wait(TimeSpan.FromSeconds(20)), "Updated frame watchdog expired.");
                session.Stop();
                Assert.Equal(invalidations, output.Invalidations);
                Assert.Equal("\u001b[?2026h\u001b[1;8H2\u001b[?2026l", output.Change);
                Assert.Equal(0, session.EventsDropped);
                Assert.Equal(0, session.RenderErrors);
            }
        }

        private sealed class DiffOutput : ITerminalOutput {
            private readonly StringTerminalWriter _writer = new StringTerminalWriter();
            private readonly VtOutput _output;
            internal readonly ManualResetEventSlim Initial = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim Updated = new ManualResetEventSlim();
            internal int Invalidations;
            internal string? Change;
            internal DiffOutput() { _output = new VtOutput(_writer, Profile, false); }
            public TerminalProfile Profile { get; } = TerminalProfile.Fixed(20, 4, ColorDepth.None, GlyphTier.Ascii);
            public bool SupportsLiveFrames => true;
            public void BeginLive() => _output.BeginLive();
            public void EndLive() => _output.EndLive();
            public void Invalidate() { Interlocked.Increment(ref Invalidations); _output.Invalidate(); }
            public void WriteBlock(CellBuffer buffer) => _output.WriteBlock(buffer);
            public void WriteLine(StyledLine line) => _output.WriteLine(line);
            public void WriteInline(StyledLine line) => _output.WriteInline(line);
            public void RenderFrame(CellBuffer frame) {
                _writer.Clear();
                _output.RenderFrame(frame);
                string text = frame.ToPlainText();
                if (text.Contains("count: 1")) { Initial.Set(); }
                if (text.Contains("count: 2") && !Updated.IsSet) { Change = _writer.ToString(); Updated.Set(); }
            }
            public void Dispose() { _output.Dispose(); Initial.Dispose(); Updated.Dispose(); }
        }
    }
}
