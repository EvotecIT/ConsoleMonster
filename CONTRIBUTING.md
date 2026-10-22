# Contributing to ConsoleMonster

Thank you for helping. This page is for people working on the repository; the [README](README.md) and the
[website](https://consolemonster.dev/) are for people using ConsoleMonster.

## Report an issue

[Open an issue](https://github.com/EvotecIT/ConsoleMonster/issues) with the host (Windows PowerShell 5.1, PowerShell 7
or a .NET application), the terminal and its font, the module or package version, and a small reproduction.

## Build and test

Build the solution and run the .NET and PowerShell suites:

```powershell
dotnet build ConsoleMonster.sln -c Release
dotnet test ConsoleMonster.Tests/ConsoleMonster.Tests.csproj -c Release --no-build
Invoke-Pester -Path ./Module/Tests
```

Run Pester 5 in both Windows PowerShell 5.1 and PowerShell 7 after building the module binaries. CI exercises both
PowerShell hosts and the .NET Framework 4.7.2 / .NET 8 / .NET 10 test targets. The source module can load development
binaries from `ConsoleMonster.PowerShell/bin`.

## The website and the README

The website (`Website/`) and the README show only code that ran. Their examples live under `Website/examples/`, and
`Website/tools/SiteFrames` runs them to make every frame and recording; see `Website/AGENTS.md` for the details.

- `dotnet run --project Website/tools/SiteFrames -c Release -- capture Website` records the frames,
  `... -- render Website` writes the site data and fails when anything shown has drifted from its source.
- The README's code blocks are marked `<!-- example: <source> -->` and come from the same example sources.
  `... -- readme Website` writes them from their sources; the render fails when one has drifted.
- The README's images are the site's own frames: `Website/build/Render-SiteImages.ps1` screenshots them into
  `Website/static/readme/` after a render.

## Build and release

The repository uses PSPublishModule / PowerForge for C# packaging, module generation and coordinated publication.
Run these commands from the repository root with PowerShell 7 and PSPublishModule installed:

```powershell
# Build the NuGet package and PowerShell module together, without signing or installing locally.
./Module/Build/Build-Module.ps1 -RunMode Build -SignModule $false -SkipInstall $true

# Preview the C# package version without building, updating files or publishing.
./Build/Build-Project.ps1 -ConfigurationGateMode Manifest

# Build only the C# package.
./Build/Build-Project.ps1 -ConfigurationGateMode Build

# Sign the assemblies, NuGet package and module, then publish the coordinated release.
./Module/Build/Build-Module.ps1 -RunMode Publish
```

The coordinated command builds packages before the module, uses the C# package as the shared version source, and
publishes in this order: **NuGet, PowerShell Gallery, GitHub**. Both publishers use the `ConsoleMonster-v{version}`
release identity. GitHub receives the C# release archive and the PowerShell module ZIP, with generated release notes.
The release signs the C# assemblies, NuGet package and PowerShell module with the configured certificate.

Publishing needs the configured signing certificate and destination credentials. Credential file paths can be
configured in `Build/project.build.json`; the module script also accepts `PowerShellGalleryApiKeyPath` and
`GitHubApiKeyPath`. Build mode does not upload packages. Generated packages and coordinated staging are under
`Artefacts/`; module build reports are under `Module/Artefacts/`.

## Native AOT

The .NET 8 and .NET 10 targets are marked AOT compatible. When trimming, disable the object inspector's reflection
member source with `InspectorOptions.UseReflection = false`, or explicitly preserve the members your application
inspects. The general-purpose inspection API still emits a trimming warning because its options are chosen at runtime.
Suppress that call-site warning only when you have verified your inputs use dictionary, JSON or custom member sources
with reflection disabled. Typed tables preserve their public properties; inferred columns use declaration order on
managed runtimes and stable alphabetical order when Native AOT does not expose metadata tokens.

CI publishes and runs a native executable on .NET 8 and .NET 10, checking typed and inferred tables, reflection-free
dictionary and JSON inspection, and text, HTML and SVG exports. This applies to the .NET library; the PowerShell
module runs inside its PowerShell host and is not a standalone Native AOT application.

## More reading

- [Usage guide](Docs/Usage.md): rich text, tables, prompts, forms, Pester output, exports and testing APIs.
- [Design system](Docs/Design-System.md): terminal layout, focus, status and interaction conventions.
- [PowerShell conventions](Docs/PowerShell-Conventions.md): every widget's cmdlet form, live update command and .NET
  method form.
- [C# showcases](ConsoleMonster.Examples) and [PowerShell examples](Module/Examples): runnable examples.
