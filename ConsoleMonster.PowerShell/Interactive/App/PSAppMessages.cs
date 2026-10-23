using System.Globalization;
using System.Resources;

namespace ConsoleMonster.PowerShell {
    /// <summary>Localized diagnostics for the PowerShell application script lifetime and native state contract.</summary>
    internal static class PSAppMessages {
        private static readonly ResourceManager Resources = new ResourceManager(
            "ConsoleMonster.PowerShell.Interactive.App.PSAppMessages", typeof(PSAppMessages).Assembly);

        /// <summary>A script must be attached in the runspace that created it.</summary>
        internal static string CreatingRunspace => Get(nameof(CreatingRunspace));

        /// <summary>A script can belong to only one running application.</summary>
        internal static string AlreadyAttached => Get(nameof(AlreadyAttached));

        /// <summary>Invocation requires the active input thread and original runspace.</summary>
        internal static string ActiveInputThread => Get(nameof(ActiveInputThread));

        /// <summary>Native application state accepts string keys.</summary>
        internal static string StringStateKeys => Get(nameof(StringStateKeys));

        /// <summary>A dependent table must be created as a widget inside an application.</summary>
        internal static string BindingRequiresFactory => Get(nameof(BindingRequiresFactory));

        private static string Get(string name) => Resources.GetString(name, CultureInfo.CurrentUICulture)!;
    }
}
