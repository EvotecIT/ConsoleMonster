using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Layout;
using ConsoleMonster.PowerShell.Display;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Builds the same default views for exported and shown objects that Out-Default uses: module views registered through the
    /// display runtime (schema 2 .consoleviews.psd1 files and product leases). An explicit column registration keeps precedence,
    /// as it does on the console, and a failing builder falls back to the automatic view.
    /// </summary>
    internal static class PSDefaultViews {
        // Another module can load an older display copy first (one assembly identity per ABI epoch). Copies before 2.1.3 do not
        // expose their internals to this assembly, so those calls stay in separate methods and their failure disables this route.
        private static volatile bool _internalsUnavailable;

        /// <summary>The view and batch scope for one object, or null when the automatic view applies.</summary>
        internal static Run? Find(PSObject value) {
            if (_internalsUnavailable) { return null; }
            try {
                if (ConsoleFormatting.Find(value, false) != null) { return null; }
                DefaultDisplayRegistration? view = DefaultDisplay.Resolve(value);
                if (view == null) { return null; }
                return new Run(view, view.ScopeSelector?.Invoke(value.BaseObject) ?? Scope(view, value.BaseObject));
            } catch (Exception error) when (!(error is PipelineStoppedException)) {
                return null;
            }
        }

        /// <summary>Builds one completed run, or returns null so the caller uses the automatic view.</summary>
        internal static IWidget? Build(Run run, IReadOnlyList<PSObject> rows, ExportOptions? export, PSCmdlet cmdlet) {
            DefaultDisplayRegistration view = run.View;
            // The broker's record ceiling applies to collected batches; a stream view renders any number of rows.
            if (view.Batch == DefaultDisplayBatch.Collect && rows.Count > view.Budget.Records) { return null; }
            object[] values = new object[rows.Count];
            for (int i = 0; i < rows.Count; i++) { values[i] = rows[i].BaseObject; }
            try {
                if (view.Batch != DefaultDisplayBatch.Single) {
                    long bytes = 0;
                    foreach (object value in values) {
                        bytes += Estimate(value, view.Budget.Bytes);
                        if (bytes > view.Budget.Bytes) { return null; }
                    }
                }
                DefaultDisplayContext context = new DefaultDisplayContext(Profile(export), run.Scope, DefaultDisplayMode.Auto);
                switch (view.Batch) {
                    case DefaultDisplayBatch.Stream:
                        // An export is a completed scope, so it takes the same builders as a stream that closes within its sample.
                        if (values.Length == 1 && view.SingleRecordBuilder != null) { return Prepare(view.SingleRecordBuilder(values[0], context), context, view.Budget.Bytes); }
                        return Prepare(view.CollectedBuilder != null ? view.CollectedBuilder(values, context) : StreamTable(view, context, values), context, view.Budget.Bytes);
                    case DefaultDisplayBatch.Single:
                        List<IWidget> each = new List<IWidget>(values.Length);
                        foreach (object value in values) { each.Add(Prepare(view.Build(new[] { value }, context), context, view.Budget.Bytes)); }
                        return PSFormatting.Combine(each);
                    default:
                        return Prepare(view.Build(values, context), context, view.Budget.Bytes);
                }
            } catch (Exception error) when (!(error is PipelineStoppedException)) {
                cmdlet.WriteVerbose("ConsoleMonster view '" + view.ViewId + "' used the automatic view: " + error.Message);
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string Scope(DefaultDisplayRegistration view, object value) {
            try { return ReadScope(view, value); }
            catch (Exception error) when (error is MemberAccessException || error is TypeLoadException) {
                _internalsUnavailable = true;
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ReadScope(DefaultDisplayRegistration view, object value) => DisplayData.Scope(value, view.ScopeKey);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long Estimate(object value, long ceiling) => DisplayData.Estimate(value, ceiling);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IWidget Prepare(IWidget widget, DefaultDisplayContext context, long maximumBytes) {
            DefaultDisplayRenderer.Prepare(widget, context.Profile, maximumBytes);
            return widget;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IWidget StreamTable(DefaultDisplayRegistration view, DefaultDisplayContext context, object[] values) => DisplayStreamSample.BuildTable(view, context, values);

        private static TerminalProfile Profile(ExportOptions? export) {
            if (export == null) { return Terminal.GetCurrentProfile(); }
            TerminalProfile profile = TerminalProfile.Fixed(Terminal.ResolveWidth(export.Width), export.Height > 0 ? export.Height : 24, export.ColorDepth, export.Glyphs);
            return export.Culture != null ? profile.WithCulture(export.Culture) : profile;
        }

        /// <summary>Consecutive objects that share one registration and scope render as one batch.</summary>
        internal sealed class Run : IEquatable<Run> {
            internal Run(DefaultDisplayRegistration view, string scope) { View = view; Scope = scope; }
            internal DefaultDisplayRegistration View { get; }
            internal string Scope { get; }
            public bool Equals(Run? other) => other != null && ReferenceEquals(View, other.View) && string.Equals(Scope, other.Scope, StringComparison.Ordinal);
            public override bool Equals(object? obj) => Equals(obj as Run);
            public override int GetHashCode() => View.GetHashCode() ^ StringComparer.Ordinal.GetHashCode(Scope);
        }
    }
}
