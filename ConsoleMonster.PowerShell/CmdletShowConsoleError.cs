using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Shows errors and exceptions readably: type, message, position, script stack, frames and inner errors.</para>
    /// <para type="description">Takes ErrorRecords (from $Error, $_ in a catch block, -ErrorVariable) and .NET exceptions from the pipeline. Each shows the type and the message, the category and error id, PowerShell's position message with the failing line, the script stack, the .NET frames as Type.Method() with shortened paths and file:line links, framework frames collapsed into "+n framework frames", and the inner exceptions as "Caused by". With -AsWidget the views are returned for New-ConsolePanel, dashboards or Export-ConsoleFrame instead of written. Redirected output is plain text.</para>
    /// </summary>
    /// <example>
    ///   <para>The last error</para>
    ///   <code>$Error[0] | Show-ConsoleError</code>
    /// </example>
    /// <example>
    ///   <para>In a catch block, with the framework frames</para>
    ///   <code>try { Get-Item C:\missing -ErrorAction Stop } catch { $_ | Show-ConsoleError -ShowFrameworkFrames }</code>
    /// </example>
    [Cmdlet(VerbsCommon.Show, "ConsoleError")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletShowConsoleError : ConsoleErrorCmdlet {
        /// <summary><para type="description">Returns the views instead of writing them.</para></summary>
        [Parameter]
        public SwitchParameter AsWidget { get; set; }

        /// <inheritdoc />
        protected override bool ReturnWidgets => AsWidget.IsPresent;
    }

    /// <summary>The parameters and output shared by Show-ConsoleError and Show-ConsoleError.</summary>
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

        /// <summary><para type="description">Uses the message-first everyday error layout, highlighted invocation and folded inner exceptions. Stacks remain hidden unless -ShowFrameworkFrames is requested.</para></summary>
        [Parameter]
        public SwitchParameter Compact { get; set; }

        /// <summary><para type="description">Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., &gt; file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.</para></summary>
        [Parameter]
        public SwitchParameter AsString { get; set; }


        /// <inheritdoc />
        protected override void BeginProcessing() => PSOutputExport.Validate(this);
        /// <summary><para type="description">Lays the output out for this width instead of the terminal's.</para></summary>
        [Parameter]
        [ValidateRange(0, 1000)]
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
                ExceptionView view = new ExceptionView(PSErrors.Describe(item, Compact.IsPresent)) {
                    ShowFrameworkFrames = ShowFrameworkFrames.IsPresent,
                    MaxFrames = Math.Max(1, MaxFrames),
                    ShowStack = !NoStack.IsPresent && (!Compact.IsPresent || ShowFrameworkFrames.IsPresent),
                    Compact = Compact.IsPresent,
                    DetailsHint = Compact.IsPresent && InvokeCommand.GetCommand("Get-Error", CommandTypes.Cmdlet) != null
                        ? Localization.WidgetLabels.For(Terminal.GetCurrentProfile().Language).GetErrorDetails : null,
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


    /// <summary>Maps PowerShell errors onto <see cref="ExceptionInfo"/>.</summary>
    internal static class PSErrors {
        // ScriptPosition contains the original extent on both 5.1 and 7. The standard reference omits this engine
        // property; PositionMessage is a display excerpt and cannot supply an accurate source span.
        private static readonly PropertyInfo? ScriptPosition = typeof(InvocationInfo).GetProperty("ScriptPosition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public static ExceptionInfo Describe(object item, bool compact = false) {
            switch (item) {
                case ErrorRecord record:
                    return FromRecord(record, compact);
                case IContainsErrorRecord contains when contains.ErrorRecord != null && item is Exception exception && contains.ErrorRecord.Exception == exception:
                    return FromRecord(contains.ErrorRecord, compact);
                case Exception exception:
                    return ExceptionInfo.FromException(exception);
                default:
                    return new ExceptionInfo("Error", item.ToString() ?? string.Empty);
            }
        }

        private static ExceptionInfo FromRecord(ErrorRecord record, bool compact) {
            ExceptionInfo inner = ExceptionInfo.FromException(record.Exception);
            string message = record.ErrorDetails?.Message is string details && details.Length > 0 ? details : record.Exception.Message;
            ExceptionInfo info = new ExceptionInfo(compact ? record.Exception.GetType().FullName ?? inner.Type : inner.Type, message) { Inner = inner.Inner };
            info.Frames.AddRange(inner.Frames);
            info.ScriptFrames.AddRange(ExceptionInfo.ParseScriptStackTrace(record.ScriptStackTrace));
            info.PositionMessage = record.InvocationInfo?.PositionMessage is string position && position.Length > 0 ? position : null;
            InvocationInfo? invocation = record.InvocationInfo;
            CommandInfo? command = invocation?.MyCommand;
            if (command != null && (command.CommandType == CommandTypes.Cmdlet || command.CommandType == CommandTypes.Function || command.CommandType == CommandTypes.Filter)) {
                info.CommandName = command.Name;
            }
            info.SourceLine = invocation?.Line;
            info.ScriptPath = invocation?.ScriptName;
            info.LineNumber = invocation?.ScriptLineNumber ?? 0;
            info.ColumnNumber = invocation?.OffsetInLine ?? 0;
            ApplyExtent(info, invocation);
            ErrorCategoryInfo category = record.CategoryInfo;
            info.Target = category?.TargetName;
            // The category only when it adds something: not "NotSpecified", not "OperationStopped" for a plain throw, and
            // without a target the message already names (the missing path).
            if (category != null && category.Category != ErrorCategory.NotSpecified
                && (compact || !(category.Category == ErrorCategory.OperationStopped && (string.IsNullOrEmpty(category.TargetName) || message.IndexOf(category.TargetName, StringComparison.OrdinalIgnoreCase) >= 0)))) {
                bool targetKnown = string.IsNullOrEmpty(category.TargetName) || message.IndexOf(category.TargetName, StringComparison.OrdinalIgnoreCase) >= 0;
                info.Category = category.Category + (compact || targetKnown ? string.Empty : " (" + category.TargetName + ")");
            }
            info.ErrorId = string.IsNullOrEmpty(record.FullyQualifiedErrorId) ? null : record.FullyQualifiedErrorId;
            return info;
        }

        private static void ApplyExtent(ExceptionInfo info, InvocationInfo? invocation) {
            if (invocation == null || ScriptPosition == null) { return; }
            try {
                if (!(ScriptPosition.GetValue(invocation, null) is IScriptExtent extent) || extent.StartColumnNumber <= 0) { return; }
                info.SourceLine = extent.StartScriptPosition.Line;
                info.ScriptPath = extent.File;
                info.LineNumber = extent.StartLineNumber;
                info.ColumnNumber = extent.StartColumnNumber;
                info.ErrorLength = Math.Max(1, extent.EndLineNumber == extent.StartLineNumber
                    ? extent.EndColumnNumber - extent.StartColumnNumber
                    : (info.SourceLine ?? string.Empty).TrimEnd('\r', '\n').Length - extent.StartColumnNumber + 1);
            } catch (Exception ex) when (ex is TargetInvocationException || ex is MemberAccessException || ex is ArgumentException) {
                // An unknown engine retains the invocation's public start position with a single caret.
            }
        }
    }
}
