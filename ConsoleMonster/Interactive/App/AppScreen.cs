using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// One screen of an <see cref="AppScreens"/> stack: its content (which may hold inputs), the title its breadcrumb
    /// shows and the key bindings that apply while it is the current screen.
    /// </summary>
    public sealed class AppScreen {
        /// <summary>Creates a screen.</summary>
        /// <param name="key">The key that opens it, unique within the app.</param>
        /// <param name="title">The breadcrumb title.</param>
        /// <param name="content">The content, built once; inputs inside it keep their values while other screens show.</param>
        public AppScreen(string key, string title, IWidget content) {
            if (string.IsNullOrWhiteSpace(key)) { throw new ArgumentException("A screen needs a key.", nameof(key)); }
            Key = key;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>The key that opens it.</summary>
        public string Key { get; }

        /// <summary>The breadcrumb title.</summary>
        public string Title { get; set; }

        /// <summary>Builds the title from state instead, for a screen that shows one item ("web-deploy"). Runs on the app thread.</summary>
        public Func<AppState, string>? TitleSource { get; set; }

        /// <summary>The content.</summary>
        public IWidget Content { get; }

        /// <summary>Keys that apply while this is the current screen. They reach the screen after the focused input and feed the footer.</summary>
        public IList<KeyBinding> Bindings { get; } = new List<KeyBinding>();
    }
}
