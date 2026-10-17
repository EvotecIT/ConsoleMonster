using System;
using System.Globalization;
using System.Threading;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.Changes {
    /// <summary>Options for <see cref="ChangePreview.Confirm"/>.</summary>
    public sealed class ChangePreviewOptions {
        /// <summary>A title above the preview (markup), or null.</summary>
        public string? Title { get; set; }

        /// <summary>The view of the changes (default <see cref="ChangeViewMode.Full"/>).</summary>
        public ChangeViewMode Mode { get; set; } = ChangeViewMode.Full;

        /// <summary>The question, or null for "Apply these changes?" in the current language.</summary>
        public string? Question { get; set; }

        /// <summary>Text the user must type to confirm (for example a server name), or null.</summary>
        public string? TypedConfirmation { get; set; }

        /// <summary>
        /// Whether changes that remove objects need a typed confirmation when <see cref="TypedConfirmation"/> is not set:
        /// the user types the number of removals (default true).
        /// </summary>
        public bool TypedWhenDestructive { get; set; } = true;

        /// <summary>Prompt options (input, target, labels), or null for the defaults.</summary>
        public InteractiveOptions? Interactive { get; set; }
    }

    /// <summary>
    /// The what-if pattern: show the changes an action would make, then ask before making them. A typed confirmation guards
    /// destructive changes. Returns the decision; the caller acts only on <c>true</c>.
    /// </summary>
    public static class ChangePreview {
        /// <summary>Shows the preview and asks for confirmation.</summary>
        /// <param name="changes">The changes.</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>True when the user approved; false when declined or when there is nothing to change.</returns>
        public static bool Confirm(ChangeSet changes, ChangePreviewOptions? options = null, CancellationToken cancellationToken = default) {
            if (changes == null) {
                throw new ArgumentNullException(nameof(changes));
            }
            options ??= new ChangePreviewOptions();
            Show(changes, options);
            return Ask(changes, options, cancellationToken);
        }

        /// <summary>Asks for confirmation of changes already shown (a typed confirmation for destructive ones).</summary>
        /// <param name="changes">The changes.</param>
        /// <param name="options">Options, or null.</param>
        /// <param name="cancellationToken">Cancels the prompt.</param>
        /// <returns>True when the user approved; false when declined or when there is nothing to change.</returns>
        public static bool Ask(ChangeSet changes, ChangePreviewOptions? options = null, CancellationToken cancellationToken = default) {
            if (changes == null) {
                throw new ArgumentNullException(nameof(changes));
            }
            options ??= new ChangePreviewOptions();
            if (!changes.HasChanges) {
                return false;
            }
            string question = options.Question ?? Localization.WidgetLabels.Current.ChangesConfirm;
            string? typed = options.TypedConfirmation ?? (changes.IsDestructive && options.TypedWhenDestructive ? changes.Removed.ToString(CultureInfo.InvariantCulture) : null);
            if (typed != null) {
                return new TypedConfirmPrompt(question, typed).Show(options.Interactive, cancellationToken);
            }
            return new ConfirmPrompt(question, false) { Destructive = changes.IsDestructive }.Show(options.Interactive, cancellationToken);
        }

        /// <summary>Shows the preview only (the what-if half), with a note when there is nothing to change.</summary>
        /// <param name="changes">The changes.</param>
        /// <param name="options">Options, or null.</param>
        public static void Show(ChangeSet changes, ChangePreviewOptions? options = null) {
            if (changes == null) {
                throw new ArgumentNullException(nameof(changes));
            }
            options ??= new ChangePreviewOptions();
            Terminal.Write(new ChangeView(changes, options.Mode) { Title = options.Title });
            if (!changes.HasChanges) {
                Terminal.WriteMarkupLine("[muted]" + Text.Markup.Escape(Localization.WidgetLabels.Current.ChangesNone) + "[/]");
            }
        }
    }
}
