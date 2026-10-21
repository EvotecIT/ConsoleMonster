using System.Collections.Generic;
using System.Globalization;

namespace ConsoleMonster.Capabilities {
    public sealed partial class CapabilityDetector {
        // The environment advertises support, but TerminalProfile owns the final output/CI/mode/VT guards.
        private bool DetectHyperlinks(TerminalKind kind, List<string> decisions) {
            bool supported = kind == TerminalKind.WindowsTerminal || kind == TerminalKind.VisualStudioCode
                || kind == TerminalKind.ITerm2 || kind == TerminalKind.WezTerm
                || (kind == TerminalKind.Vte && VersionAtLeast("VTE_VERSION", 5000))
                || (kind == TerminalKind.Konsole && VersionAtLeast("KONSOLE_VERSION", 220400));
            string value = Get("CONSOLEMONSTER_HYPERLINKS");
            switch (value.ToLowerInvariant()) {
                case "1":
                case "true":
                case "on":
                    supported = true;
                    break;
                case "0":
                case "false":
                case "off":
                    supported = false;
                    break;
                case "":
                case "auto":
                    break;
                default:
                    decisions.Add("CONSOLEMONSTER_HYPERLINKS: ignored invalid value '" + value + "'");
                    break;
            }
            decisions.Add("hyperlinks: " + supported + " (terminal/CONSOLEMONSTER_HYPERLINKS; requires interactive VT, never redirected or CI)");
            return supported;
        }

        private bool VersionAtLeast(string name, int minimum) =>
            int.TryParse(Get(name), NumberStyles.None, CultureInfo.InvariantCulture, out int version) && version >= minimum;
    }
}
