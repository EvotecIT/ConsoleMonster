namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// How the interactive cmdlets build choices from objects: each selector is a property name or a scriptblock ($_ is the
    /// object), except where noted.
    /// </summary>
    internal sealed class ChoiceSelectors {
        /// <summary>The title.</summary>
        public object? Display { get; set; }

        /// <summary>The group heading.</summary>
        public object? Group { get; set; }

        /// <summary>The sentence shown while the choice has focus.</summary>
        public object? Description { get; set; }

        /// <summary>The short right-aligned text.</summary>
        public object? Badge { get; set; }

        /// <summary>Whether text badges are muted (rich text badges keep their own style).</summary>
        public bool MutedBadge { get; set; }

        /// <summary>The key that picks the choice.</summary>
        public object? Hotkey { get; set; }

        /// <summary>Why the choice cannot be picked: nothing, $false or an empty string when it can.</summary>
        public object? Disabled { get; set; }

        /// <summary>The choices selected at first: their texts or objects, or a scriptblock returning $true for selected ones.</summary>
        public object? Selected { get; set; }

        /// <summary>Whether a choice starts selected, read from the object (a property name or a scriptblock); takes precedence over <see cref="Selected"/>.</summary>
        public object? SelectedBy { get; set; }

        /// <summary>The text the equivalent command shows and the field returns for a choice, or null for the object itself.</summary>
        public object? CommandValue { get; set; }

        /// <summary>The same selectors without scriptblocks, for loaders that run on a worker thread.</summary>
        /// <returns>The selectors.</returns>
        public ChoiceSelectors PropertiesOnly() {
            return new ChoiceSelectors {
                Display = PSInteractive.PropertyOnly(Display),
                Group = PSInteractive.PropertyOnly(Group),
                Description = PSInteractive.PropertyOnly(Description),
                Badge = PSInteractive.PropertyOnly(Badge),
                MutedBadge = MutedBadge,
                Hotkey = PSInteractive.PropertyOnly(Hotkey),
                Disabled = PSInteractive.PropertyOnly(Disabled),
                SelectedBy = PSInteractive.PropertyOnly(SelectedBy),
                CommandValue = PSInteractive.PropertyOnly(CommandValue)
            };
        }
    }
}
