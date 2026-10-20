using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Changes;
using ConsoleMonster.PowerShell.Binding;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Compares two sets of objects and shows what was added, removed and changed, property by property.</para>
    /// <para type="description">Like Compare-Object, with a much better display: objects are matched by -Property (or by Id, Name, Key and similar members), then compared property by property, nested objects by path. The view shows the counts by kind and a table of the changes in the design-system tones (added in green, removed in red, changed in yellow), with line diffs of long and multi-line values; -View picks Summary, Table, SideBySide or Unified. -PassThru returns the structured changes (ConsoleMonster.Changes.ObjectChange) instead.</para>
    /// <example>
    ///   <para>What changed between two exports</para>
    ///   <code>Compare-ConsoleObject (Import-Csv .\before.csv) (Import-Csv .\after.csv) -Property SamAccountName</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Compare, "ConsoleObject")]
    [OutputType(typeof(ObjectChange))]
    public sealed class CmdletCompareConsoleObject : PSCmdlet {
        /// <summary><para type="description">The old objects.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [AllowEmptyCollection]
        [AllowNull]
        public object[]? ReferenceObject { get; set; }

        /// <summary><para type="description">The new objects.</para></summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
        [AllowEmptyCollection]
        [AllowNull]
        public object[]? DifferenceObject { get; set; }

        /// <summary><para type="description">The properties that identify an object (matching key). Default: Id, Key, Name and similar members.</para></summary>
        [Parameter]
        public string[]? Property { get; set; }

        /// <summary><para type="description">The properties to compare (default all).</para></summary>
        [Parameter]
        public string[]? CompareProperty { get; set; }

        /// <summary><para type="description">Properties or paths (Address.City, wildcards) left out of the comparison.</para></summary>
        [Parameter]
        public string[]? Ignore { get; set; }

        /// <summary><para type="description">How deep nested objects are compared (default 3).</para></summary>
        [Parameter]
        public int Depth { get; set; } = 3;

        /// <summary><para type="description">Lists unchanged objects too.</para></summary>
        [Parameter]
        public SwitchParameter IncludeEqual { get; set; }

        /// <summary><para type="description">The view: Full (default), Summary, Table, SideBySide or Unified.</para></summary>
        [Parameter]
        public ChangeViewMode View { get; set; } = ChangeViewMode.Full;

        /// <summary><para type="description">A title (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">Returns the changes as objects instead of showing them.</para></summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        private readonly List<object?> _difference = new List<object?>();

        /// <inheritdoc />
        protected override void ProcessRecord() {
            if (DifferenceObject != null) {
                _difference.AddRange(DifferenceObject);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ChangeSet changes = PSChanges.Compare(ReferenceObject, _difference, Property, CompareProperty, Ignore, Depth, IncludeEqual.IsPresent);
            if (PassThru.IsPresent) {
                foreach (ObjectChange change in changes.Changes) {
                    WriteObject(change);
                }
                return;
            }
            PSFormatting.Write(this, new ChangeView(changes, View) { Title = PSText.Markup(Title), MoreHint = "-PassThru or -View SideBySide" }, 0);
        }
    }

    /// <summary>
    /// <para type="synopsis">Shows the changes an action would make and asks before making them: the what-if pattern.</para>
    /// <para type="description">Compares -Before with -After (as Compare-ConsoleObject does), shows the preview, then asks for confirmation and returns $true or $false; act only on $true. Changes that remove objects need the number of removals typed (or the -TypedConfirmation text). It honours the caller's -WhatIf: the preview is shown and $false returned without asking. -Force (or -Confirm:$false) skips the question and returns $true when there are changes. Use it inside your own functions: if (Show-ConsoleChanges -Before $current -After $planned -Property Name) { apply }.</para>
    /// <example>
    ///   <para>Preview a group membership change</para>
    ///   <code>if (Show-ConsoleChanges -Before $members -After $planned -Property SamAccountName -Title 'Domain Admins') { Set-Members $planned }</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "ConsoleChanges", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(bool))]
    public sealed class CmdletShowConsoleChanges : PSCmdlet {
        /// <summary><para type="description">The current objects.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        [AllowEmptyCollection]
        [AllowNull]
        public object[]? Before { get; set; }

        /// <summary><para type="description">The objects after the change.</para></summary>
        [Parameter(Position = 1, Mandatory = true)]
        [AllowEmptyCollection]
        [AllowNull]
        public object[]? After { get; set; }

        /// <summary><para type="description">The properties that identify an object (matching key).</para></summary>
        [Parameter]
        public string[]? Property { get; set; }

        /// <summary><para type="description">Properties or paths left out of the comparison.</para></summary>
        [Parameter]
        public string[]? Ignore { get; set; }

        /// <summary><para type="description">The view of the preview: Full (default), Summary, Table, SideBySide or Unified.</para></summary>
        [Parameter]
        public ChangeViewMode View { get; set; } = ChangeViewMode.Full;

        /// <summary><para type="description">A title (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">The question (default "Apply these changes?").</para></summary>
        [Parameter]
        public string? Question { get; set; }

        /// <summary><para type="description">Text that must be typed to confirm (for example the name of what is changed).</para></summary>
        [Parameter]
        public string? TypedConfirmation { get; set; }

        /// <summary><para type="description">Skips the question and returns $true when there are changes.</para></summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            ChangeSet changes = PSChanges.Compare(Before, After, Property, null, Ignore, 3, false);
            ChangePreviewOptions options = new ChangePreviewOptions { Title = PSText.Markup(Title), Mode = View, Question = Question, TypedConfirmation = TypedConfirmation };
            PSFormatting.Write(this, new ChangeView(changes, View) { Title = options.Title, MoreHint = "-View SideBySide" }, 0);
            if (!changes.HasChanges) {
                HostOutput.WriteMarkup(this, "[muted]" + Text.Markup.Escape(Localization.WidgetLabels.Current.ChangesNone) + "[/]");
                WriteObject(false);
                return;
            }
            string target = PSText.PlainText(Title ?? string.Empty);
            string summary = changes.Added + " added, " + changes.Removed + " removed, " + changes.Changed + " changed";
            if (WhatIfRequested()) {
                ShouldProcess(target.Length == 0 ? summary : target, summary);
                WriteObject(false);
                return;
            }
            if (Force.IsPresent || ConfirmDeclinedExplicitly()) {
                WriteObject(true);
                return;
            }
            options.Interactive = PSInteractive.Options(this, null);
            WriteObject(ChangePreview.Ask(changes, options));
        }

        private bool WhatIfRequested() {
            if (MyInvocation.BoundParameters.TryGetValue("WhatIf", out object? value)) {
                return value is SwitchParameter s ? s.IsPresent : LanguagePrimitives.IsTrue(value);
            }
            return LanguagePrimitives.IsTrue(SessionState.PSVariable.GetValue("WhatIfPreference"));
        }

        private bool ConfirmDeclinedExplicitly() {
            return MyInvocation.BoundParameters.TryGetValue("Confirm", out object? value) && value is SwitchParameter s && !s.IsPresent;
        }
    }

    internal static class PSChanges {
        internal static ChangeSet Compare(object[]? before, IEnumerable<object?>? after, string[]? key, string[]? compare, string[]? ignore, int depth, bool includeEqual) {
            DiffOptions options = new DiffOptions { Depth = Math.Max(1, depth), IncludeUnchanged = includeEqual, Unwrap = PSObjectMemberSource.UnwrapPSObject };
            options.Sources.Add(new DirectoryEntryMemberSource());
            options.Sources.Add(new PSObjectMemberSource());
            foreach (string name in key ?? Array.Empty<string>()) {
                options.Key.Add(name);
            }
            foreach (string name in compare ?? Array.Empty<string>()) {
                options.Properties.Add(name);
            }
            foreach (string name in ignore ?? Array.Empty<string>()) {
                options.Ignore.Add(name);
            }
            return ObjectDiff.Compare(Unwrap(before), Unwrap(after), options);
        }

        // Scalars compare by value; objects keep their PowerShell wrapper so note properties are read.
        private static IEnumerable<object?> Unwrap(IEnumerable<object?>? items) {
            if (items == null) {
                yield break;
            }
            foreach (object? item in items) {
                // Scalars, dictionaries, collections and data values compare as themselves; other objects keep their
                // PowerShell wrapper so note and script properties are read.
                yield return item == null ? null : PSObjectMemberSource.UnwrapPSObject(item);
            }
        }
    }
}
