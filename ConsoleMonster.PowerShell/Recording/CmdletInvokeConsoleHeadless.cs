using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Headless;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs a script (usually an app) on a headless terminal with scripted input and returns the screens it drew.</para>
    /// <para type="description">The script runs in this session against a headless terminal of the given size, so apps, prompts, menus and dashboards draw there exactly as in a console window. The input is queued first in the recorder's grammar: key names (Down, Enter, Ctrl+K), quoted text, paste:, resize:WxH, wait:ms and the mouse at zero-based cells (click:x,y, dclick:, rclick:, move:, wheel:x,y,n, drag:x1,y1,x2,y2). Input ends after it, so an app that reads past the last key finishes. The result has the final screen's Text and Cells, every distinct screen in Frames and the script's Output, so a Pester test can assert on what the user would have seen. To watch an app in a browser or drive it from an agent, set CONSOLEMONSTER_HEADLESS and CONSOLEMONSTER_DEBUG_SOCKET before starting it; the browser host itself is a C# API.</para>
    /// </summary>
    /// <example><para>Drive a list with a key and a click, then check the screen.</para><code>$result = Invoke-ConsoleHeadless -Width 56 -Height 10 -InputScript 'Down click:4,5 Escape' {
    ///     Show-ConsoleApp -Content {
    ///         New-ConsoleSelect -Key Server -Choices WEB01, SQL01, FS01
    ///         New-ConsoleBinding -Content { param($State) "Picked $($State.Server)" }
    ///     }
    /// }
    /// $result.Text | Should -Match 'Picked FS01'</code></example>
    [Cmdlet(VerbsLifecycle.Invoke, "ConsoleHeadless")]
    [OutputType(typeof(ConsoleHeadlessResult))]
    public sealed class CmdletInvokeConsoleHeadless : PSCmdlet {
        /// <summary><para type="description">The script to run, usually one that calls Show-ConsoleApp or a prompt.</para></summary>
        [Parameter(Mandatory = true, Position = 0)] public ScriptBlock ScriptBlock { get; set; } = null!;
        /// <summary><para type="description">The keys, text and mouse actions to send, in the recorder's input grammar.</para></summary>
        [Parameter] public string? InputScript { get; set; }
        /// <summary><para type="description">Terminal width in cells (default 100).</para></summary>
        [Parameter, ValidateRange(10, 1000)] public int Width { get; set; } = 100;
        /// <summary><para type="description">Terminal height in cells (default 30).</para></summary>
        [Parameter, ValidateRange(3, 1000)] public int Height { get; set; } = 30;
        /// <summary><para type="description">Arguments passed to the script block.</para></summary>
        [Parameter] public object[]? ArgumentList { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            using (HeadlessTerminal terminal = new HeadlessTerminal(Width, Height)) {
                var frames = new List<CellBuffer>();
                terminal.FrameRendered += frame => { lock (frames) { frames.Add(frame); } };
                if (!string.IsNullOrEmpty(InputScript)) { terminal.Send(InputScript!); }
                terminal.CompleteInput();
                Collection<PSObject> output;
                using (Terminal.Use(terminal)) { output = ScriptBlock.Invoke(ArgumentList ?? Array.Empty<object>()); }
                CellBuffer[] drawn;
                lock (frames) { drawn = frames.ToArray(); }
                WriteObject(new ConsoleHeadlessResult(terminal.Snapshot(), drawn, output.Select(item => item?.BaseObject).ToArray()));
            }
        }
    }

    /// <summary>What a script drew on a headless terminal: the final screen, every distinct screen, and the script's output.</summary>
    public sealed class ConsoleHeadlessResult {
        internal ConsoleHeadlessResult(CellBuffer final, IReadOnlyList<CellBuffer> frames, object?[] output) {
            List<ConsoleHeadlessScreen> screens = new List<ConsoleHeadlessScreen>();
            foreach (CellBuffer frame in frames) {
                string text = Plain(frame);
                if (screens.Count == 0 || screens[screens.Count - 1].Text != text) { screens.Add(new ConsoleHeadlessScreen(screens.Count, text, frame)); }
            }
            // An app restores the screen it started on when it ends; the result is the last screen it drew.
            ConsoleHeadlessScreen? last = screens.LastOrDefault(screen => screen.Text.Trim().Length > 0);
            Cells = last?.Cells ?? final;
            Text = last?.Text ?? Plain(final);
            Frames = screens;
            Output = output;
        }

        /// <summary>The text of the last screen the script drew, rows separated by newlines, trailing spaces removed.</summary>
        public string Text { get; }
        /// <summary>The cells of that screen, with their styles.</summary>
        public CellBuffer Cells { get; }
        /// <summary>Every distinct screen, in order.</summary>
        public IReadOnlyList<ConsoleHeadlessScreen> Frames { get; }
        /// <summary>What the script block returned, such as the app state from Show-ConsoleApp -PassThru.</summary>
        public IReadOnlyList<object?> Output { get; }

        /// <inheritdoc />
        public override string ToString() => Text;

        internal static string Plain(CellBuffer frame) => string.Join("\n", frame.ToPlainText().Replace("\r\n", "\n").Split('\n').Select(row => row.TrimEnd())).TrimEnd('\n');
    }

    /// <summary>One screen a headless run drew.</summary>
    public sealed class ConsoleHeadlessScreen {
        internal ConsoleHeadlessScreen(int index, string text, CellBuffer cells) { Index = index; Text = text; Cells = cells; }

        /// <summary>Its position among the distinct screens, from zero.</summary>
        public int Index { get; }
        /// <summary>Its text, rows separated by newlines, trailing spaces removed.</summary>
        public string Text { get; }
        /// <summary>Its cells, with their styles.</summary>
        public CellBuffer Cells { get; }

        /// <inheritdoc />
        public override string ToString() => Text;
    }
}
