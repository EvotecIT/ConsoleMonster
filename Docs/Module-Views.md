# Module-owned console views

A module can ship ConsoleMonster views with **no required module or package dependency**. Put `<ModuleName>.consoleviews.psd1` beside its manifest. ConsoleMonster discovers files for already loaded modules when formatting is enabled or a ConsoleMonster view is built, including modules imported after ConsoleMonster.

```powershell
# Contoso.Servers.consoleviews.psd1
@{
    SchemaVersion = 1
    Views = @(
        @{
            TypeName = 'Contoso.Server'
            View = 'Table'
            Property = @(
                'Name'
                @{ Name = 'Health'; Expression = 'State'; Presenter = 'StatusWord' }
                'Uptime'
            )
            GroupBy = 'Site'
            Group = 'ContosoServers'
        }
    )
}
```

The file is a PowerShell data file, loaded with `Import-PowerShellDataFile`. It contains property names and declarative column options, with no script blocks, commands or executable shapers. `SchemaVersion` is `1`; `Views` contains entries with a `TypeName`, optional `View` (`Table`, `Cards`, `Tree`, `Inspector`, `Chart`), `Property`, `GroupBy` and `Group`. `GroupBy` is a property name. The whole file is validated before any registration is installed. Invalid automatically discovered files produce a warning; explicit imports report the error.

The supplying module continues to return ordinary typed objects when ConsoleMonster is absent. It does not import ConsoleMonster or register a formatter itself. Its users choose whether to enable default views or invoke a formatter directly:

```powershell
Import-Module Contoso.Servers
Import-Module ConsoleMonster
Enable-ConsoleFormatting
Get-ContosoServer

# Explicit formatting and export use the same registered columns.
Get-ContosoServer | New-ConsoleTable | Export-ConsoleFrame -Format Markdown
```

Files extend the existing `Register-ConsoleView` registry. An explicit registration takes precedence over automatically discovered module data. Registrations belong to the current PowerShell runspace. Disabling session formatting preserves registrations for later use.

```powershell
# Import a non-conventional location explicitly.
Register-ConsoleView -Path ./views/Contoso.Servers.consoleviews.psd1

# Executable expressions belong in an explicit registration in the current session.
Register-ConsoleView -TypeName Contoso.Server -Property Name, @{
    Name = 'UptimeDays'
    Expression = { $_.Uptime.TotalDays }
    FormatString = 'N1'
}

Unregister-ConsoleView -TypeName Contoso.Server
```

Discovery does not import other modules or alter their objects. A discovered module is inspected once per session; use `Register-ConsoleView -Path` to reload a file during development. Existing explicit registrations are kept during automatic discovery. PowerShell's own named `Format-* -View` definitions remain separate and come from `.format.ps1xml` files.

For .NET code, declare the equivalent columns in the existing table API. The `.consoleviews.psd1` discovery convention belongs to PowerShell modules.

```csharp
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

var table = new Table { RecordViewRows = 0 };
table.AddColumn("Name");
table.AddColumn(new TableColumn("Health") { Presenter = CellPresenters.StatusWord });
table.AddValues("alpha", "Pass");
```

The test fixture under `Module/Tests/Fixtures/EverydayViews` is a complete dependency-free example module. Product-specific ADX, Cx, CertNoob and TestimoX view designs are supplied by their owning modules.
