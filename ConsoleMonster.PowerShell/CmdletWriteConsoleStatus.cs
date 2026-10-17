using System;
using System.Management.Automation;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

using ConsoleMonster.Output;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows one status line that updates in place, with an optional spinner, clock and elapsed time.</para>
    /// <para type="description">Each call replaces the line (Write-Color style pieces, colours, -Markup, -Style and -Role as in Write-ConsoleText). -Spinner, -ShowClock and -ShowElapsed keep refreshing about ten times a second between calls. -Complete writes the final line (with -Status Pass, Fail or Warn for a marker) and moves on. The refresh stops by itself when other output or the prompt moves the cursor, so it never draws over them. In redirected output, CI, transcripts, jobs and other non-console hosts each call writes a normal line instead (unchanged repeats are skipped).</para>
    /// <example>
    ///   <para>A copy loop with a spinner and elapsed time</para>
    ///   <code>foreach ($file in $files) { Write-ConsoleStatus 'Copying ', $file.Name -Color Gray, Cyan -Spinner -ShowElapsed; Copy-Item $file.FullName $target }
    /// Write-ConsoleStatus "Copied $($files.Count) files" -Status Pass -ShowElapsed -Complete</code>
    /// </example>
    /// <example>
    ///   <para>A waiting line with a clock (PSWriteColor issue #15)</para>
    ///   <code>Write-ConsoleStatus 'You have waited ' -Color Green -BackgroundColor Blue -ShowClock -ShowElapsed; Start-Sleep 5; Write-ConsoleStatus 'Done' -Status Pass -Complete</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleStatus")]
    public sealed class CmdletWriteConsoleStatus : PSCmdlet {
        private static string? _lastFallback;

        /// <summary><para type="description">Text pieces, shown literally (strings, or rich text from New-ConsoleText).</para></summary>
        [Parameter(Position = 0)]
        [Alias("T")]
        [AllowEmptyString]
        [AllowNull]
        public object[]? Text { get; set; }

        /// <summary><para type="description">Foreground colour per piece.</para></summary>
        [Parameter]
        [Alias("C", "ForegroundColor", "FGC")]
        public object[]? Color { get; set; }

        /// <summary><para type="description">Background colour per piece.</para></summary>
        [Parameter]
        [Alias("B", "BGC")]
        public object[]? BackgroundColor { get; set; }

        /// <summary><para type="description">Markup added after the pieces.</para></summary>
        [Parameter]
        public string? Markup { get; set; }

        /// <summary><para type="description">Decorations: Bold, Dim, Italic, Underline, Strikethrough, Invert.</para></summary>
        [Parameter]
        public string[]? Style { get; set; }

        /// <summary><para type="description">A theme role for pieces without their own colour.</para></summary>
        [Parameter]
        public ThemeToken? Role { get; set; }

        /// <summary><para type="description">A status marker before the text (Pass, Fail, Warn, Info, Running...).</para></summary>
        [Parameter]
        public StatusKind? Status { get; set; }

        /// <summary><para type="description">Shows a spinner while the status is open.</para></summary>
        [Parameter]
        public SwitchParameter Spinner { get; set; }

        /// <summary><para type="description">Shows the time of day, refreshed every second.</para></summary>
        [Parameter]
        public SwitchParameter ShowClock { get; set; }

        /// <summary><para type="description">Shows the time since the status line started.</para></summary>
        [Parameter]
        public SwitchParameter ShowElapsed { get; set; }

        /// <summary><para type="description">The clock format (default HH:mm:ss).</para></summary>
        [Parameter]
        [Alias("DateTimeFormat")]
        public string ClockFormat { get; set; } = "HH:mm:ss";

        /// <summary><para type="description">Writes the final version of the line and ends it.</para></summary>
        [Parameter]
        public SwitchParameter Complete { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            RichText content = PSRichText.Build(Text, Color, BackgroundColor, Markup, Style, null, Role, null);
            TerminalProfile profile = Terminal.GetCurrentProfile();
            bool inPlace = !HostOutput.UseHost(this, profile) && profile.Mode == OutputMode.Interactive && profile.SupportsVirtualTerminal;
            if (inPlace) {
                StatusLine.Show(profile, content, Status, Spinner.IsPresent, ShowClock.IsPresent, ShowElapsed.IsPresent, ClockFormat, Complete.IsPresent);
                return;
            }
            // Append-only hosts: one line per change; the spinner and the refresh do not apply.
            string key = content.PlainText + "|" + Status + "|" + Complete.IsPresent;
            if (!Complete.IsPresent && string.Equals(key, _lastFallback, StringComparison.Ordinal)) {
                return;
            }
            _lastFallback = Complete.IsPresent ? null : key;
            DateTime started = DateTime.Now;
            HostOutput.WriteLines(this, (context, width) => new[] { StatusLine.Compose(context, content, Status, false, ShowClock.IsPresent, false, ClockFormat, started, 0, true) }, true);
        }
    }
}
