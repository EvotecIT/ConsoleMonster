Import-Module .\ConsoleMonster.psd1 -Force

#[AnsiConsole].Markup("[underline red]Hello[/] World!");

#[Spectre.Console.AnsiConsole]::Markup("[underline red]Hello[/] World!")

$Table = [Spectre.Console.Table]::new()

[Spectre.Console.]
[Spectre.Console.TableRow]::new

$Table.Rows.Add(2)

$AnsiTable = [Spectre.Console.AnsiConsole]::Live($Table)
$AnsiTable.Start({
    #$table.AddColumn("Foo");
    #$table.AddColumn("Foo");
})