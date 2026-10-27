using System;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>A composed modal form definition whose result contains its named field values.</summary>
    public sealed class FormDialogOptions : IContentOwner {
        /// <summary>The visible dialog heading.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>The retained field subtree.</summary>
        public IWidget Content { get; set; } = new Rows();
        void IContentOwner.ReplaceContent(IWidget[] content) => Content = ContentBuilder.Compose(content);
        /// <summary>The affirmative button label.</summary>
        public string AcceptText { get; set; } = "OK";
        /// <summary>The cancellation button label.</summary>
        public string CancelText { get; set; } = "Cancel";
        /// <summary>Whether the affirmative button is initially focused. The default is Cancel.</summary>
        public bool DefaultAccept { get; set; }
        /// <summary>A field key to focus instead of the default button.</summary>
        public string? InitialFocus { get; set; }
        /// <summary>Maximum dialog width in cells.</summary>
        public int MaxWidth { get; set; } = 64;
        /// <summary>An app-thread validator; return an error to keep the form open.</summary>
        public Func<AppState, string?>? Validate { get; set; }
        /// <summary>Builds a typed form without starting a nested input loop.</summary>
        public FormDialog<AppState> Build() => new FormDialog<AppState>(Title, Content, values => values) {
            AcceptText = AcceptText, CancelText = CancelText, DefaultAccept = DefaultAccept,
            InitialFocus = InitialFocus, MaxWidth = MaxWidth, Validate = Validate
        };
    }
}
