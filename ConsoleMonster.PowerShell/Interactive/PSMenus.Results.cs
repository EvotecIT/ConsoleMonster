using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    internal static partial class PSMenus {
        // The PowerShell view of the existing context accepts result records at the transport boundary.
        // All navigation and execution remain on MenuContext; no parallel menu runner is introduced.
        private static PSObject Context(MenuContext context) {
            var adapted = new PSObject(context);
            adapted.Members.Add(new PSCodeMethod("Run", typeof(PSMenus).GetMethod(nameof(Run))!));
            adapted.Members.Add(new PSCodeMethod("ShowResult", typeof(PSMenus).GetMethod(nameof(ShowResult))!));
            return adapted;
        }

        /// <summary>Invokes PowerShell work and converts its single result record for the existing menu run.</summary>
        public static MenuRunResult Run(PSObject instance, IWidget dashboard, ScriptBlock work, LiveSessionOptions? options = null) {
            var context = (MenuContext)instance.BaseObject;
            return context.Run(dashboard, run => {
                var variables = new List<PSVariable> { new PSVariable("MenuContext", Context(context)), new PSVariable("RunContext", run) };
                var output = work.InvokeWithContext(null, variables, run);
                if (output.Count == 0) { return null; }
                if (output.Count != 1) { throw new PSArgumentException("A menu run returns one result record with Title, Status, Summary, Figure and Next."); }
                return Result(output[0]);
            }, options);
        }

        /// <summary>Displays a result record through the existing menu result page.</summary>
        public static void ShowResult(PSObject instance, object result) => ((MenuContext)instance.BaseObject).ShowResult(Result(result));

        private static MenuRunResult Result(object value) {
            if (PSText.Unwrap(value) is MenuRunResult typed) { return typed; }
            if (PSText.Unwrap(value) is IDictionary dictionary) { value = dictionary; }
            PSItems.RecordFields(value, "Title Status Summary Figure FigureStatus Next ElapsedSeconds Details");
            string title = PSText.PlainText(PSItems.Field(value, "Title"));
            if (string.IsNullOrWhiteSpace(title)) { throw new PSArgumentException("A menu result record needs Title."); }
            object? status = PSItems.Field(value, "Status");
            StatusKind kind = StatusKind.Pass;
            if (status != null && !Binding.StatusArguments.TryParse(status, out kind)) { throw new PSArgumentException("A menu result Status must be a supported status word."); }
            var result = new MenuRunResult(title, kind) {
                Summary = PSItems.Field(value, "Summary") is object summary ? PSText.PlainText(summary) : null,
                Details = PSWidgets.AsWidget(PSItems.Field(value, "Details"))
            };
            if (PSItems.Field(value, "ElapsedSeconds") is object elapsed) {
                double seconds = LanguagePrimitives.ConvertTo<double>(elapsed);
                if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) { throw new PSArgumentException("ElapsedSeconds must be finite and nonnegative."); }
                result.Elapsed = TimeSpan.FromSeconds(seconds);
            }
            object? figures = PSItems.Field(value, "Figure");
            if (figures != null && !(figures is IDictionary)) { throw new PSArgumentException("Figure must map labels to values."); }
            if (figures is IDictionary values) {
                object? figureStatuses = PSItems.Field(value, "FigureStatus");
                if (figureStatuses != null && !(figureStatuses is IDictionary)) { throw new PSArgumentException("FigureStatus must map figure labels to status words."); }
                foreach (DictionaryEntry figure in values) {
                    string label = PSWidgets.ToText(figure.Key);
                    object? figureStatus = figureStatuses == null ? null : PSItems.Field(figureStatuses, label);
                    if (figureStatus == null) { result.Figure(label, PSWidgets.ToText(figure.Value)); }
                    else if (Binding.StatusArguments.TryParse(figureStatus, out StatusKind figureKind)) { result.Figure(label, PSWidgets.ToText(figure.Value), figureKind); }
                    else { throw new PSArgumentException("FigureStatus for '" + label + "' is not a supported status word."); }
                }
            }
            object? next = PSItems.Field(value, "Next");
            if (next != null && !(next is IDictionary)) { throw new PSArgumentException("Next must map action titles to script blocks."); }
            if (next is IDictionary actions) {
                foreach (DictionaryEntry action in actions) {
                    if (!(PSText.Unwrap(action.Value) is ScriptBlock script)) { throw new PSArgumentException("Each Next action needs a script block."); }
                    result.Next(PSWidgets.ToText(action.Key), context => Invoke(script, context));
                }
            }
            return result;
        }
    }
}
