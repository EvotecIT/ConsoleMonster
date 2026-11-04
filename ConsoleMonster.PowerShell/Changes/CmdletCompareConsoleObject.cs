using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Changes;
using ConsoleMonster.PowerShell.Binding;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Compares object sets or shows property drift across a baseline matrix.</para>
    /// <para type="description">ReferenceObject and DifferenceObject are matched by Property (or Id, Key, Name and similar members), then compared by nested property path. Keys and string values compare case-insensitively by default; CaseSensitive applies to both. Case-only differences remain visible with IsCaseOnly and do not increase changed counts. Missing properties use a localized marker, distinct from present null or empty values. Normalize maps exact paths to scriptblocks receiving $_ or $PSItem; comparison uses their results while display retains the originals. Full, Summary, Table, SideBySide and Unified show two-set comparisons, including line diffs of long values. InputObject selects a baseline matrix with one property per row and one object per column. Baseline chooses a column by name or zero-based integer index; Name or NameProperty sets headings. Equal rows are hidden unless IncludeEqual is set; case-only rows remain visible. PassThru returns ObjectChange records or BaselineRow records without drawing. AsWidget returns an IWidget for Show-ConsoleApp layouts and Export-ConsoleFrame.</para>
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
    /// <example>
    /// <para>Compare three domain controller settings against dc02.</para>
    /// <code>$controllers = @(
    ///     [pscustomobject]@{ Name = 'dc01'; Site = 'HQ'; LdapPort = 389 }
    ///     [pscustomobject]@{ Name = 'dc02'; Site = 'HQ'; LdapPort = 636 }
    ///     [pscustomobject]@{ Name = 'dc03'; Site = 'Branch'; LdapPort = 636 }
    /// )
    /// $controllers | Compare-ConsoleObject -View Baseline -NameProperty Name -Baseline dc02 -Ignore Name -Title 'Domain controllers'</code>
    /// </example>
    /// <example>
    /// <para>Normalize host-specific noise while retaining the original displayed values.</para>
    /// <code>$normalizers = @{ ServicePrincipalName = { $_ -replace 'dc[0-9]+', 'HOST' } }
    /// $widget = Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name -Normalize $normalizers -AsWidget
    /// $widget | Export-ConsoleFrame -Format Html -Width 120 -Path ./comparison.html</code>
    /// </example>
    [Cmdlet(VerbsData.Compare, "ConsoleObject", DefaultParameterSetName = "Compare", SupportsShouldProcess = true)]
    [OutputType(typeof(ObjectChange), typeof(BaselineRow), typeof(ChangeView), typeof(BaselineView), typeof(bool))]
    public sealed class CmdletCompareConsoleObject : PSCmdlet {
        /// <summary><para type="description">The old objects.</para></summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Compare")]
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = "Confirm")]
        [AllowEmptyCollection]
        [AllowNull]
        public object[]? ReferenceObject { get; set; }

        /// <summary><para type="description">The new objects.</para></summary>
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Compare")]
        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Confirm")]
        [AllowEmptyCollection]
        [AllowNull]
        public object[]? DifferenceObject { get; set; }

        /// <summary><para type="description">Two or more objects to compare as a baseline matrix, in column order. Accepts pipeline input.</para></summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Baseline")]
        [AllowNull]
        [AllowEmptyCollection]
        public object[]? InputObject { get; set; }

        /// <summary><para type="description">One explicit column name per input object.</para></summary>
        [Parameter(ParameterSetName = "Baseline")]
        public string[]? Name { get; set; }

        /// <summary><para type="description">The property supplying column names. Defaults to Property or the normal Id, Key, Name discovery.</para></summary>
        [Parameter(ParameterSetName = "Baseline")]
        public string? NameProperty { get; set; }

        /// <summary><para type="description">The reference column name or zero-based integer index. Default zero. A numeric string is a name.</para></summary>
        [Parameter(ParameterSetName = "Baseline")]
        public object? Baseline { get; set; }

        /// <summary><para type="description">Compares both match keys and string values with ordinal case sensitivity. By default case-only differences remain visible but do not increase changed counts.</para></summary>
        [Parameter]
        public SwitchParameter CaseSensitive { get; set; }

        /// <summary><para type="description">A hashtable of exact property paths to comparison-only scriptblocks. Each receives its original value as $_ and $PSItem. Display and structured values remain original.</para></summary>
        [Parameter]
        public Hashtable? Normalize { get; set; }

        /// <summary><para type="description">Returns a composable ChangeView or BaselineView without drawing. Cannot be combined with PassThru or Question.</para></summary>
        [Parameter(ParameterSetName = "Compare")]
        [Parameter(ParameterSetName = "Baseline")]
        public SwitchParameter AsWidget { get; set; }

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

        /// <summary><para type="description">Lists unchanged objects or baseline rows too. Case-only differences are always visible.</para></summary>
        [Parameter]
        public SwitchParameter IncludeEqual { get; set; }

        /// <summary><para type="description">The view: Full (default), Summary, Table, SideBySide, Unified or Baseline (with InputObject).</para></summary>
        [Parameter]
        public ChangeViewMode View { get; set; } = ChangeViewMode.Full;

        /// <summary><para type="description">A title (markup).</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <summary><para type="description">Returns the changes as objects instead of showing them.</para></summary>
        [Parameter(ParameterSetName = "Compare")]
        [Parameter(ParameterSetName = "Baseline")]
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
            object[]? input = ParameterSetName == "Baseline" ? InputObject : DifferenceObject;
            if (input != null) {
                _difference.AddRange(input);
            }
        }

        /// <inheritdoc />
        protected override void EndProcessing() {
            if (PassThru.IsPresent && AsWidget.IsPresent) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("Choose either PassThru or AsWidget."), "ComparisonOutputConflict", ErrorCategory.InvalidArgument, null));
            }
            DiffOptions diffOptions = PSChanges.Options(Property, CompareProperty, Ignore, Depth, IncludeEqual.IsPresent, CaseSensitive.IsPresent, Normalize);
            if (ParameterSetName == "Baseline") {
                WriteBaseline(diffOptions);
                return;
            }
            if (View == ChangeViewMode.Baseline) {
                ThrowTerminatingError(new ErrorRecord(new ArgumentException("View Baseline requires InputObject."), "BaselineInputRequired", ErrorCategory.InvalidArgument, View));
            }
            ChangeSet changes = ObjectDiff.Compare(PSChanges.Unwrap(ReferenceObject), PSChanges.Unwrap(_difference), diffOptions);
            var widget = new ChangeView(changes, View) { Title = PSText.Markup(Title), MoreHint = "-PassThru or -View SideBySide" };
            if (AsWidget.IsPresent) { WriteObject(widget); return; }
            if (PassThru.IsPresent) {
                foreach (ObjectChange change in changes.Changes) {
                    WriteObject(change);
                }
                return;
            }
            PSFormatting.Write(this, widget, 0);
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
        private void WriteBaseline(DiffOptions options) {
            try {
                if (MyInvocation.BoundParameters.ContainsKey(nameof(View)) && View != ChangeViewMode.Baseline) {
                    throw new ArgumentException("InputObject uses View Baseline.");
                }
                if (!string.IsNullOrEmpty(NameProperty)) {
                    options.Key.Clear();
                    options.Key.Add(NameProperty!);
                }
                object?[] objects = PSChanges.Unwrap(_difference).ToArray();
                options.Baseline = Baseline == null ? null : PSObjectMemberSource.UnwrapPSObject(Baseline);
                BaselineComparison comparison = ObjectDiff.CompareBaseline(objects, options, Name);
                if (PassThru.IsPresent) {
                    foreach (BaselineRow row in comparison.Rows) { WriteObject(row); }
                } else {
                    var widget = new BaselineView(comparison) { Title = PSText.Markup(Title) };
                    if (AsWidget.IsPresent) { WriteObject(widget); } else { PSFormatting.Write(this, widget, 0); }
                }
            } catch (ArgumentException exception) {
                ThrowTerminatingError(new ErrorRecord(exception, "InvalidBaselineComparison", ErrorCategory.InvalidArgument, Baseline));
            }
        }
    }

    internal static class PSChanges {
        internal static DiffOptions Options(string[]? key, string[]? compare, string[]? ignore, int depth, bool includeEqual, bool caseSensitive, Hashtable? normalize) {
            DiffOptions options = new DiffOptions { Depth = Math.Max(1, depth), IncludeUnchanged = includeEqual, CaseSensitive = caseSensitive, Unwrap = PSObjectMemberSource.UnwrapPSObject };
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
            if (normalize != null) {
                foreach (DictionaryEntry entry in normalize) {
                    if (!(entry.Key is string path) || !(entry.Value is ScriptBlock script)) {
                        throw new ArgumentException("Normalize maps property paths to scriptblocks.", nameof(normalize));
                    }
                    options.Normalize[path] = value => {
                        var variables = new List<PSVariable> { new PSVariable("_", value), new PSVariable("PSItem", value) };
                        var result = script.InvokeWithContext(null, variables, Array.Empty<object>());
                        if (result.Count == 0) { return null; }
                        if (result.Count == 1) { return PSObjectMemberSource.UnwrapPSObject(result[0]); }
                        return result.Select(item => PSObjectMemberSource.UnwrapPSObject(item)).ToArray();
                    };
                }
            }
            return options;
        }

        // Scalars compare by value; objects keep their PowerShell wrapper so note properties are read.
        internal static IEnumerable<object?> Unwrap(IEnumerable<object?>? items) {
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
