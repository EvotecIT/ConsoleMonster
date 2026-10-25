using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Keeps typed operation configuration and individual shell settings mutually exclusive without mutating caller-owned options.</summary>
    internal static class PSOptions {
        /// <summary>Chooses typed configuration or shell defaults and rejects ambiguous mixed settings.</summary>
        public static T Resolve<T>(PSCmdlet command, T? options, Func<T> defaults, params string[] individualSettings) where T : class {
            if (options == null) { return defaults(); }
            foreach (string name in individualSettings) {
                if (command.MyInvocation.BoundParameters.ContainsKey(name)) {
                    throw new PSArgumentException("Use -Options or individual settings; -" + name + " cannot be combined with -Options.");
                }
            }
            return options;
        }
    }
}
