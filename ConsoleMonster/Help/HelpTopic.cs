using System.Collections.Generic;

namespace ConsoleMonster.Help {
    /// <summary>
    /// A command's help, as structured data: what <see cref="Widgets.HelpView"/> shows. Show-ConsoleHelp fills it from
    /// PowerShell's Get-Help objects; .NET hosts can fill it from their own command descriptions.
    /// </summary>
    public sealed class HelpTopic {
        /// <summary>Creates a topic.</summary>
        /// <param name="name">The command name.</param>
        public HelpTopic(string name) {
            Name = name ?? string.Empty;
        }

        /// <summary>The command name.</summary>
        public string Name { get; }

        /// <summary>The one-line summary, or null.</summary>
        public string? Synopsis { get; set; }

        /// <summary>Optional compact command-line layout; null retains the PowerShell help layout.</summary>
        public CommandLineHelp? CommandLine { get; set; }

        /// <summary>A note shown muted under the name (for example that only partial help is installed), or null.</summary>
        public string? Notice { get; set; }

        /// <summary>The description paragraphs.</summary>
        public IList<string> Description { get; } = new List<string>();

        /// <summary>One syntax per parameter set.</summary>
        public IList<HelpSyntax> Syntax { get; } = new List<HelpSyntax>();

        /// <summary>The parameters, in help order.</summary>
        public IList<HelpParameter> Parameters { get; } = new List<HelpParameter>();

        /// <summary>Whether the command takes the common parameters (-Verbose, -ErrorAction and the others).</summary>
        public bool CommonParameters { get; set; }

        /// <summary>The examples.</summary>
        public IList<HelpExample> Examples { get; } = new List<HelpExample>();

        /// <summary>The input types (with an optional description after a tab).</summary>
        public IList<string> Inputs { get; } = new List<string>();

        /// <summary>The output types (with an optional description after a tab).</summary>
        public IList<string> Outputs { get; } = new List<string>();

        /// <summary>The notes paragraphs.</summary>
        public IList<string> Notes { get; } = new List<string>();

        /// <summary>The related links.</summary>
        public IList<HelpLink> Links { get; } = new List<HelpLink>();
    }

    /// <summary>One parameter set's syntax: the command name and its parameters in order.</summary>
    public sealed class HelpSyntax {
        /// <summary>Creates a syntax line.</summary>
        /// <param name="command">The command name.</param>
        public HelpSyntax(string command) {
            Command = command ?? string.Empty;
        }

        /// <summary>The command name.</summary>
        public string Command { get; }

        /// <summary>The parameter set's name, or null.</summary>
        public string? SetName { get; set; }

        /// <summary>The parameters.</summary>
        public IList<HelpSyntaxParameter> Parameters { get; } = new List<HelpSyntaxParameter>();

        /// <summary>Whether the common parameters follow.</summary>
        public bool CommonParameters { get; set; }
    }

    /// <summary>A parameter as it appears in a syntax line.</summary>
    public sealed class HelpSyntaxParameter {
        /// <summary>Creates a syntax parameter.</summary>
        /// <param name="name">The name without the dash.</param>
        public HelpSyntaxParameter(string name) {
            Name = name ?? string.Empty;
        }

        /// <summary>The name without the dash.</summary>
        public string Name { get; }

        /// <summary>The value type (for example <c>string[]</c>), or null for a switch.</summary>
        public string? Type { get; set; }

        /// <summary>Whether it is required.</summary>
        public bool Required { get; set; }

        /// <summary>Whether its name may be left out (a position is given).</summary>
        public bool Positional { get; set; }

        /// <summary>Whether the syntax includes a parameter name (default true). False renders only the value placeholder, as used by command-line positional arguments.</summary>
        public bool Named { get; set; } = true;
    }

    /// <summary>A parameter's details for the parameter table.</summary>
    public sealed class HelpParameter {
        /// <summary>Creates a parameter.</summary>
        /// <param name="name">The name without the dash.</param>
        public HelpParameter(string name) {
            Name = name ?? string.Empty;
        }

        /// <summary>The name without the dash.</summary>
        public string Name { get; }

        /// <summary>Whether the name has a parameter dash (default true); false for command-line positional arguments.</summary>
        public bool Named { get; set; } = true;

        /// <summary>The type, or null.</summary>
        public string? Type { get; set; }

        /// <summary>Whether it is required (in any parameter set).</summary>
        public bool Required { get; set; }

        /// <summary>The position (<c>0</c>, <c>1</c>, ...), or null when it is named only.</summary>
        public string? Position { get; set; }

        /// <summary>How it takes pipeline input (for example <c>ByValue, ByPropertyName</c>), or null for none.</summary>
        public string? Pipeline { get; set; }

        /// <summary>The default value, or null.</summary>
        public string? Default { get; set; }

        /// <summary>The aliases, or empty.</summary>
        public IList<string> Aliases { get; } = new List<string>();

        /// <summary>The description, or null.</summary>
        public string? Description { get; set; }
    }

    /// <summary>One example: a title, the code and the remarks.</summary>
    public sealed class HelpExample {
        /// <summary>Creates an example.</summary>
        /// <param name="title">The title.</param>
        /// <param name="code">The code.</param>
        public HelpExample(string? title, string code) {
            Title = title;
            Code = code ?? string.Empty;
        }

        /// <summary>The title, or null.</summary>
        public string? Title { get; }

        /// <summary>The code (PowerShell).</summary>
        public string Code { get; }

        /// <summary>The remarks, or null.</summary>
        public string? Remarks { get; set; }
    }

    /// <summary>A related link: text, an address, or both.</summary>
    public sealed class HelpLink {
        /// <summary>Creates a link.</summary>
        /// <param name="text">The link text, or null.</param>
        /// <param name="uri">The address, or null.</param>
        public HelpLink(string? text, string? uri) {
            Text = text;
            Uri = uri;
        }

        /// <summary>The link text, or null.</summary>
        public string? Text { get; }

        /// <summary>The address, or null.</summary>
        public string? Uri { get; }
    }
}
