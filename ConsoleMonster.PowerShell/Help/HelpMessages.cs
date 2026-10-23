using System;
using System.Globalization;
using System.Resources;

namespace ConsoleMonster.PowerShell {
    /// <summary>Resource-owned help-adapter labels with the render profile's language and English fallback.</summary>
    internal static class HelpMessages {
        private static readonly ResourceManager Resources = new ResourceManager("ConsoleMonster.PowerShell.Help.HelpMessages", typeof(HelpMessages).Assembly);

        internal static string Get(string name, CultureInfo? language = null) =>
            Resources.GetString(name, language ?? CultureInfo.CurrentUICulture) ?? throw new InvalidOperationException(name);
    }
}
