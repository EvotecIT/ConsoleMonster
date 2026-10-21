using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Widgets;

// What ConsoleMonster detected about this console, and why.
TerminalProfile profile = Terminal.Profile;
KeyValueList facts = new KeyValueList()
    .Add("Mode", profile.Mode.ToString())
    .Add("ColorDepth", profile.ColorDepth.ToString())
    .Add("Glyphs", profile.Glyphs.ToString())
    .Add("Size", profile.Width + " × " + profile.Height);
Terminal.Write(facts);
foreach (string decision in profile.Decisions) {
    Terminal.WriteMarkupLine("  " + ConsoleMonster.Text.Markup.Escape(decision));
}