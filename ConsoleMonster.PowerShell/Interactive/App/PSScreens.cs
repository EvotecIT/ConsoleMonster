using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>AppScreen titles written as script blocks: built from $State on the app's input thread.</summary>
    internal static class PSScreens {
        private static readonly ConditionalWeakTable<AppScreen, PSAppScript> Titles = new ConditionalWeakTable<AppScreen, PSAppScript>();

        internal static void SetTitle(AppScreen screen, ScriptBlock title) {
            PSAppScript script = new PSAppScript(title);
            screen.TitleSource = state => string.Join(" ", script.Invoke(state).Select(PSWidgets.ToText).ToArray());
            Titles.Add(screen, script);
        }

        internal static PSAppScript? TitleScript(AppScreen screen) => Titles.TryGetValue(screen, out PSAppScript? script) ? script : null;
    }
}
