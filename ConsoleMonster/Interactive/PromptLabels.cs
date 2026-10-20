using System;
using System.Globalization;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Every built-in word the interactive layer shows: key hints in footers, button captions, validation messages,
    /// accessible-mode questions. They follow the prompt's language (<see cref="Capabilities.TerminalProfile.Language"/>,
    /// <c>CONSOLEMONSTER_LANGUAGE</c> or the current UI culture): English, Polish, German, Spanish, French and Italian are
    /// built in. Assign <see cref="Default"/> to use your own everywhere, or pass labels in
    /// <see cref="InteractiveOptions.Labels"/>; start from <see cref="For"/> and <see cref="Clone"/> to change a few words.
    /// Format strings use <see cref="string.Format(IFormatProvider, string, object[])"/> placeholders.
    /// </summary>
    public sealed class PromptLabels {
        private static readonly System.Reflection.PropertyInfo[] LabelProperties = typeof(PromptLabels).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        private static PromptLabels? _default;

        /// <summary>
        /// The labels used when none are passed: the assigned ones, otherwise the translation for
        /// <see cref="Localization.LabelLanguage.Default"/>.
        /// </summary>
        public static PromptLabels Default {
            get => _default ?? For(Localization.LabelLanguage.Default);
            set => _default = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>The labels for a language: the assigned <see cref="Default"/>, otherwise the built-in translation (English when there is none).</summary>
        /// <param name="language">The language, or null for <see cref="Localization.LabelLanguage.Default"/>.</param>
        /// <returns>The labels; don't modify the shared instance, use <see cref="Clone"/>.</returns>
        public static PromptLabels For(CultureInfo? language) => _default ?? Localization.LabelCatalog.Get(language ?? Localization.LabelLanguage.Default, "Prompt.", () => new PromptLabels(), LabelProperties);

        /// <summary>A copy to modify.</summary>
        /// <returns>The copy.</returns>
        public PromptLabels Clone() => (PromptLabels)MemberwiseClone();

        /// <summary>Key hint: move the focus.</summary>
        public string KeyMove { get; set; } = "move";

        /// <summary>Key hint: choose the focused item.</summary>
        public string KeySelect { get; set; } = "select";

        /// <summary>Key hint: toggle the focused item.</summary>
        public string KeyToggle { get; set; } = "toggle";

        /// <summary>Key hint: accept the answer.</summary>
        public string KeyConfirm { get; set; } = "confirm";

        /// <summary>Key hint: cancel the prompt.</summary>
        public string KeyCancel { get; set; } = "cancel";

        /// <summary>Key hint: go back.</summary>
        public string KeyBack { get; set; } = "back";

        /// <summary>Key hint: start filtering.</summary>
        public string KeyFilter { get; set; } = "filter";

        /// <summary>Key hint: clear the filter.</summary>
        public string KeyClearFilter { get; set; } = "clear filter";

        /// <summary>Key hint: select all.</summary>
        public string KeyAll { get; set; } = "all";

        /// <summary>Key hint: select none.</summary>
        public string KeyNone { get; set; } = "none";

        /// <summary>Key hint: invert the selection.</summary>
        public string KeyInvert { get; set; } = "invert";

        /// <summary>Key hint: show only selected items.</summary>
        public string KeySelectedOnly { get; set; } = "selected only";

        /// <summary>Key hint: page through the list.</summary>
        public string KeyPage { get; set; } = "page";

        /// <summary>Key hint: expand or collapse a group.</summary>
        public string KeyExpand { get; set; } = "expand/collapse";

        /// <summary>Key hint: next field or page.</summary>
        public string KeyNext { get; set; } = "next";

        /// <summary>Key hint: previous field or page.</summary>
        public string KeyPrevious { get; set; } = "previous";

        /// <summary>Key hint: open the command palette.</summary>
        public string KeyPalette { get; set; } = "commands";

        /// <summary>Key hint: show every key.</summary>
        public string KeyHelp { get; set; } = "keys";

        /// <summary>Key hint: go to the first page of the app.</summary>
        public string KeyHome { get; set; } = "home";

        /// <summary>Key hint: leave the app.</summary>
        public string KeyQuit { get; set; } = "quit";

        /// <summary>Key hint: show a masked value.</summary>
        public string KeyReveal { get; set; } = "reveal";

        /// <summary>Key hint: mask the value again.</summary>
        public string KeyHide { get; set; } = "hide";

        /// <summary>Key hint: complete a path.</summary>
        public string KeyComplete { get; set; } = "complete";

        /// <summary>Key hint: step a number up or down.</summary>
        public string KeyStep { get; set; } = "adjust";

        /// <summary>Key hint: open the focused item.</summary>
        public string KeyOpen { get; set; } = "open";

        /// <summary>Key hint: change the focused value.</summary>
        public string KeyEdit { get; set; } = "edit";

        /// <summary>Key hint: move between panes.</summary>
        public string KeyPane { get; set; } = "switch pane";

        /// <summary>Key hint: earlier answers.</summary>
        public string KeyHistory { get; set; } = "history";

        /// <summary>Key hint: answer yes or no.</summary>
        public string KeyYesNo { get; set; } = "yes/no";

        /// <summary>Key hint: run the action.</summary>
        public string KeyRun { get; set; } = "run";

        /// <summary>Key hint: sort a grid by the next column.</summary>
        public string KeySort { get; set; } = "sort";

        /// <summary>Key hint: reverse a grid's sort order.</summary>
        public string KeyOrder { get; set; } = "order";

        /// <summary>Key hint: choose a grid's columns.</summary>
        public string KeyColumns { get; set; } = "columns";

        /// <summary>Key hint: the focused row's details.</summary>
        public string KeyDetails { get; set; } = "details";

        /// <summary>Key hint: finish and return the selection.</summary>
        public string KeyDone { get; set; } = "done";

        /// <summary>The grid's row count: {0} shown of {1}.</summary>
        public string GridRows { get; set; } = "{0} of {1} rows";

        /// <summary>The grid's selection count.</summary>
        public string GridSelected { get; set; } = "{0} selected";

        /// <summary>The column chooser's title.</summary>
        public string Columns { get; set; } = "Columns";

        /// <summary>Key hint: a new line in a multi-line text box.</summary>
        public string KeyNewLine { get; set; } = "new line";

        /// <summary>Key hint: save a multi-line text.</summary>
        public string KeySave { get; set; } = "save";

        /// <summary>The line-mode hint of a multi-line answer.</summary>
        public string EndWithEmptyLine { get; set; } = "end with an empty line";

        /// <summary>Too long: {0} characters at most.</summary>
        public string MaximumLength { get; set; } = "Enter at most {0} characters.";

        /// <summary>Too many lines: {0} at most.</summary>
        public string MaximumLines { get; set; } = "Enter at most {0} lines.";

        /// <summary>The counter under a multi-line text box: {0} lines, {1} characters.</summary>
        public string TextAreaCounter { get; set; } = "{0} lines · {1} characters";

        /// <summary>Footer label for ctrl+c during a run.</summary>
        public string KeyStop { get; set; } = "stop";

        /// <summary>Footer label for Enter on a blocked Run button (it opens the item that fixes the first problem).</summary>
        public string KeyFix { get; set; } = "fix";

        /// <summary>The answer "yes".</summary>
        public string Yes { get; set; } = "Yes";

        /// <summary>The answer "no".</summary>
        public string No { get; set; } = "No";

        /// <summary>Placeholder of a filter box.</summary>
        public string TypeToFilter { get; set; } = "type to filter";

        /// <summary>A filter matched nothing.</summary>
        public string NoMatches { get; set; } = "No matches";

        /// <summary>The live selection count: {0} selected of {1}.</summary>
        public string SelectedCount { get; set; } = "{0} of {1} selected";

        /// <summary>How many items a filter shows: {0}.</summary>
        public string Shown { get; set; } = "{0} shown";

        /// <summary>More items above or below the visible part of a list: {0}.</summary>
        public string More { get; set; } = "{0} more";

        /// <summary>Options are loading.</summary>
        public string Loading { get; set; } = "Loading…";

        /// <summary>A page cannot move on while a field's options are still loading.</summary>
        public string StillLoading { get; set; } = "Still loading; wait a moment or press Esc to cancel.";

        /// <summary>Loading failed: {0}.</summary>
        public string LoadFailed { get; set; } = "Couldn't load the options: {0}";

        /// <summary>A prompt was cancelled.</summary>
        public string Cancelled { get; set; } = "cancelled";

        /// <summary>A value is required.</summary>
        public string Required { get; set; } = "A value is required.";

        /// <summary>The text is not a number.</summary>
        public string NotANumber { get; set; } = "Enter a number.";

        /// <summary>The text is not a whole number.</summary>
        public string NotAWholeNumber { get; set; } = "Enter a whole number.";

        /// <summary>The number is too small: {0} is the minimum.</summary>
        public string Minimum { get; set; } = "Enter {0} or more.";

        /// <summary>The number is too large: {0} is the maximum.</summary>
        public string Maximum { get; set; } = "Enter {0} or less.";

        /// <summary>Too short: {0} characters at least.</summary>
        public string MinimumLength { get; set; } = "Enter at least {0} characters.";

        /// <summary>The answer does not match the expected pattern.</summary>
        public string InvalidFormat { get; set; } = "That doesn't have the expected format.";

        /// <summary>Too few items selected: {0}.</summary>
        public string MinimumSelected { get; set; } = "Select at least {0}.";

        /// <summary>Too many items selected: {0}.</summary>
        public string MaximumSelected { get; set; } = "Select at most {0}.";

        /// <summary>A path does not exist: {0}.</summary>
        public string PathNotFound { get; set; } = "Not found: {0}";

        /// <summary>A folder was chosen where a file is expected.</summary>
        public string FileExpected { get; set; } = "Choose a file, not a folder.";

        /// <summary>A file was chosen where a folder is expected.</summary>
        public string FolderExpected { get; set; } = "Choose a folder, not a file.";

        /// <summary>The focused item is disabled: {0} is the reason.</summary>
        public string Disabled { get; set; } = "Not available: {0}";

        /// <summary>Non-interactive and no default: {0} the prompt, {1} the parameter.</summary>
        public string NonInteractiveNeedsValue { get; set; } = "Prompt '{0}' needs a value in a non-interactive session; pass {1} or set a default.";

        /// <summary>Non-interactive and no default, without a parameter: {0} the prompt.</summary>
        public string NonInteractiveNeedsDefault { get; set; } = "Prompt '{0}' needs a value in a non-interactive session; set a default.";

        /// <summary>The review page title.</summary>
        public string Review { get; set; } = "Review";

        /// <summary>The equivalent command heading.</summary>
        public string EquivalentCommand { get; set; } = "Equivalent command";

        /// <summary>The run button.</summary>
        public string Run { get; set; } = "Run";

        /// <summary>The copy-command button.</summary>
        public string CopyCommand { get; set; } = "Copy command";

        /// <summary>The save-preset button.</summary>
        public string SavePreset { get; set; } = "Save preset";

        /// <summary>The back button.</summary>
        public string Back { get; set; } = "Back";

        /// <summary>The next button.</summary>
        public string Next { get; set; } = "Next";

        /// <summary>The finish button of a form without a review page.</summary>
        public string Done { get; set; } = "Done";

        /// <summary>The question for a preset name.</summary>
        public string PresetName { get; set; } = "Preset name";

        /// <summary>A preset was saved: {0}.</summary>
        public string PresetSaved { get; set; } = "Preset '{0}' saved";

        /// <summary>A preset was applied: {0}.</summary>
        public string PresetApplied { get; set; } = "Preset '{0}' applied";

        /// <summary>The presets heading.</summary>
        public string Presets { get; set; } = "Presets";

        /// <summary>No presets exist yet.</summary>
        public string NoPresets { get; set; } = "No presets yet. Save one from the review page.";

        /// <summary>The mark of a preset the app comes with.</summary>
        public string BuiltIn { get; set; } = "built in";

        /// <summary>A built-in preset cannot be saved over or deleted: {0} its name.</summary>
        public string PresetReadOnly { get; set; } = "'{0}' is built in and can't be changed or deleted";

        /// <summary>The command was copied.</summary>
        public string CommandCopied { get; set; } = "Command copied to the clipboard";

        /// <summary>The clipboard could not be reached.</summary>
        public string CopyFailed { get; set; } = "Couldn't reach the clipboard";

        /// <summary>Typed confirmation: {0} is the text to type.</summary>
        public string TypeToConfirm { get; set; } = "Type {0} to confirm";

        /// <summary>The typed confirmation does not match: {0} is the expected text.</summary>
        public string TypedMismatch { get; set; } = "That doesn't match {0}.";

        /// <summary>The key map heading.</summary>
        public string Keys { get; set; } = "Keys";

        /// <summary>The command palette heading.</summary>
        public string Commands { get; set; } = "Commands";

        /// <summary>The palette's placeholder.</summary>
        public string SearchCommands { get; set; } = "Search items, actions and presets";

        /// <summary>The palette group of menu items.</summary>
        public string GoTo { get; set; } = "Go to";

        /// <summary>The palette group of app commands (home, keys, exit).</summary>
        public string AppCommands { get; set; } = "App";

        /// <summary>How many problems block a review's run: {0}.</summary>
        public string ToFix { get; set; } = "{0} to fix";

        /// <summary>The problem count on a blocked Run button when there is one problem.</summary>
        public string OneProblem { get; set; } = "1 problem";

        /// <summary>The hint on dialogs that close on any key (the key map, action output).</summary>
        public string AnyKeyCloses { get; set; } = "any key closes";

        /// <summary>Footer label for closing a dialog.</summary>
        public string KeyClose { get; set; } = "close";

        /// <summary>The title of the dialog that shows what an action wrote.</summary>
        public string Output { get; set; } = "Output";

        /// <summary>The line above action output when older lines are not shown: {0} is how many.</summary>
        public string EarlierLines { get; set; } = "{0} earlier lines";

        /// <summary>The question when leaving a form opened from a menu with changes that were not saved.</summary>
        public string DiscardFormChanges { get; set; } = "Discard the changes to {0}?";

        /// <summary>The button that discards unsaved form changes.</summary>
        public string Discard { get; set; } = "Discard";

        /// <summary>The problem count on a blocked Run button: {0} is the number of problems.</summary>
        public string ManyProblems { get; set; } = "{0} problems";

        /// <summary>The home breadcrumb.</summary>
        public string Home { get; set; } = "Home";

        /// <summary>The exit item.</summary>
        public string Exit { get; set; } = "Exit";

        /// <summary>The question before leaving with unsaved changes.</summary>
        public string DiscardChanges { get; set; } = "Discard unsaved changes and exit?";

        /// <summary>The preview pane title.</summary>
        public string Preview { get; set; } = "Preview";

        /// <summary>The results heading after a run.</summary>
        public string Results { get; set; } = "Results";

        /// <summary>The next-actions heading after a run.</summary>
        public string NextActions { get; set; } = "Next steps";

        /// <summary>The press-enter hint of an action item.</summary>
        public string PressEnterToRun { get; set; } = "Press Enter to run.";

        /// <summary>Accessible mode: the question for a numbered choice.</summary>
        public string EnterNumber { get; set; } = "Enter a number";

        /// <summary>Accessible mode: the question for several numbered choices.</summary>
        public string EnterNumbers { get; set; } = "Enter numbers separated by commas, 'all' or 'none'";

        /// <summary>Accessible mode: the answer was not one of the numbers: {0}.</summary>
        public string NotAChoice { get; set; } = "'{0}' is not one of the numbers.";

        /// <summary>A yes/no answer was neither: {0} the yes key, {1} the no key.</summary>
        public string AnswerYesNo { get; set; } = "Answer {0} or {1}.";

        /// <summary>Accessible mode: default value hint: {0}.</summary>
        public string DefaultHint { get; set; } = "default: {0}";

        /// <summary>Accessible mode: currently selected: {0}.</summary>
        public string SelectedHint { get; set; } = "selected: {0}";

        /// <summary>Group headings in accessible mode: {0}.</summary>
        public string GroupHeading { get; set; } = "{0}:";

        /// <summary>A menu item whose action is unavailable: {0}.</summary>
        public string Unavailable { get; set; } = "unavailable: {0}";

        /// <summary>Replay player key hint: play or pause.</summary>
        public string KeyPlay { get; set; } = "play/pause";

        /// <summary>Replay player key hint: step one frame.</summary>
        public string KeyFrame { get; set; } = "frame";

        /// <summary>Replay player key hint: change the speed.</summary>
        public string KeySpeed { get; set; } = "speed";

        /// <summary>Replay player key hint: jump to the next or previous marker.</summary>
        public string KeyMarker { get; set; } = "marker";

        /// <summary>Replay player key hint: jump to the next or previous failure.</summary>
        public string KeyFailure { get; set; } = "failure";

        /// <summary>Replay player key hint: jump to the next or previous warning.</summary>
        public string KeyWarning { get; set; } = "warning";

        /// <summary>Replay player key hint: jump to the next or previous log entry.</summary>
        public string KeyLogEntry { get; set; } = "log entry";

        /// <summary>Replay player key hint: search the log.</summary>
        public string KeySearch { get; set; } = "search";

        /// <summary>Replay player state: playing.</summary>
        public string ReplayPlaying { get; set; } = "playing";

        /// <summary>Replay player state: paused.</summary>
        public string ReplayPaused { get; set; } = "paused";

        /// <summary>Replay player: nothing further in the requested direction.</summary>
        public string ReplayNoMore { get; set; } = "Nothing more in that direction";

        /// <summary>Replay player: no log entry contains the search text {0}.</summary>
        public string ReplayNoMatch { get; set; } = "No log entry contains {0}";

        /// <summary>Replay player: placeholder of the log search box.</summary>
        public string ReplaySearch { get; set; } = "search the log";

        /// <summary>Formats a label with arguments using the invariant culture.</summary>
        /// <param name="format">The format string (one of this object's properties).</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The formatted text.</returns>
        public static string Format(string format, params object?[] args) => string.Format(CultureInfo.CurrentCulture, format, args);
    }
}
