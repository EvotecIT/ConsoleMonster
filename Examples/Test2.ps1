$table = [Spectre.Console.Table]::new()
$table.AddColumn([Spectre.Console.TableColumn]::new([Spectre.Console.Markup]::new("[yellow]Foo[/]")));
$table.AddColumn([Spectre.Console.TableColumn]::new("[blue]Bar[/]"));
$table.AddRow("Baz", "[green]Qux[/]");
$table.AddRow( [Spectre.Console.Markup]::new("[blue]Corgi[/]"), [Spectre.Console.Panel]::new("Waldo"));


[Spectre.Console.AnsiConsole]::Render($table);