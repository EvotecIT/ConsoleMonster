namespace ConsoleMonster.Localization {
    public sealed partial class WidgetLabels {
        /// <summary>Command-line usage section heading.</summary>
        public string HelpUsage { get; set; } = "Usage";
        /// <summary>Command-line command catalog heading.</summary>
        public string HelpCommands { get; set; } = "Commands";
        /// <summary>Command-line value catalog heading.</summary>
        public string HelpValues { get; set; } = "Values";
        /// <summary>Command-line option catalog heading.</summary>
        public string HelpOptions { get; set; } = "Options";
        /// <summary>Command-line examples heading.</summary>
        public string HelpExamples { get; set; } = "Examples";
        /// <summary>Required-option description prefix.</summary>
        public string HelpRequired { get; set; } = "Required.";
        /// <summary>Default-value annotation, with the value in placeholder zero.</summary>
        public string HelpDefault { get; set; } = "[default: {0}]";
    }
}
