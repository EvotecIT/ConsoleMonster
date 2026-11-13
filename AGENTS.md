# ConsoleMonster

C# terminal UI library (rich text, tables, charts, live dashboards, prompts,
interactive apps, recording and export) with a PowerShell module built on it,
plus logging/hosting extensions and the consolemonster.dev website.

## Layout

- `ConsoleMonster/`: the core library. `ConsoleMonster.Graphics/`,
  `ConsoleMonster.Markdown/`, `ConsoleMonster.Hosting/` and
  `ConsoleMonster.Extensions.Logging|NLog|Serilog/` are add-on packages.
- `ConsoleMonster.Generators/`: source generator for the fluent API.
- `ConsoleMonster.PowerShell/` and `ConsoleMonster.PowerShell.Display/`: the
  binary cmdlets. `Module/`: manifest, format data, build, Pester tests,
  examples and generated docs.
- `ConsoleMonster.CLI/`: command-line tool.
- Tests: `ConsoleMonster.Tests/` (engine, live sessions, concurrency),
  `ConsoleMonster.Extensions.Tests/` (logging, hosting, golden files under
  `Golden/`), `ConsoleMonster.CLI.Tests/`, `Module/Tests/` (Pester),
  `Tests/AotSmoke/` (Native AOT smoke). `ConsoleMonster.TestHost/` supports them.
- `ConsoleMonster.Examples*/`: C# showcases. `ConsoleMonster.Benchmarks/`.
- `Templates/ConsoleMonster.Templates/`: `dotnet new` template.
- `Website/`: the consolemonster.dev site. Read `Website/AGENTS.md` before
  working there.
- `Docs/`: design, behaviour contract and usage documents.

## Build and test

```powershell
dotnet build ConsoleMonster.sln -c Release
dotnet test ConsoleMonster.Tests/ConsoleMonster.Tests.csproj -c Release --no-build
dotnet test ConsoleMonster.Extensions.Tests/ConsoleMonster.Extensions.Tests.csproj -c Release --no-build
dotnet test ConsoleMonster.CLI.Tests/ConsoleMonster.CLI.Tests.csproj -c Release --no-build -f net8.0
Invoke-Pester -Path ./Module/Tests                    # after building ConsoleMonster.PowerShell
./Module/Build/Build-Module.ps1 -RunMode Build -SignModule $false -SkipInstall $true
./Build/Build-Project.ps1 -ConfigurationGateMode Build  # NuGet packages only
```

- Targets: libraries `netstandard2.0;net472;net8.0;net10.0`; cmdlets
  `net472;net8.0`; engine and extension tests `net472;net8.0;net10.0`; CLI
  `net8.0`. SDK pinned in `global.json` (10.0.100).
- Warnings are errors (`Directory.Build.props`). Nullable is on, implicit
  usings are off.
- Run Pester in both Windows PowerShell 5.1 and PowerShell 7. CI pins Pester
  6.1.0. The source module loads development binaries from
  `ConsoleMonster.PowerShell/bin`.
- Release only: `./Module/Build/Build-Module.ps1 -RunMode Publish` signs and
  publishes NuGet, PowerShell Gallery and GitHub in that order.

## Generated files

- `Module/Docs/*.md` and `Module/en-US/ConsoleMonster-help.xml` come from the
  cmdlet XML docs. Regenerate with
  `./Module/Build/Build-Module.ps1 -RunMode Documentation`.
- `Module/ConsoleMonster.format.ps1xml` is generated from the widget types.
  After adding or renaming a widget, build `ConsoleMonster.PowerShell` (Debug)
  and run `pwsh -NoProfile -File Module/Build/Update-ConsoleFormatData.ps1`.
- Website frames, replays, data and the README's code blocks and images come
  from `Website/examples/` through `Website/tools/SiteFrames`
  (`capture`, `render`, `readme`). Never hand-edit them; see `Website/AGENTS.md`.
- Golden files: set `CONSOLEMONSTER_UPDATE_GOLDEN=1` to rewrite them after an
  intended change, then review the diff.

## Conventions

- C#: block-scoped namespaces, braces on the same line (`.editorconfig`).
- PowerShell and C# share one vocabulary. Follow `Docs/PowerShell-Conventions.md`
  for verbs (`New` builds, `Show` renders, `Read` asks, `Invoke` waits, `Start`
  returns a handle) and `Docs/Behaviour-Contract.md` for interaction rules.
- PowerShell examples are UTF-8 with a BOM so Windows PowerShell 5.1 reads them.
- Website and README show only real ConsoleMonster output: no screenshots or
  hand-drawn frames.

## Gotchas

- Trash tests use the real recycle bin only when `CONSOLEMONSTER_TEST_REAL_TRASH=1`
  (set in CI); local runs skip them.
- Rendering depends on the terminal profile. Pin `CONSOLEMONSTER_COLOR`,
  `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_THEME` and `CONSOLEMONSTER_CULTURE`
  for reproducible output.
- The .NET 8 and .NET 10 targets are AOT compatible; reflection-based
  inspection needs care when trimming (see `CONTRIBUTING.md`).
