using ConsoleMonster;
using ConsoleMonster.Interactive;

string name = new TextPrompt("Share name")
{
    Default = "Reports",
    Pattern = "[A-Za-z][A-Za-z0-9_-]{1,23}",
    PatternMessage = "Start with a letter; letters, digits, - and _ only",
    Description = "Shown to users when they browse the server."
}.Show();
Terminal.WriteMarkupLine("Creating share [bold]" + name + "[/]");
