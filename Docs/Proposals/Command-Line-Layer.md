# Proposal: a command-line layer for ConsoleMonster

**Declined (October 2026).** Our command-line tools parse with System.CommandLine and write their output with
ConsoleMonster; ConsoleMonster does not build its own command-line layer while Microsoft maintains System.CommandLine.
See `ROADMAP.md` and the "Command-line tools with System.CommandLine" section of `Docs/Usage.md`. The paper is kept for
the record.

Status: decision paper. Nothing in this document is built. It describes what a command-line layer would look like,
how much of it ConsoleMonster already has, what it would cost to own, and what products would gain, so the decision
can be made on facts.

## The idea in one paragraph

One C# definition of a command drives four things that are written separately today:

- parsing the arguments;
- the help page;
- shell completion;
- the interactive form that appears when arguments are missing.

A source generator turns that definition into plain code, so the layer works with trimming and native AOT. The help
page is drawn with the design system (`HelpView`). The form is ConsoleMonster's existing form, and its review step
already shows the equivalent command line, so every interactive run teaches the non-interactive one.

## What already exists

| Piece | Where | State |
| --- | --- | --- |
| Interactive forms: text, number, toggle, choice, path, secret and text-area fields, pages, validation | `ConsoleMonster.Interactive.Form`, `Field` and its kinds | Shipped |
| Review step with the equivalent command line | `FormRunner`, `ReviewModel`, `PromptLabels.EquivalentCommand` | Shipped |
| Command-line formatting in three styles (`-Name value`, `--name value`, `/name value`), with quoting and switches | `CommandLine`, `CommandStyle` | Shipped |
| Menus that host forms, pages and runs | `Menu`, `MenuItem.FormDefinition` | Shipped |
| Help as data and its rendering: synopsis, syntax, parameter table, examples | `HelpTopic`, `HelpView` (this release) | Shipped |
| A build-time source generator in the repository | `ConsoleMonster.Generators` (fluent widget methods) | Shipped; it shows that the build and packaging route for a generator works |

What is missing is the parser itself, the attribute model, the generator that connects these pieces, and completion.

## API sketch

```csharp
[Command("scan", Description = "Checks the DNS records of a domain.")]
public sealed partial class ScanCommand : ICommand {
    [Argument(0, Description = "The domain to check.")]
    public string Domain { get; set; } = "";

    [Option("--depth", "-d", Description = "How many levels of CNAMEs to follow.")]
    [Range(1, 10)]
    public int Depth { get; set; } = 2;

    [Option("--format", Description = "Table, Json or Markdown.")]
    public OutputFormat Format { get; set; } = OutputFormat.Table;

    [Option("--force", Description = "Checks again even when the cache is fresh.")]
    public bool Force { get; set; }

    public async Task<int> RunAsync(CommandContext context, CancellationToken cancellationToken) {
        // context.Terminal, context.Log, context.Interactive ...
        return 0;
    }
}

public static Task<int> Main(string[] args) =>
    new CommandApp("dnscheck")
        .Add<ScanCommand>()
        .Add<ExportCommand>()
        .RunAsync(args);
```

Behaviour a user would see:

- `dnscheck scan example.org -d 3` parses and runs.
- `dnscheck scan --help` draws the help page with `HelpView`: synopsis, syntax, parameter table and examples.
- `dnscheck scan` on an interactive terminal, with the required `Domain` missing, opens the form. Its fields come from
  the options. The review shows `dnscheck scan example.org --depth 3`, and submitting runs the command.
- When the output is redirected or in CI, the same call fails with a usage error instead of prompting.
- `dnscheck completion pwsh` prints a completion script. The shell can be `pwsh`, `bash`, `zsh`, or `fish`.

## Source generation for AOT

The generator runs at build time on every `partial` class marked `[Command]` and emits:

1. **A parser per command.** A `switch` over option names with typed conversions for primitives, enums, `FileInfo`,
   `DirectoryInfo`, `TimeSpan`, arrays and nullable types. It uses no reflection, so trimming and native AOT need no
   annotations.
2. **A `HelpTopic` factory.** Descriptions come from the attributes, falling back to XML documentation. Examples come
   from an `[Example]` attribute. Rendering stays in `HelpView`, so help pages look the same as `Show-ConsoleHelp`.
