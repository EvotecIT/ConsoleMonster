using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Changes;
using ConsoleMonster.PowerShell.Binding;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Compares two sets of objects and shows what was added, removed and changed, property by property.</para>
    /// <para type="description">Like Compare-Object, with a much better display: objects are matched by -Property (or by Id, Name, Key and similar members), then compared property by property, nested objects by path. The view shows the counts by kind and a table of the changes in the design-system tones (added in green, removed in red, changed in yellow), with line diffs of long and multi-line values; -View picks Summary, Table, SideBySide or Unified. -PassThru returns the structured changes (ConsoleMonster.Changes.ObjectChange) instead.</para>
    /// </summary>
    /// <example>
    /// <para>Compare two small inventories</para>
    /// <code>$before = @([pscustomobject]@{ Name = 'web01'; Port = 80 })
    /// $after = @([pscustomobject]@{ Name = 'web01'; Port = 443 })
    /// Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name</code>
    /// </example>
    /// <example>
    /// <para>Ask before accepting a configuration change</para>
    /// <code>$before = @([pscustomobject]@{ Name = 'web01'; Port = 80 })
    /// $after = @([pscustomobject]@{ Name = 'web01'; Port = 443 })
    /// $accepted = Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name -Question 'Accept these changes?'
    /// $accepted</code>
    /// </example>
    [Cmdlet(VerbsData.Compare, "ConsoleObject", DefaultParameterSetName = "Compare", SupportsShouldProcess = true)]
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
        [Parameter(ParameterSetName = "Compare")]
        public SwitchParameter PassThru { get; set; }

        /// <summary><para type="description">Shows the changes, asks this question and returns a Boolean answer.</para></summary>
        [Parameter(Mandatory = true, ParameterSetName = "Confirm")]
        public string? Question { get; set; }
        /// <summary><para type="description">Text that must be typed to confirm the preview.</para></summary>
        [Parameter(ParameterSetName = "Confirm")]
        public string? TypedConfirmation { get; set; }
        /// <summary><para type="description">Accepts a nonempty change set without asking. WhatIf still returns false.</para></summary>
        [Parameter(ParameterSetName = "Confirm")]
        public SwitchParameter Force { get; set; }
        /// <summary><para type="description">Uses a yes/no question for removals unless TypedConfirmation is supplied.</para></summary>
        [Parameter(ParameterSetName = "Confirm")]
        public SwitchParameter NoTypedWhenDestructive { get; set; }

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
            if (Question != null) {
                if (!changes.HasChanges) { WriteObject(false); return; }
                string target = PSText.PlainText(Title ?? string.Empty);
                string summary = changes.Added + " added, " + changes.Removed + " removed, " + changes.Changed + " changed";
                bool whatIf = MyInvocation.BoundParameters.TryGetValue("WhatIf", out object? preview)
                    ? LanguagePrimitives.IsTrue(preview) : LanguagePrimitives.IsTrue(SessionState.PSVariable.GetValue("WhatIfPreference"));
                if (whatIf) { ShouldProcess(target.Length == 0 ? summary : target, summary); WriteObject(false); return; }
                bool noConfirm = MyInvocation.BoundParameters.TryGetValue("Confirm", out object? confirm) && !LanguagePrimitives.IsTrue(confirm);
                if (Force.IsPresent || noConfirm) { WriteObject(true); return; }
                var options = new ChangePreviewOptions { Title = PSText.Markup(Title), Mode = View, Question = Question,
                    TypedConfirmation = TypedConfirmation, TypedWhenDestructive = !NoTypedWhenDestructive.IsPresent,
                    Interactive = PSInteractive.Options(this, null) };
                WriteObject(ChangePreview.Ask(changes, options));
            }
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
