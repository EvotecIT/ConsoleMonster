using System;
using ConsoleMonster.Live;

namespace ConsoleMonster.Interactive {
    /// <summary>Input, live rendering and fallback settings for one application run.</summary>
    public sealed class ConsoleAppOptions {
        /// <summary>Shared prompt input, capture, capability, theme and language settings.</summary>
        public InteractiveOptions? Interactive { get; set; }

        /// <summary>Optional live-session configuration, set before the run begins.</summary>
        public LiveSessionOptions? Live { get; set; }

        /// <summary>Prints or captures one initialized snapshot instead of reading input, including on CI and redirected output.</summary>
        public bool StaticSnapshot { get; set; }

        /// <summary>Enables optional terminal mouse reporting for an interactive run. Keyboard operation is always available.</summary>
        public bool Mouse { get; set; } = true;

        /// <summary>The default width below which column layouts stack; explicitly configured column thresholds take precedence.</summary>
        public int ReflowBelow { get; set; } = 60;

        /// <summary>Called on the owning input thread before the next read, for hosts that dispatch callbacks on that thread.</summary>
        public Action<AppState>? ProcessEvents { get; set; }
    }
}