3. **A `Form` factory**, mapped as described in the next section.
4. **Completion data.** Command, option and enum names, plus a hook for dynamic values such as file paths.
5. **Diagnostics at build time:** duplicate names, a required option with a default, an unsupported type, an argument
   index with a gap.

The runtime part is small: `CommandApp` (it dispatches subcommands and handles `--help`, `--version` and `--`
handling), the conversion helpers, error formatting and the completion script writers.

## How it maps onto the form and review machinery

| Option type | Form field | Equivalent command (`CommandStyle.Posix`) |
| --- | --- | --- |
| `string` | `TextField` | `--name value` (quoted when needed) |
| `string` with `[Multiline]` | `TextAreaField` | `--name "line1\nline2"` |
| `int`, `double`, `[Range]` | `NumberField` with bounds | `--name 3` |
| `bool` | `ToggleField` | `--name` when on, nothing when off |
| `enum` | `ChoiceField` (single) | `--name Value` |
| `T[]` of enum or string with known values | `ChoiceField` (multiple) | `--name a --name b` |
| `FileInfo`, `DirectoryInfo` | `PathField` | `--name C:\path` |
| `[Secret]` string | `SecretField` | display-only placeholder `--name ***`; the actual secret is never included in generated command text or logs |

Required arguments become required fields; validation attributes become field validation. The review step already
formats the command through `CommandLine.Format`, so the form and the parser agree by construction: both come from one
generated table.

## What it costs to own

Parsing is the part with a long tail:

- **Syntax:**
  - bundled short options (`-abc`) and `--name=value` next to `--name value`;
  - the `--` terminator, repeated options, negative numbers that look like options;
  - response files (`@args.txt`).
- **Platforms:** Windows quoting rules differ from POSIX shells, and arguments arrive already split by the runtime,
  with known quirks.
- **Completion:** four shells, each with its own script format and quoting, and dynamic completion needs a hidden
  command protocol.
- **Errors and localization:** messages for unknown options, missing values, failed conversions, with suggestions for
  near-miss names; translated where ConsoleMonster's labels are translated.
- **Compatibility:** once products depend on it, changes in parsing behaviour are breaking changes for their users'
  scripts.

A rough size, from the size of the existing generator and form code:

| Piece | Size |
| --- | --- |
| Generator | 1,500–2,500 lines |
| Runtime | about 2,000 lines |
| Tests | a similar amount: a parsing matrix, generator snapshot tests, completion scripts per shell, AOT publish checks in CI |

The ongoing work is mostly parsing edge cases reported by users and shell changes. It would live in a separate package
(`ConsoleMonster.CommandLine`), so the core stays zero-dependency and parser changes never touch rendering.

## What products would gain

- **One definition** instead of a parser setup, a help text, completion scripts and, where they exist, interactive
  prompts kept in step by hand.
- **Help pages** in the same design as the rest of the output and as `Show-ConsoleHelp` in PowerShell.
- **An interactive fallback for free.** Users who don't remember the options get a form, and the form teaches the
  command line.
- **Native AOT executables** with no reflection warnings: single-file tools that start fast, which matters for a
  `cmon` tool and for product command-line tools shipped next to PowerShell modules.
- **Consistent behaviour** across the products' command-line tools: the same conventions, errors and completion.

## Options

| Option | What we build | We maintain | Products get |
| --- | --- | --- | --- |
| A. Own layer | Parser, generator, completion, help and form integration | Everything above | All of the gains |
| B. Integration over an existing parser | An adapter that renders that parser's help with `HelpView` and opens forms from its model | The adapter; parsing stays with the parser's maintainers | Help design and interactive fallback; AOT depends on the parser chosen |
| C. Building blocks only | Nothing new; `HelpTopic`, `HelpView`, `Form` and `CommandLine` are public today | Nothing extra | Products wire the pieces by hand |

## Suggested path

1. Keep C as the baseline: the pieces are public and documented with this release.
2. List the product command-line tools that would adopt a layer within the next two releases, and what each uses for
   parsing today. Two or more adopters justify building something; fewer don't.
3. Before deciding between A and B, prototype the generator for the form and help half only, from `[Command]` classes,
   with parsing left to whatever the product uses now. That half has no parsing long tail. It proves the value of one
   definition, and it is needed by both A and B.

The decision is the owner's. This paper doesn't add a dependency or change any shipped behaviour.
