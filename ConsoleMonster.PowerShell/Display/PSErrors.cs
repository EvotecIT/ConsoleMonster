using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
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
