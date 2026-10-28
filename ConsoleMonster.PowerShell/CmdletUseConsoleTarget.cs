using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Output;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Runs a script block with ConsoleMonster output going somewhere else: a file, a text writer, any target, or a string.</para>
    /// <para type="description">Inside the script block every ConsoleMonster command (Write-ConsoleText, Show-ConsoleTable, Write-ConsoleText -Markup, live dashboards, prompts) writes to the target instead of the console. With -Path the output goes to a UTF-8 file, with -Writer to a TextWriter, with -Target to a target object (ConsoleMonster.Output.ITerminalTarget); with none of them it is returned as one string. -Width, -Glyphs and -ColorDepth lay the output out for the file or string; prompts there have nobody to answer and use their defaults. The script block's own output is passed through.</para>
    /// </summary>
    /// <example>
    ///   <para>Save a report as plain text</para>
    ///   <code>Use-ConsoleTarget -Path .\report.txt -Width 120 { $servers | Show-ConsoleTable; Write-ConsoleText -Markup '[pass]done[/]' }</code>
    /// </example>
    /// <example>
    ///   <para>Capture a table as a string</para>
    ///   <code>$text = Use-ConsoleTarget -Width 80 { Get-Service | Select-Object -First 5 | Show-ConsoleTable }</code>
    /// </example>
    [Cmdlet(VerbsOther.Use, "ConsoleTarget", DefaultParameterSetName = "String")]
    public sealed class CmdletUseConsoleTarget : NoPipelineInputCmdlet {
        /// <summary><para type="description">The commands whose output goes to the target.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public ScriptBlock ScriptBlock { get; set; } = ScriptBlock.Create(string.Empty);

        /// <summary><para type="description">A file to write to (UTF-8 without a byte order mark; replaced unless -Append).</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Path")]
        public string? Path { get; set; }

        /// <summary><para type="description">Appends to the file instead of replacing it.</para></summary>
        [Parameter(ParameterSetName = "Path")]
        public SwitchParameter Append { get; set; }

        /// <summary><para type="description">A TextWriter to write to (it stays open).</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Writer")]
        public TextWriter? Writer { get; set; }

        /// <summary><para type="description">A target object (ConsoleMonster.Output.ITerminalTarget).</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Target")]
        public ITerminalTarget? Target { get; set; }

        /// <summary><para type="description">The width in cells for -Path, -Writer or the string (default 0: terminal width, falling back to 100).</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
        public int Width { get; set; }

        /// <summary><para type="description">The glyph tier for -Path, -Writer or the string (default Ascii).</para></summary>
        [Parameter]
        public GlyphTier Glyphs { get; set; } = GlyphTier.Ascii;

        /// <summary><para type="description">The colour depth for -Path, -Writer or the string (default None: plain text).</para></summary>
        [Parameter]
        public ColorDepth ColorDepth { get; set; } = ColorDepth.None;

        /// <inheritdoc />
        protected override void EndProcessing() {
            StringWriter? text = null;
            StreamWriter? file = null;
            ITerminalTarget target;
            try {
                switch (ParameterSetName) {
                    case "Target":
                        target = Target!;
                        break;
                    case "Writer":
                        target = new WriterTarget(Writer!, Width, Glyphs, ColorDepth);
                        break;
                    case "Path":
                        string path = SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);
                        file = new StreamWriter(path, Append.IsPresent, new UTF8Encoding(false));
                        target = new WriterTarget(file, Width, Glyphs, ColorDepth);
                        break;
                    default:
                        text = new StringWriter();
                        target = new WriterTarget(text, Width, Glyphs, ColorDepth);
                        break;
                }
                using (Terminal.Use(target)) {
                    foreach (PSObject item in ScriptBlock.Invoke()) {
                        WriteObject(item);
                    }
                }
            } catch (RuntimeException ex) {
                ThrowTerminatingError(new ErrorRecord(ex, "ConsoleMonsterTargetScript", ErrorCategory.NotSpecified, null));
            } finally {
                file?.Dispose();
            }
            if (text != null) {
                WriteObject(text.ToString().Replace("\r\n", "\n").TrimEnd('\n'));
            }
        }
    }
}
