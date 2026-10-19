using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows errors and exceptions readably: type, message, position, script stack, frames and inner errors.</para>
    /// <para type="description">Takes ErrorRecords (from $Error, $_ in a catch block, -ErrorVariable) and .NET exceptions from the pipeline. Each shows the type and the message, the category and error id, PowerShell's position message with the failing line, the script stack, the .NET frames as Type.Method() with shortened paths and file:line links, framework frames collapsed into "+n framework frames", and the inner exceptions as "Caused by". With -AsWidget the views are returned for New-ConsolePanel, dashboards or Export-ConsoleFrame instead of written. Redirected output is plain text.</para>
    /// <example>
    ///   <para>The last error</para>
    ///   <code>$Error[0] | Format-ConsoleError</code>
    /// </example>
    /// <example>
    ///   <para>In a catch block, with the framework frames</para>
    ///   <code>try { Get-Item C:\missing -ErrorAction Stop } catch { $_ | Format-ConsoleError -ShowFrameworkFrames }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Format, "ConsoleError")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletFormatConsoleError : ConsoleErrorCmdlet {
        /// <summary><para type="description">Returns the views instead of writing them.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override bool ReturnWidgets => AsWidget.IsPresent;
    }

    /// <summary>The parameters and output shared by Format-ConsoleError and Write-ConsoleException.</summary>
    public abstract class ConsoleErrorCmdlet : PSCmdlet {
        private readonly List<object> _items = new List<object>();

        /// <summary><para type="description">An ErrorRecord, an exception or a message (pipeline input).</para></summary>
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public object? InputObject { get; set; }

        /// <summary><para type="description">Shows framework frames (System, Microsoft, the PowerShell engine) instead of collapsing them.</para></summary>
        [Parameter]
        public SwitchParameter ShowFrameworkFrames { get; set; }

        /// <summary><para type="description">The most app frames shown per error (default 8).</para></summary>
        [Parameter]
        public int MaxFrames { get; set; } = 8;

        /// <summary><para type="description">Leaves out the stack frames: type, message and position only.</para></summary>
        [Parameter]
        public SwitchParameter NoStack { get; set; }

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }
        /// <summary><para type="description">Lays the output out for this width instead of the terminal's.</para></summary>
        [Parameter]
        public int Width { get; set; }

        /// <summary>Whether the views are returned instead of written.</summary>
        protected virtual bool ReturnWidgets => false;

        /// <inheritdoc />
        protected override void ProcessRecord() {
            object? item = InputObject is PSObject ps ? ps.BaseObject : InputObject;
            if (item != null) {
                _items.Add(item);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<IWidget> views = new List<IWidget>();
            foreach (object item in _items) {
                ExceptionView view = new ExceptionView(PSErrors.Describe(item)) {
                    ShowFrameworkFrames = ShowFrameworkFrames.IsPresent,
                    MaxFrames = Math.Max(1, MaxFrames),
                    ShowStack = !NoStack.IsPresent,
                    BasePath = SessionState.Path.CurrentFileSystemLocation.ProviderPath
                };
                if (ReturnWidgets) {
                    WriteObject(view);
                } else {
                    views.Add(view);
                }
            }
            if (views.Count > 0) {
                PSFormatting.Emit(this, views.Count == 1 ? views[0] : new Rows(views.ToArray()) { Gap = 1 }, Width, null, AsString.IsPresent);
            }
        }
    }

    /// <summary>
    /// <para type="synopsis">Writes an error or exception readably (Format-ConsoleError without -AsWidget).</para>
    /// <para type="description">The same view as Format-ConsoleError: type, message, position, script stack, frames with file:line links, collapsed framework frames and inner errors.</para>
    /// <example>
    ///   <para>In a catch block</para>
    ///   <code>try { [int]::Parse('x') } catch { Write-ConsoleException $_ }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Write, "ConsoleException")]
    public sealed class CmdletWriteConsoleException : ConsoleErrorCmdlet {
    }

    /// <summary>Maps PowerShell errors onto <see cref="ExceptionInfo"/>.</summary>
    internal static class PSErrors {
        public static ExceptionInfo Describe(object item) {
            switch (item) {
                case ErrorRecord record:
                    return FromRecord(record);
                case IContainsErrorRecord contains when contains.ErrorRecord != null && item is Exception exception && contains.ErrorRecord.Exception == exception:
                    return FromRecord(contains.ErrorRecord);
                case Exception exception:
                    return ExceptionInfo.FromException(exception);
                default:
                    return new ExceptionInfo("Error", item.ToString() ?? string.Empty);
            }
        }

        private static ExceptionInfo FromRecord(ErrorRecord record) {
            ExceptionInfo inner = ExceptionInfo.FromException(record.Exception);
            string message = record.ErrorDetails?.Message is string details && details.Length > 0 ? details : record.Exception.Message;
            ExceptionInfo info = new ExceptionInfo(inner.Type, message) { Inner = inner.Inner };
            info.Frames.AddRange(inner.Frames);
            info.ScriptFrames.AddRange(ExceptionInfo.ParseScriptStackTrace(record.ScriptStackTrace));
            info.PositionMessage = record.InvocationInfo?.PositionMessage is string position && position.Length > 0 ? position : null;
            ErrorCategoryInfo category = record.CategoryInfo;
            if (category != null && category.Category != ErrorCategory.NotSpecified) {
                info.Category = category.Category + (string.IsNullOrEmpty(category.TargetName) ? string.Empty : " (" + category.TargetName + (string.IsNullOrEmpty(category.TargetType) ? string.Empty : ":" + category.TargetType) + ")");
            }
            info.ErrorId = string.IsNullOrEmpty(record.FullyQualifiedErrorId) ? null : record.FullyQualifiedErrorId;
            return info;
        }
    }
}
