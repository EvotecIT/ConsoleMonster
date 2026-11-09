using System;

namespace ConsoleMonster.Capabilities {
    // One policy for prompts, apps, live output and explicitly supplied profiles. Accessibility is an opt-in floor;
    // callers can override the motion preference, but cannot turn motion back on while accessibility is enabled.
    internal static class AccessibilityPolicy {
        internal static bool Enabled(string? value) => value == "1"
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

        internal static TerminalProfile Resolve(TerminalProfile profile, bool? reducedMotion = null) {
            bool accessible = profile.IsAccessible || Interactive.InteractiveOptions.Default.Mode == Interactive.InteractionMode.Accessible || Enabled(ConsoleSettings.Variable("CONSOLEMONSTER_ACCESSIBLE"));
            bool? requested = reducedMotion ?? profile.MotionOverride;
            bool reduced = accessible || (requested ?? (profile.ReducedMotion
                || string.Equals(ConsoleSettings.Variable("CONSOLEMONSTER_MOTION"), "reduced", StringComparison.OrdinalIgnoreCase)));
            return profile.IsAccessible == accessible && profile.ReducedMotion == reduced && profile.MotionOverride == requested
                ? profile : profile.WithAccessibility(accessible, reduced, requested);
        }
    }
}
