using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>What a run's work gets: the live session, cancellation and the form values.</summary>
    public sealed class RunContext {
        internal RunContext(LiveSession session, MenuContext menu) {
            Session = session;
            Menu = menu;
        }

        /// <summary>The live session (post updates through it or its widgets' handles).</summary>
        public LiveSession Session { get; }

        /// <summary>The menu context.</summary>
        public MenuContext Menu { get; }

        /// <summary>Cancelled by Ctrl+C or when the menu ends.</summary>
        public CancellationToken CancellationToken => Session.Cancellation;

        /// <summary>The values of the submitted form, if any.</summary>
        public FormValues? Values => Menu.Values;
    }
}
