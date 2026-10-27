using System.Collections;

namespace ConsoleMonster.PowerShell.Pester {
    /// <summary>Presence-aware native Pester input and ConsoleMonster display options.</summary>
    public sealed class ConsolePesterOptions {
        private readonly Hashtable _simple = new Hashtable(System.StringComparer.OrdinalIgnoreCase);
        internal Hashtable Simple() => (Hashtable)_simple.Clone();
        private string[]? _path;
        /// <summary>The native Path argument; omission retains Pester's default and explicit null remains explicit.</summary>
        public string[]? Path { get => _path; set { _path = value; _simple[nameof(Path)] = value; } }
        private string[]? _excludePath;
        /// <summary>The native ExcludePath argument; omission retains Pester's default and explicit null remains explicit.</summary>
        public string[]? ExcludePath { get => _excludePath; set { _excludePath = value; _simple[nameof(ExcludePath)] = value; } }
        private string[]? _tagFilter;
        /// <summary>The native TagFilter argument; omission retains Pester's default and explicit null remains explicit.</summary>
        public string[]? TagFilter { get => _tagFilter; set { _tagFilter = value; _simple[nameof(TagFilter)] = value; } }
        private string[]? _excludeTagFilter;
        /// <summary>The native ExcludeTagFilter argument; omission retains Pester's default and explicit null remains explicit.</summary>
        public string[]? ExcludeTagFilter { get => _excludeTagFilter; set { _excludeTagFilter = value; _simple[nameof(ExcludeTagFilter)] = value; } }
        private string[]? _fullNameFilter;
        /// <summary>The native FullNameFilter argument; omission retains Pester's default and explicit null remains explicit.</summary>
        public string[]? FullNameFilter { get => _fullNameFilter; set { _fullNameFilter = value; _simple[nameof(FullNameFilter)] = value; } }
        private object[]? _container;
        /// <summary>The native Container argument; omission retains Pester's default and explicit null remains explicit.</summary>
        public object[]? Container { get => _container; set { _container = value; _simple[nameof(Container)] = value; } }
        private string? _output;
        /// <summary>The native Output argument; omission retains Pester's default and explicit null remains explicit.</summary>
        public string? Output { get => _output; set { _output = value; _simple[nameof(Output)] = value; } }
        /// <summary>An advanced Pester configuration, exclusive with the simple argument set.</summary>
        public object? Configuration { get; set; }
        /// <summary>Applies native CI result-file and exit behavior.</summary>
        public bool CI { get; set; }
        /// <summary>Returns the native result through the owning pipeline.</summary>
        public bool PassThru { get; set; }
        /// <summary>Uses the full-screen dashboard instead of inline output.</summary>
        public bool FullScreen { get; set; }
        /// <summary>An optional HTML destination for the final dashboard.</summary>
        public string? ExportHtml { get; set; }
        /// <summary>The dashboard title.</summary>
        public string Title { get; set; } = "Pester";
        /// <summary>Enables the adapter when supported; false preserves Pester output and adds the summary.</summary>
        public bool Live { get; set; } = true;
    }
}
