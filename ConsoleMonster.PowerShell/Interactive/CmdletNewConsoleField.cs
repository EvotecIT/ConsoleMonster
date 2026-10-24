using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a form field for New-ConsoleForm.</para>
    /// <para type="description">A field has a name (its key in the result), a label, help shown while it has focus, a default, validation (-Required, -Validator and the type's own checks) and the command-line parameter it supplies (-ParameterName) for the equivalent command. -VisibleWhen shows it only while a condition on the other values holds ($Values). -DependsOn resets it when those fields change; with -LoadChoices its options are loaded again in their own runspace (a spinner shows, Esc cancels), from the current values ($Values). Choices are built from any objects: -Display, -GroupBy, -Describe, -Badge, -DisabledReason and -Selected read each object (a property name, or a scriptblock for choices given with -Choices; loaded choices take property names), and -CommandValue picks the text the equivalent command shows and the field returns.</para>
    /// </summary>
    /// <example>
    ///   <para>Dependent fields: picking a site loads its servers</para>
    ///   <code>
    /// New-ConsoleField Site 'Site' -Type Select -Choices 'Warsaw', 'Berlin' -ParameterName Site
    /// New-ConsoleField Server 'Servers' -Type MultiSelect -DependsOn Site -LoadChoices { param($Values) Get-AppServer -Site $Values.Site } -Display Name -ParameterName Server</code>
    /// </example>
    /// <example>
    ///   <para>A conditional field</para>
    ///   <code>
    /// New-ConsoleField Notify 'Send an email' -Type Toggle
    /// New-ConsoleField Email 'Email' -VisibleWhen { $Values.Notify } -Validator { $_ -match '@' }</code>
    /// </example>
    /// <example>
    ///   <para>Loaded servers grouped by site, with their role as a badge, offline ones disabled and online ones selected; the command shows their names</para>
    ///   <code>
    /// New-ConsoleField Servers 'Servers' -Type MultiSelect -DependsOn Domain -ParameterName ComputerName -LoadChoices {
    ///     param($Values)
    ///     Get-AppServer -Domain $Values.Domain | ForEach-Object {
    ///         [pscustomobject] @{ Name = $_.Name; Site = $_.Site; Role = $_.Role; Offline = $_.OfflineReason; Online = -not $_.OfflineReason }
    ///     }
    /// } -Display Name -CommandValue Name -GroupBy Site -Badge Role -DisabledReason Offline -Selected Online</code>
    /// </example>
    /// <example>
    ///   <para>Checks offered by name, returned and shown in the command as their ids, with a typical time as a badge</para>
    ///   <code>New-ConsoleField Checks 'Checks' -Type MultiSelect -Choices $checks -Display Name -CommandValue Id -GroupBy Category -Badge { "~$($_.Seconds) s" } -ParameterName Check</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleField")]
    [OutputType(typeof(Field))]
    public sealed class CmdletNewConsoleField : PSCmdlet {
        /// <summary><para type="description">The key of the value in the result.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The label (default: the name).</para></summary>
        [Parameter(Position = 1)]
        public string? Label { get; set; }

        /// <summary><para type="description">The kind: Text (default), Secret, Number, Toggle, Select, MultiSelect or Path.</para></summary>
        [Parameter]
        public ConsoleFieldType Type { get; set; } = ConsoleFieldType.Text;

        /// <summary><para type="description">The wizard page the field is on.</para></summary>
        [Parameter]
        public string? Page { get; set; }

        /// <summary><para type="description">Help shown while the field has focus.</para></summary>
        [Parameter]
        public string? Help { get; set; }

        /// <summary><para type="description">The parameter the field supplies in the equivalent command (Name, -Key or --name).</para></summary>
        [Parameter]
        public string? ParameterName { get; set; }

        /// <summary><para type="description">Refuses an empty value.</para></summary>
        [Parameter]
        public SwitchParameter Required { get; set; }

        /// <summary><para type="description">The default (for choices: the choice object or its text; for multi-selections: several).</para></summary>
        [Parameter]
        public object? Default { get; set; }

        /// <summary><para type="description">Muted text while a text field is empty.</para></summary>
        [Parameter]
        public string? Placeholder { get; set; }

        /// <summary><para type="description">The choices of a Select or MultiSelect field (any objects).</para></summary>
        [Parameter]
        public object?[]? Choices { get; set; }

        /// <summary><para type="description">What to show for a choice: a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? Display { get; set; }

        /// <summary><para type="description">The group of a choice: a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? GroupBy { get; set; }

        /// <summary><para type="description">A sentence for the focused choice: a property name or a scriptblock. With -LoadChoices, use a property name.</para></summary>
        [Parameter]
        public object? Describe { get; set; }

        /// <summary><para type="description">Short muted text at the right of a choice ("~2 s", "Web"): a property name or a scriptblock. Rich text keeps its own style.</para></summary>
        [Parameter]
        public object? Badge { get; set; }

        /// <summary><para type="description">Why a choice cannot be picked: a property name or a scriptblock returning the reason. Nothing, $false or an empty string leaves the choice available; $true disables it without a reason. Disabled choices are dimmed and show the reason while they have focus.</para></summary>
        [Parameter]
        public object? DisabledReason { get; set; }

        /// <summary><para type="description">Whether a choice starts selected: a property name or a scriptblock returning $true. With -LoadChoices, use a property name. Works for loaded choices too, so the choices a loader marks are chosen once they arrive (a preset's values take their place when one is applied).</para></summary>
        [Parameter]
        public object? Selected { get; set; }

        /// <summary><para type="description">The value of a choice: the text the equivalent command shows and the field returns, a property name or a scriptblock. The returned text keeps the choice object's properties, so $_.Name still works in -Preview and in the submitted values; presets and remembered choices store the text.</para></summary>
        [Parameter]
        public object? CommandValue { get; set; }

        /// <summary><para type="description">Preview content for the focused choice in the editor ($_ is the choice).</para></summary>
        [Parameter]
        public ScriptBlock? Preview { get; set; }

        /// <summary><para type="description">Loads the choices in their own runspace; the script gets the current values as $Values (and its first argument).</para></summary>
        [Parameter]
        public ScriptBlock? LoadChoices { get; set; }

        /// <summary><para type="description">More arguments for the -LoadChoices script.</para></summary>
        [Parameter]
        public object?[]? ArgumentList { get; set; }

        /// <summary><para type="description">Fields whose change resets this one (and reloads its choices).</para></summary>
        [Parameter]
        public string[]? DependsOn { get; set; }

        /// <summary><para type="description">Shows the field only while this returns $true ($Values holds the other values).</para></summary>
        [Parameter]
        public ScriptBlock? VisibleWhen { get; set; }

        /// <summary><para type="description">A validator: $_ is the value, $Values the other values; return $true (or nothing), $false or an error message.</para></summary>
        [Parameter]
        public ScriptBlock? Validator { get; set; }

        /// <summary><para type="description">The smallest number, or the fewest choices of a MultiSelect field.</para></summary>
        [Parameter]
        public decimal? Minimum { get; set; }

        /// <summary><para type="description">The largest number, or the most choices of a MultiSelect field.</para></summary>
        [Parameter]
        public decimal? Maximum { get; set; }

        /// <summary><para type="description">The step of a number field.</para></summary>
        [Parameter]
        public decimal? Step { get; set; }

        /// <summary><para type="description">Whole numbers only.</para></summary>
        [Parameter]
        public SwitchParameter Integer { get; set; }

        /// <summary><para type="description">What a path field accepts: Any, File or Folder.</para></summary>
        [Parameter]
        public PathKind Kind { get; set; } = PathKind.Any;

        /// <summary><para type="description">Requires a path to exist.</para></summary>
        [Parameter]
        public SwitchParameter MustExist { get; set; }

        /// <summary><para type="description">File extensions offered as path suggestions.</para></summary>
        [Parameter]
        public string[]? Extension { get; set; }

        /// <summary><para type="description">Text after a toggle that is on (default Yes).</para></summary>
        [Parameter]
        public string? OnText { get; set; }

        /// <summary><para type="description">Text after a toggle that is off (default No).</para></summary>
        [Parameter]
        public string? OffText { get; set; }

        /// <summary><para type="description">A pattern a text field must match.</para></summary>
        [Parameter]
        public string? Pattern { get; set; }

        /// <summary><para type="description">The message when -Pattern does not match.</para></summary>
        [Parameter]
        public string? PatternMessage { get; set; }

        /// <summary><para type="description">The fewest characters of a text or secret field.</para></summary>
        [Parameter]
        public int MinLength { get; set; }

        /// <summary><para type="description">The most characters (TextArea).</para></summary>
        [Parameter]
        public int MaxLength { get; set; } = int.MaxValue;

        /// <summary><para type="description">The most lines (TextArea).</para></summary>
        [Parameter]
        public int MaxLines { get; set; } = int.MaxValue;

        /// <summary><para type="description">The visible rows of the text area dialog (TextArea, default 8).</para></summary>
        [Parameter]
        public int Height { get; set; } = 8;

        /// <summary><para type="description">What stands in for a secret in the equivalent command.</para></summary>
        [Parameter]
        public string? CommandPlaceholder { get; set; }

        /// <summary><para type="description">Keeps the value out of presets and remembered choices.</para></summary>
        [Parameter]
        public SwitchParameter NoRemember { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            Field field = Create();
            field.Help = Help;
            field.Parameter = ParameterName;
            field.Required = Required.IsPresent || field.Required && Type == ConsoleFieldType.Secret;
            if (NoRemember.IsPresent) {
                field.Remember = false;
            }
            if (DependsOn != null) {
                foreach (string dependency in DependsOn) {
                    field.DependsOn.Add(dependency);
                }
            }
            if (VisibleWhen != null) {
                ScriptBlock visible = VisibleWhen;
                field.VisibleWhen = values => LanguagePrimitives.IsTrue(PSInteractive.Last(PSInteractive.InvokeWith(visible, null, new KeyValuePair<string, object?>("Values", PSForms.ToHashtable(values, null)))));
            }
            if (Validator != null) {
                ScriptBlock validate = Validator;
                field.Validator = (value, values) => PSInteractive.Validation(validate, value, PromptLabels.Default, new KeyValuePair<string, object?>("Values", PSForms.ToHashtable(values, null)));
            }
            PSForms.SetPage(field, Page);
            WriteObject(field);
        }

        private Field Create() {
            switch (Type) {
                case ConsoleFieldType.TextArea:
                    TextAreaField area = new TextAreaField(Key, Label) { Placeholder = Placeholder, MinLength = MinLength, MaxLength = MaxLength, MaxLines = MaxLines, Height = Height };
                    if (Default != null) {
                        area.Default = PSInteractive.Text(Default);
                    }
                    return area;
                case ConsoleFieldType.Secret:
                    SecretField secret = new SecretField(Key, Label) { MinLength = MinLength, Required = Required.IsPresent };
                    if (CommandPlaceholder != null) {
                        secret.CommandPlaceholder = CommandPlaceholder;
                    }
                    return secret;
                case ConsoleFieldType.Number:
                    NumberField number = new NumberField(Key, Label) { Integer = Integer.IsPresent, Minimum = Minimum, Maximum = Maximum };
                    if (Step.HasValue) {
                        number.Step = Step.Value;
                    }
                    if (Default != null) {
                        number.Default = LanguagePrimitives.ConvertTo<decimal>(PSText.Unwrap(Default));
                    }
                    return number;
                case ConsoleFieldType.Toggle:
                    ToggleField toggle = new ToggleField(Key, Label) { OnText = OnText, OffText = OffText };
                    if (Default != null) {
                        toggle.Default = LanguagePrimitives.IsTrue(PSText.Unwrap(Default));
                    }
                    return toggle;
                case ConsoleFieldType.Path:
                    PathField path = new PathField(Key, Label) { PathKind = Kind, MustExist = MustExist.IsPresent };
                    if (Extension != null) {
                        foreach (string e in Extension) {
                            path.Extensions.Add(e.StartsWith(".", StringComparison.Ordinal) ? e : "." + e);
                        }
                    }
                    if (Default != null) {
                        path.Default = PSInteractive.Text(Default);
                    }
                    return path;
                case ConsoleFieldType.Select:
                case ConsoleFieldType.MultiSelect:
                    bool multiple = Type == ConsoleFieldType.MultiSelect;
                    ChoiceField choice = new ChoiceField(Key, Label, multiple) {
                        Minimum = Minimum.HasValue ? (int)Minimum.Value : (int?)null,
                        Maximum = Maximum.HasValue ? (int)Maximum.Value : (int?)null
                    };
                    ChoiceSelectors selectors = new ChoiceSelectors {
                        Display = Display,
                        Group = GroupBy,
                        Description = Describe,
                        Badge = Badge,
                        MutedBadge = true,
                        Disabled = DisabledReason,
                        Selected = multiple ? Default : null,
                        SelectedBy = Selected,
                        CommandValue = CommandValue
                    };
                    if (Choices != null) {
                        foreach (Choice c in PSInteractive.Choices(new List<object?>(Choices), selectors)) {
                            choice.Choices.Add(c);
                        }
                    }
                    if (LoadChoices != null) {
                        choice.Loader = PSInteractive.Loader(LoadChoices, selectors, ArgumentList);
                    }
                    if (Preview != null) {
                        ScriptBlock preview = Preview;
                        choice.Preview = c => PSInteractive.ToWidget(PSInteractive.InvokeWith(preview, c.Value));
                    }
                    if (Default != null) {
                        if (multiple) {
                            List<object?> values = new List<object?>();
                            foreach (object? d in PSInteractive.Enumerate(Default)) {
                                Choice? match = PSInteractive.Find(choice.Choices, d);
                                values.Add(match != null ? match.Value : PSInteractive.ChoiceDefault(d, CommandValue));
                            }
                            choice.Default = values;
                        } else {
                            Choice? match = PSInteractive.Find(choice.Choices, Default);
                            choice.Default = match != null ? match.Value : PSInteractive.ChoiceDefault(PSText.Unwrap(Default), CommandValue);
                        }
                    }
                    return choice;
                default:
                    TextField text = new TextField(Key, Label) { Placeholder = Placeholder, MinLength = MinLength, Pattern = Pattern, PatternMessage = PatternMessage };
                    if (Default != null) {
                        text.Default = PSInteractive.Text(Default);
                    }
                    return text;
            }
        }
    }
}
