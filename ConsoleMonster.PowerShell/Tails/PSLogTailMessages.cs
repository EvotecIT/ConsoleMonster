using System;
using System.Globalization;
using System.Resources;

namespace ConsoleMonster.PowerShell {
    /// <summary>Resource-owned diagnostics for the PowerShell log-source adapters.</summary>
    internal static class PSLogTailMessages {
        private static readonly ResourceManager Resources = new ResourceManager("ConsoleMonster.PowerShell.Tails.PSLogTailMessages", typeof(PSLogTailMessages).Assembly);

        internal static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture) ?? throw new InvalidOperationException(name);
    }
}
