using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Text;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.</para>
    /// <para type="description">What the item does follows from the parameter you give: -Action (a scriptblock, run on the pipeline thread with $MenuContext), -Submenu (a menu or a scriptblock of items), -Form (with -OnSubmit, which gets $Values and $FormResult), -Select or -MultiSelect (the chosen objects stay on the item: $item.Selection), -LoadChoices (loaded in its own runspace with a spinner), -Page (content built each time it shows), -Review (a summary with the equivalent command and Run), -Presets, -Separator or -Exit. -Badge and -Detail can be scriptblocks, recomputed every time the menu draws; -DisabledReason returns why the item is unavailable.</para>
    /// </summary>
    /// <example>
    ///   <para>A multi-selection with a computed badge</para>
    ///   <code>New-ConsoleMenuItem 'Tasks' -Hotkey k -MultiSelect $tasks -Display Name -GroupBy Category -Preview { $_.Description }</code>
    /// </example>
    /// <example>
    ///   <para>An action that asks first and shows a toast</para>
    ///   <code>New-ConsoleMenuItem 'Clear cache' -Action { if ($MenuContext.Confirm('Clear the cache?')) { Clear-AppCache; $MenuContext.Toast('Cache cleared') } }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleMenuItem")]
    [OutputType(typeof(MenuItem))]
    public sealed class CmdletNewConsoleMenuItem : PSCmdlet {
        /// <summary><para type="description">The title.</para></summary>
        [Parameter(Position = 0)]
        public string? Label { get; set; }

        /// <summary><para type="description">The action, run on the pipeline thread; $MenuContext (also $_) offers Toast, Confirm, Ask, Run (a live dashboard) and ShowResult.</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Action { get; set; }

        /// <summary><para type="description">The key that opens the item directly.</para></summary>
        [Parameter]
        public char? Hotkey { get; set; }

        /// <summary><para type="description">A sentence shown with the item.</para></summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary><para type="description">A short mark before the title, as free text. Prefer -IconName, which follows the glyph tier.</para></summary>
        [Parameter]
        public string? Icon { get; set; }

        /// <summary><para type="description">A semantic icon that follows the glyph tier: Item, Targets, Tasks, Settings, Favorite, Run, Back, List, Help, Warning, Done or Exit (for example ● in modern fonts, o in ASCII). Takes precedence over -Icon.</para></summary>
        [Parameter]
        public ConsoleMonster.Glyphs.GlyphIcon? IconName { get; set; }

        /// <summary><para type="description">Right-aligned text: a string, rich text, or a scriptblock recomputed whenever the menu draws.</para></summary>
        [Parameter]
        public object? Badge { get; set; }

        /// <summary><para type="description">State text ("24 selected"): a string or a scriptblock recomputed whenever the menu draws.</para></summary>
        [Parameter]
        public object? Detail { get; set; }

        /// <summary><para type="description">Why the item is unavailable: a string, or a scriptblock returning the reason (nothing when available).</para></summary>
        [Parameter]
        public object? DisabledReason { get; set; }

        /// <summary><para type="description">A submenu: a menu from New-ConsoleMenu, or a scriptblock of New-ConsoleMenuItem calls.</para></summary>
        [Parameter]
        public object? Submenu { get; set; }

        /// <summary><para type="description">A form from New-ConsoleForm.</para></summary>
        [Parameter]
        public Form? Form { get; set; }

        /// <summary><para type="description">Runs when the form is submitted; $Values is a hashtable of the values, $FormResult the full result.</para></summary>
        [Parameter]
        public ScriptBlock? OnSubmit { get; set; }

        /// <summary><para type="description">Choices for picking one value (any objects).</para></summary>
        [Parameter]
        public object?[]? Select { get; set; }

        /// <summary><para type="description">Choices for picking several values (any objects).</para></summary>
        [Parameter]
        public object?[]? MultiSelect { get; set; }

        /// <summary><para type="description">Loads the choices in its own runspace (a spinner shows meanwhile). Pair with -Single for one value.</para></summary>
        [Parameter]
        public ScriptBlock? LoadChoices { get; set; }

        /// <summary><para type="description">Arguments for the -LoadChoices script.</para></summary>
        [Parameter]
        public object?[]? ArgumentList { get; set; }

        /// <summary><para type="description">With -LoadChoices: pick one value instead of several.</para></summary>
        [Parameter]
        public SwitchParameter Single { get; set; }

        /// <summary><para type="description">What to show for a choice: a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? Display { get; set; }

        /// <summary><para type="description">The group of a choice: a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? GroupBy { get; set; }

        /// <summary><para type="description">A sentence for the focused choice: a property name or a scriptblock.</para></summary>
        [Parameter]
        public object? Describe { get; set; }

        /// <summary><para type="description">Badge for each selection choice: a property name or scriptblock. With -LoadChoices, use a property name from the loader's output. The item's own badge remains -Badge.</para></summary>
        [Parameter]
        public object? ChoiceBadge { get; set; }

        /// <summary><para type="description">Disabled reason for each selection choice: a property name or scriptblock; null or empty enables the choice. With -LoadChoices, use a property name. The item's own disabled reason remains -DisabledReason.</para></summary>
        [Parameter]
        public object? ChoiceDisabledReason { get; set; }

        /// <summary><para type="description">Preview content for the focused choice ($_ is the choice).</para></summary>
        [Parameter]
        public ScriptBlock? Preview { get; set; }

        /// <summary><para type="description">Pre-selected choices: the objects or their texts, or a scriptblock returning $true for static choices. With -LoadChoices, pass initial values; they are matched when the choices arrive.</para></summary>
        [Parameter]
        public object? Selected { get; set; }

        /// <summary><para type="description">The fewest values of a multi-selection.</para></summary>
        [Parameter]
        public int? Minimum { get; set; }

        /// <summary><para type="description">The most values of a multi-selection.</para></summary>
        [Parameter]
        public int? Maximum { get; set; }

        /// <summary><para type="description">Runs when the selection changes ($MenuContext and $_).</para></summary>
        [Parameter]
        public ScriptBlock? OnChange { get; set; }

        /// <summary><para type="description">Page content, built each time it shows: text, rich text, widgets, or objects shown as a list.</para></summary>
        [Parameter]
        public ScriptBlock? Page { get; set; }

        /// <summary><para type="description">
        /// A review built each time it shows: a hashtable with Rows (an ordered hashtable of label = value), Command (the
        /// equivalent command line), Problems (strings that block running), Run (a scriptblock) and optionally Title,
        /// Description and RunText.
        /// </para></summary>
        [Parameter]
        public ScriptBlock? Review { get; set; }

        /// <summary><para type="description">Makes the item list the menu's presets: the ones it comes with (New-ConsoleMenu -Preset or New-ConsoleMenuPreset in its scriptblock) first, marked as built in, with their previews, then the saved ones. Enter applies one; Delete removes a saved one (Shell and Classic layouts; the Inline layout applies them only).</para></summary>
        [Parameter]
        public SwitchParameter Presets { get; set; }

        /// <summary><para type="description">Makes a divider (with -Label as its heading, if given).</para></summary>
        [Parameter]
        public SwitchParameter Separator { get; set; }

        /// <summary><para type="description">Makes the exit item (-Label defaults to Exit).</para></summary>
        [Parameter]
        public SwitchParameter Exit { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            MenuItem item = new MenuItem(Label ?? (Exit.IsPresent ? PromptLabels.Default.Exit : string.Empty)) {
                Description = Description,
                Icon = Icon,
                IconName = IconName,
                Hotkey = Hotkey
            };
            ApplyDynamic(item);
            if (Separator.IsPresent) {
                item.Kind = MenuItemKind.Separator;
            } else if (Exit.IsPresent) {
                item.Kind = MenuItemKind.Exit;
            } else if (Submenu != null) {
                Menu submenu = PSText.Unwrap(Submenu) is Menu given ? given : new Menu(item.Label);
                if (PSText.Unwrap(Submenu) is ScriptBlock items) {
                    foreach (MenuItem child in PSMenus.CollectItems(items)) {
                        submenu.Add(child);
                    }
                }
                item.Submenu(submenu);
            } else if (Form != null) {
                ScriptBlock? submit = OnSubmit;
                item.Form(Form, submit == null ? null : (Action<MenuContext, FormResult>)((context, result) => PSMenus.Invoke(submit, context,
                    new KeyValuePair<string, object?>("Values", PSForms.ToHashtable(result.Values, Form)),
                    new KeyValuePair<string, object?>("FormResult", result))));
            } else if (Select != null || MultiSelect != null) {
                bool multiple = MultiSelect != null;
                List<object?> values = new List<object?>(multiple ? MultiSelect! : Select!);
                List<Choice> choices = PSInteractive.Choices(values, new ChoiceSelectors {
                    Display = Display, Group = GroupBy, Description = Describe, Badge = ChoiceBadge,
                    Disabled = ChoiceDisabledReason, Selected = Selected, MutedBadge = true
                });
                if (multiple) {
                    item.MultiSelect(choices);
                } else {
                    item.Select(choices);
                    Choice? fallback = PSInteractive.Find(choices, Selected);
                    if (fallback != null) {
                        item.SetSelection(new[] { fallback.Value });
                    }
                }
                ApplyChoiceSettings(item);
            } else if (LoadChoices != null) {
                if (PSText.Unwrap(ChoiceBadge) is ScriptBlock || PSText.Unwrap(ChoiceDisabledReason) is ScriptBlock || PSText.Unwrap(Selected) is ScriptBlock) {
                    ThrowTerminatingError(new ErrorRecord(new PSArgumentException("With -LoadChoices, -ChoiceBadge and -ChoiceDisabled take property names, and -Selected takes initial values rather than a scriptblock."),
                        "ConsoleMonsterChoiceLoaderSelector", ErrorCategory.InvalidArgument, Label));
                    return;
                }
                item.LoadChoices = PSInteractive.MenuLoader(LoadChoices, new ChoiceSelectors {
                    Display = Display, Group = GroupBy, Description = Describe, Badge = ChoiceBadge, Disabled = ChoiceDisabledReason,
                    Selected = Selected, MutedBadge = true
                }, ArgumentList);
                item.Kind = Single.IsPresent ? MenuItemKind.Select : MenuItemKind.MultiSelect;
                ApplyChoiceSettings(item);
            } else if (Page != null) {
                ScriptBlock page = Page;
                item.Page = context => PSInteractive.ToWidget(PSInteractive.InvokeWith(page, context, new KeyValuePair<string, object?>("MenuContext", context))) ?? new Widgets.TextBlock(string.Empty);
            } else if (Review != null) {
                ScriptBlock review = Review;
                item.Review = context => PSForms.ToReview(PSInteractive.Last(PSInteractive.InvokeWith(review, context, new KeyValuePair<string, object?>("MenuContext", context))));
            } else if (Presets.IsPresent) {
                item.Presets();
            } else if (Action != null) {
                ScriptBlock action = Action;
                item.Action = context => PSMenus.Invoke(action, context);
            }
            WriteObject(item);
        }

        private void ApplyChoiceSettings(MenuItem item) {
            item.Minimum = Minimum;
            item.Maximum = Maximum;
            if (Preview != null) {
                ScriptBlock preview = Preview;
                item.Preview = choice => PSInteractive.ToWidget(PSInteractive.InvokeWith(preview, choice.Value));
            }
            if (OnChange != null) {
                ScriptBlock change = OnChange;
                item.OnChange = context => PSMenus.Invoke(change, context);
            }
        }

        private void ApplyDynamic(MenuItem item) {
            object? badge = PSText.Unwrap(Badge);
            if (badge is ScriptBlock badgeScript) {
                item.Badge = () => {
                    object? result = PSText.Unwrap(PSInteractive.Last(badgeScript.Invoke()));
                    return result is RichText rich ? rich : result == null || PSInteractive.Text(result).Length == 0 ? null : RichText.Plain(PSInteractive.Text(result));
                };
            } else if (badge is RichText richBadge) {
                item.Badge = () => richBadge;
            } else if (badge != null) {
                string text = PSInteractive.Text(badge);
                item.Badge = () => RichText.Plain(text);
            }
            object? state = PSText.Unwrap(Detail);
            if (state is ScriptBlock stateScript) {
                item.Detail = () => {
                    string text = PSInteractive.Text(PSInteractive.Last(stateScript.Invoke()));
                    return text.Length == 0 ? null : text;
                };
            } else if (state != null) {
                string text = PSInteractive.Text(state);
                item.Detail = () => text;
            }
            object? disabled = PSText.Unwrap(DisabledReason);
            if (disabled is ScriptBlock disabledScript) {
                item.DisabledReason = () => {
                    object? result = PSText.Unwrap(PSInteractive.Last(disabledScript.Invoke()));
                    if (result == null || result is false) {
                        return null;
                    }
                    return result is true ? "unavailable" : PSInteractive.Text(result);
                };
            } else if (disabled != null) {
                string reason = PSInteractive.Text(disabled);
                item.DisabledReason = () => reason;
            }
        }
    }
}
