using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    public static partial class ConsoleHostViews {
        /// <summary>Registers one type's view and optional value shaper in the owning PowerShell runspace.</summary>
        /// <param name="registration">The type's display definition.</param>
        /// <param name="shaper">Optional owning-runspace value transformation.</param>
        /// <param name="onlyIfMissing">Keeps an existing view when registering only a shaper.</param>
        public static void Register(ConsoleViewRegistration registration, ScriptBlock? shaper = null, bool onlyIfMissing = false) {
            if (registration == null) { throw new ArgumentNullException(nameof(registration)); }
            if (!onlyIfMissing) { EmbeddedDisplayIntegration.Unregister(registration.TypeName); }
            if (shaper != null) { Binding.PSShaping.RegisterShaper(registration.TypeName, shaper); }
            ConsoleFormatting.Register(registration, onlyIfMissing);
        }

        /// <summary>Imports validated data-only view definitions using the owning host's module and provider context.</summary>
        /// <param name="source">The owning host cmdlet.</param>
        /// <param name="path">Resolved filesystem path.</param>
        public static void Import(PSCmdlet source, string path) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ConsoleModuleViews.Import(source, path, false);
        }

        /// <summary>Reads validated data-only view definitions without registering or executing the file.</summary>
        /// <param name="source">The owning host cmdlet.</param>
        /// <param name="path">Resolved filesystem path.</param>
        /// <returns>The validated definitions.</returns>
        public static IReadOnlyList<ConsoleViewRegistration> Read(PSCmdlet source, string path) {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (EmbeddedDisplayIntegration.TryReadV2(source, path, out Display.ConsoleViewDocument? document)) {
                List<ConsoleViewRegistration> result = new List<ConsoleViewRegistration>();
                foreach (string typeName in document!.TypeNames) { result.Add(new ConsoleViewRegistration(typeName)); }
                return result;
            }
            return ConsoleModuleViews.Read(source, path);
        }

        /// <summary>Removes a type's registered view and value shaper.</summary>
        /// <param name="typeName">The PowerShell type name.</param>
        /// <returns>Whether a saved view or value shaper existed, including an inactive schema 2 definition.</returns>
        public static bool Unregister(string typeName) {
            bool embedded = EmbeddedDisplayIntegration.Unregister(typeName);
            bool view = ConsoleFormatting.Unregister(typeName);
            bool shaper = Values.ValueShaper.Unregister(typeName);
            return embedded || view || shaper;
        }
    }
}
