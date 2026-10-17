using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Compares console output with a golden file (for Pester and other tests).</para>
    /// <para type="description">Renders a widget at a fixed width, glyph tier and culture (invariant by default), so the result does not depend on the machine, and compares the text with a golden file. The first run writes the file and fails so it can be reviewed; a difference fails with the first differing line, both versions and the column where they part. Set CONSOLEMONSTER_UPDATE_GOLDEN=1 to rewrite golden files after an intended change. Strings are compared as they are. In Pester the thrown error fails the test; see the README for a Should operator built on it.</para>
    /// <example>
    ///   <para>A table snapshot in a Pester test</para>
    ///   <code>It 'renders the report table' {
    ///     $rows | Format-ConsoleTable -AsWidget | Assert-ConsoleOutput -GoldenPath "$PSScriptRoot\Golden\report.txt" -Width 80
    /// }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Assert, "ConsoleOutput")]
    [OutputType(typeof(string))]
    public sealed class CmdletAssertConsoleOutput : PSCmdlet {
        /// <summary><para type="description">A widget, or text to compare as it is.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public object InputObject { get; set; } = null!;

        /// <summary><para type="description">The golden file.</para></summary>
        [Parameter(Mandatory = true, Position = 1)]
        public string GoldenPath { get; set; } = string.Empty;

        /// <summary><para type="description">Width in cells (default 80).</para></summary>
        [Parameter]
        [ValidateRange(1, 1000)]
        public int Width { get; set; } = 80;

        /// <summary><para type="description">Height in cells (default: as tall as the widget measures).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Height { get; set; }

        /// <summary><para type="description">The glyph tier (default Full).</para></summary>
        [Parameter]
        public GlyphTier Glyphs { get; set; } = GlyphTier.Full;

        /// <summary><para type="description">The culture for numbers and dates (default: invariant).</para></summary>
        [Parameter]
        public CultureInfo? Culture { get; set; }

        /// <summary><para type="description">Writes the rendered text to the pipeline when it matches.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object raw = PSObject.AsPSObject(InputObject).BaseObject;
            string text;
            if (raw is string s) {
                text = s;
            } else if (raw is CellBuffer buffer) {
                text = buffer.ToPlainText();
            } else {
                IWidget widget = PSWidgets.AsWidget(InputObject) ?? new TextBlock(PSWidgets.ToText(InputObject)) { IsMarkup = false };
                text = TestTerminal.RenderText(widget, Width, Height, Glyphs, Culture);
            }
            string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(GoldenPath);
            try {
                GoldenFile.Match(path, text);
            } catch (GoldenFileMismatchException ex) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleGoldenMismatch", ErrorCategory.InvalidResult, path));
            }
            if (PassThru.IsPresent) {
                WriteObject(text);
            }
        }
    }
}
