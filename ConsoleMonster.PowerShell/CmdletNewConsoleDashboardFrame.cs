using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using ConsoleMonster.Frames;
using ConsoleMonster.Layout;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a dashboard frame: a summary with a title line, meters, a needs-attention list, sections, a legend and a footer.</summary>
    /// <remarks><para>For a script that builds its own summary. The frame prints once and lays itself out for the terminal's width: the facts follow the title and the states sit at its right, meters fill a row in the order given, Needs attention lists problems worst first, summary sections pack beside it, detail sections print below at full width, then the legend and the footer. Below 80 columns meters stack and sections take the whole width. Meters, states, attention items and sections are records or hashtables, the way New-ConsoleTabs takes pages. A module that owns summary objects declares its frame in C# and registers it as their default display instead.</para></remarks>
    /// <example><para>A small estate summary.</para><code>New-ConsoleDashboardFrame -Title 'Server estate' -Fact 'contoso.example', '48 servers' -State @{ State = 'Failing'; Count = 1; Word = 'failing' } -Meter @(
    ///     @{ Label = 'Servers healthy'; Healthy = 44; Warning = 3; Failing = 1 }
    ///     @{ Label = 'Backups current'; Healthy = 46; Whole = 48 }
    /// ) -Attention @{ State = 'Failing'; Identity = 'SQL-03'; Reason = 'Disk D: 97% full'; Values = '97%' } -Noun server -OutOf 48 -Section @{
    ///     Title = 'Sites'; Content = { New-ConsoleKeyValue -InputObject ([ordered] @{ London = 22; Amsterdam = 16 }) }
    /// } | Show-Console</code></example>
    /// <example>
    /// <para>Compose an inventory dashboard frame</para>
    /// <code>New-ConsoleDashboardFrame -Title 'Fleet checks' -Fact '12 servers inspected' -Meter @{
    ///     Kind = 'Count'; Label = 'Passed'; Value = 12; State = 'Healthy'
    /// } | Show-Console
    /// </code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleDashboardFrame", HelpUri = "https://consolemonster.dev/powershell/commands/new-consoledashboardframe/")]
    [OutputType(typeof(DashboardFrame))]
    public sealed class CmdletNewConsoleDashboardFrame : NoPipelineInputCmdlet {
        /// <summary><para type="description">The literal title, usually the view name.</para></summary>
        [Parameter(Mandatory = true, Position = 0), AllowEmptyString] public string Title { get; set; } = string.Empty;
        /// <summary><para type="description">Facts after the title, such as the scope and counts.</para></summary>
        [Parameter] public string[]? Fact { get; set; }
        /// <summary><para type="description">States at the right of the title line: records or hashtables with State (Healthy, Warning, Failing, Unknown or NotEvaluated), Count and Word.</para></summary>
        [Parameter] public object[]? State { get; set; }
        /// <summary><para type="description">When the data was observed; the title line shows how long ago.</para></summary>
        [Parameter] public DateTimeOffset? ObservedAt { get; set; }
        /// <summary><para type="description">Headline meters in priority order: records with Label and Kind (Composition, Ratio, Count, Fact or Missing). Composition takes Healthy, Warning, Failing, Unknown and NotEvaluated; Ratio takes Healthy, Whole and Rest; Count takes a number Value; Fact takes a text Value; Count and Fact also take Fact and State; Missing takes Reason. Without Kind, Whole means Ratio, Healthy means Composition, a number Value means Count, other Value means Fact, and no value means Missing.</para></summary>
        [Parameter] public object[]? Meter { get; set; }
        /// <summary><para type="description">Needs-attention items: records with State, Identity, Reason, and optionally Order (worst first when given) and Values (short values beside the item).</para></summary>
        [Parameter] public object[]? Attention { get; set; }
        /// <summary><para type="description">What the attention list counts, in the singular, as in '3 of 48 servers' (default item).</para></summary>
        [Parameter, ValidateNotNullOrEmpty] public string? Noun { get; set; }
        /// <summary><para type="description">The total the attention list counts against.</para></summary>
        [Parameter, ValidateRange(0, long.MaxValue)] [PSDefaultValue(Help = "0")]
        public long OutOf { get; set; }
        /// <summary><para type="description">Sections: records with Title and Content (widgets, text or a script block), and optionally Placement (Summary or Detail), Facts, Meta, States, MinWidth, MaxWidth, Grow and FullWidth.</para></summary>
        [Parameter] public object[]? Section { get; set; }
        /// <summary><para type="description">Legend entries: an ordered dictionary of state to word, or records with State and Word.</para></summary>
        [Parameter] public object? Legend { get; set; }
        /// <summary><para type="description">Counts in the footer, such as '48 servers'.</para></summary>
        [Parameter] public string[]? FooterCount { get; set; }
        /// <summary><para type="description">The footer hint: None (default, for a frame the script builds), EveryRow or EveryProperty (for frames shown for pipeline objects).</para></summary>
        [Parameter] [PSDefaultValue(Help = "None")]
        public FrameHint FooterHint { get; set; } = FrameHint.None;
        /// <summary><para type="description">An explicit length (Summary, Standard or Full); without it the session preference or Standard.</para></summary>
        [Parameter] [PSDefaultValue(Help = "Standard")]
        public ConsoleViewLength Length { get; set; } = ConsoleViewLength.Standard;

        /// <inheritdoc />
        protected override void EndProcessing() {
            var frame = new DashboardFrame(Title);
            if (MyInvocation.BoundParameters.ContainsKey(nameof(Length))) { frame.Length = Length; }
            foreach (string fact in Fact ?? Array.Empty<string>()) { frame.Header.Facts.Add(fact); }
            foreach (object item in Records(State)) {
                PSItems.RecordFields(item, "State Count Word");
                frame.Header.States.Add(FrameState.Of(Health(item, "State"), Number(item, "Count"), Text(item, "Word")));
            }
            frame.Header.ObservedAt = ObservedAt;
            foreach (object item in Records(Meter)) { frame.Meters.Add(ToMeter(item)); }
            if (Noun != null) { frame.Attention.Noun = Noun; }
            if (MyInvocation.BoundParameters.ContainsKey(nameof(OutOf))) { frame.Attention.OutOf = OutOf; }
            foreach (object item in Records(Attention)) {
                PSItems.RecordFields(item, "State Identity Reason Order Values");
                object? order = PSItems.Field(item, "Order");
                var attention = new AttentionItem(Health(item, "State"), Text(item, "Identity"), Text(item, "Reason"), order == null ? null : (IComparable)LanguagePrimitives.ConvertTo<double>(order));
                foreach (object value in PSItems.Items(PSItems.Field(item, "Values"))) { attention.Values.Add(PSWidgets.ToText(value)); }
                frame.Attention.Items.Add(attention);
            }
            foreach (object item in Records(Section)) { frame.Sections.Add(ToSection(item)); }
            foreach (object item in Records(Legend)) {
                if (PSText.Unwrap(item) is IDictionary map && !map.Contains("State")) {
                    foreach (DictionaryEntry entry in map) { frame.Legend.Add(LanguagePrimitives.ConvertTo<HealthState>(entry.Key), PSWidgets.ToText(entry.Value)); }
                    continue;
                }
                PSItems.RecordFields(item, "State Word");
                frame.Legend.Add(Health(item, "State"), Text(item, "Word"));
            }
            foreach (string count in FooterCount ?? Array.Empty<string>()) { frame.Footer.Counts.Add(count); }
            frame.Footer.Hint = FooterHint;
            WriteObject(frame);
        }

        private static IEnumerable<object> Records(object? value) => PSItems.Items(value);

        private static Meter ToMeter(object item) {
            PSItems.RecordFields(item, "Label Kind Healthy Warning Failing Unknown NotEvaluated Whole Rest Value Fact State Reason");
            string label = Text(item, "Label");
            string? kind = PSItems.Field(item, "Kind") is object k ? PSWidgets.ToText(k) : null;
            object? value = PSItems.Field(item, "Value");
            if (kind == null) {
                kind = PSItems.Field(item, "Whole") != null ? "Ratio" : PSItems.Field(item, "Healthy") != null ? "Composition"
                    : value == null ? "Missing" : IsNumber(value) ? "Count" : "Fact";
            }
            string? fact = PSItems.Field(item, "Fact") is object f ? PSWidgets.ToText(f) : null;
            HealthState state = PSItems.Field(item, "State") == null ? HealthState.Healthy : Health(item, "State");
            switch (kind.ToLowerInvariant()) {
                case "composition":
                    return Frames.Meter.Composition(label, Number(item, "Healthy"), Number(item, "Warning"), Number(item, "Failing"), Number(item, "Unknown"), Number(item, "NotEvaluated"));
                case "ratio":
                    return Frames.Meter.Ratio(label, Number(item, "Healthy"), Number(item, "Whole"), PSItems.Field(item, "Rest") == null ? HealthState.Failing : Health(item, "Rest"));
                case "count":
                    return Frames.Meter.Count(label, Number(item, "Value"), fact, state);
                case "fact":
                    return Frames.Meter.Fact(label, Text(item, "Value"), fact, state);
                case "missing":
                    return Frames.Meter.Missing(label, PSItems.Field(item, "Reason") is object r ? PSWidgets.ToText(r) : null);
                default:
                    throw new PSArgumentException("Meter Kind '" + kind + "' is not Composition, Ratio, Count, Fact or Missing.");
            }
        }

        private static FrameSection ToSection(object item) {
            PSItems.RecordFields(item, "Title Content Placement Facts Meta States MinWidth MaxWidth Grow FullWidth");
            List<IWidget> children = PSWidgets.Collect(PSItems.Items(PSItems.Field(item, "Content")).Select(PSObject.AsPSObject));
            IWidget body = children.Count == 1 ? children[0] : new Rows(children.ToArray());
            var section = new FrameSection(Text(item, "Title"), _ => body);
            if (PSItems.Field(item, "Placement") is object placement) { section.Placement = LanguagePrimitives.ConvertTo<FramePlacement>(placement); }
            foreach (object fact in PSItems.Items(PSItems.Field(item, "Facts"))) { section.Facts.Add(PSWidgets.ToText(fact)); }
            if (PSItems.Field(item, "Meta") is object meta) { section.Meta = PSWidgets.ToText(meta); }
            foreach (object state in PSItems.Items(PSItems.Field(item, "States"))) {
                object record = Binding.PSObjectMemberSource.Identity(state);
                PSItems.RecordFields(record, "State Count Word");
                section.States.Add(FrameState.Of(Health(record, "State"), Number(record, "Count"), Text(record, "Word")));
            }
            if (PSItems.Field(item, "MinWidth") is object min) { section.MinWidth = LanguagePrimitives.ConvertTo<int>(min); }
            if (PSItems.Field(item, "MaxWidth") is object max) { section.MaxWidth = LanguagePrimitives.ConvertTo<int>(max); }
            if (PSItems.Field(item, "Grow") is object grow) { section.Grow = LanguagePrimitives.ConvertTo<double>(grow); }
            if (PSItems.Field(item, "FullWidth") is object full) { section.FullWidth = LanguagePrimitives.IsTrue(full); }
            return section;
        }

        private static HealthState Health(object item, string name) {
            object? value = PSItems.Field(item, name) ?? throw new PSArgumentException("A record needs " + name + ".");
            try { return LanguagePrimitives.ConvertTo<HealthState>(value); }
            catch (PSInvalidCastException) { throw new PSArgumentException("'" + PSWidgets.ToText(value) + "' is not a state for " + name + ". Use Healthy, Warning, Failing, Unknown or NotEvaluated."); }
        }

        private static long Number(object item, string name) => PSItems.Field(item, name) is object value ? LanguagePrimitives.ConvertTo<long>(value) : 0;

        private static string Text(object item, string name) => PSItems.Field(item, name) is object value ? PSWidgets.ToText(value) : throw new PSArgumentException("A record needs " + name + ".");

        private static bool IsNumber(object value) => (PSText.Unwrap(value) ?? value) is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}
