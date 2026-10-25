# ConsoleMonster.Templates

`dotnet new` templates for ConsoleMonster.

```shell
dotnet new install ConsoleMonster.Templates
dotnet new consolemonster -o MyTool
cd MyTool
dotnet run                 # the menu: a status report and how to run the service
dotnet run -- serve        # the generic host with the live console dashboard (Ctrl+C stops it)
dotnet publish -c Release -r win-x64   # one native executable (native AOT)
```

The app shows:

- a `Menu` with a page;
- a `Table` with status cells;
- Microsoft.Extensions.Logging through `ConsoleMonster.Extensions.Logging`;
- a generic host with `ConsoleMonster.Hosting`'s dashboard.

When the output is redirected, it prints the report instead of opening the menu.

Start from a gallery demo with just the `ConsoleMonster` package:

```shell
dotnet new consolemonster --demo fleet -o FleetDemo
cd FleetDemo
dotnet run                       # the ordinary live program
# In a capture host: set CONSOLEMONSTER_INPUT to scripted keys.
# CONSOLEMONSTER_SNAPSHOT=1 skips dashboard pauses and initializes apps without input.
```

Choose a full showcase such as `fleet` or `assessment`, or a small starter such
as `fleet-filter` or `readiness-report`. Run `dotnet new consolemonster --help`
for the complete list of gallery slugs. Interactive demos run live in a terminal;
static demos print their output. Each generated `Program.cs` is the same source
used by the website gallery. Omitting `--demo` keeps the menu and hosted service above.

| Option | Default | Meaning |
| --- | --- | --- |
| `--Framework` | `net8.0` | `net8.0` or `net10.0`. |
| `--ConsoleMonsterVersion` | `2.0.2` | The ConsoleMonster package version (2.0.2 by default). |
| `--NoAot` | `false` | Leaves native AOT publishing off. |
| `--demo` | `none` | A gallery slug, or `none` for the menu and hosted service. |

For development, stage the template sources from the repository root, then install
the staging directory reported by MSBuild:

```shell
dotnet msbuild Templates/ConsoleMonster.Templates/ConsoleMonster.Templates.csproj -t:PrepareTemplateSource
dotnet new install Templates/ConsoleMonster.Templates/obj/template-source
python3 Tests/Build/test_template_gallery.py
```

The integration test packs and installs the actual `.nupkg` in an isolated template
hive, compares every generated program with the canonical gallery source, and
builds and runs the demos against the local ConsoleMonster project. It also checks
folder installation and the default template. Python 3 and the .NET 10 SDK are
required; temporary test output is removed when the test finishes.
