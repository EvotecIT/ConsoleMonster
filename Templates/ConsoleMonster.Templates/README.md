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

| Option | Default | Meaning |
| --- | --- | --- |
| `--Framework` | `net8.0` | `net8.0` or `net10.0`. |
| `--ConsoleMonsterVersion` | `2.0.*` | The ConsoleMonster package version (the newest 2.0 release by default). |
| `--NoAot` | `false` | Leaves native AOT publishing off. |
