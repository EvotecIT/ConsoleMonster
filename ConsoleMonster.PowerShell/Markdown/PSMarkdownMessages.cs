using System;
using System.Globalization;
using System.Resources;

namespace ConsoleMonster.PowerShell {
    /// <summary>Owns the explicit Markdown reader's labels and provider diagnostics.</summary>
    internal static class PSMarkdownMessages {
        private static readonly ResourceManager Resources = new ResourceManager("ConsoleMonster.PowerShell.Markdown.PSMarkdownMessages", typeof(PSMarkdownMessages).Assembly);

        internal static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? throw new InvalidOperationException(key);
    }
}
