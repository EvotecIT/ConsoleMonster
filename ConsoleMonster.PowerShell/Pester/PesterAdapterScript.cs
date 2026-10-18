using System.IO;
using System.Reflection;

namespace ConsoleMonster.PowerShell.Pester {
    /// <summary>
    /// Loads the PowerShell side of Invoke-ConsolePester, kept as the native script <c>Pester/PesterAdapter.ps1</c> and
    /// embedded in the assembly. The script imports Pester lazily, builds the configuration, checks that the Pester internals
    /// the adapter relies on are present, and either runs Pester with the adapter in place of Pester's screen output plugin
    /// or runs Pester unchanged. Pester has no public reporter API (see Docs/Pester-Upstream-Proposal.md).
    /// </summary>
    internal static class PesterAdapterScript {
        private static string? _text;

        /// <summary>The script text.</summary>
        public static string Text {
            get {
                if (_text == null) {
                    using (Stream stream = typeof(PesterAdapterScript).GetTypeInfo().Assembly.GetManifestResourceStream("ConsoleMonster.PowerShell.Pester.PesterAdapter.ps1")!)
                    using (StreamReader reader = new StreamReader(stream)) {
                        _text = reader.ReadToEnd();
                    }
                }
                return _text;
            }
        }
    }
}
