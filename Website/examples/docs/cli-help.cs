using ConsoleMonster;
using ConsoleMonster.Help;
using ConsoleMonster.Widgets;

HelpTopic topic = new HelpTopic("atlas") {
    Synopsis = "Inspect saved inventory and write reports.",
    CommandLine = new CommandLineHelp { Usage = "atlas [command] [options]" }
};
topic.CommandLine.Commands.Add(new HelpCommand("inspect", "Inspect the saved inventory.",
    "Inventory"));
topic.CommandLine.Commands.Add(new HelpCommand("export", "Write a report from saved observations.",
    "Reports"));
topic.CommandLine.Values.Add(new HelpValue("inventory", "Host and service observations.",
    "Saved data") {
    Note = "offline"
});
HelpParameter output = new HelpParameter("-output") {
    Type = "path", Description = "Destination for the generated report.", Default = "report.html"
};
output.Aliases.Add("-o");
topic.Parameters.Add(output);
topic.Examples.Add(new HelpExample(null, "atlas export --output report.html"));
Terminal.Write(new HelpView(topic));
