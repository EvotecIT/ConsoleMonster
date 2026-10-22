---
title: Three ways to write it in C#
description: "Fluent methods, object initializers or the Ui factories: three complete programs that print the same frame, and when each reads best."
slug: csharp-styles
order: 2
group: start
card: three-fluent
chips: [Fluent, Objects, Ui factories]
apis: [Table.For, DashboardBuilder, Ui]
summary: "Fluent, objects or Ui factories: the same widgets, three ways."
---

Every widget can be written in three styles. They build the same objects, so they print the same output: the three programs on this page ran for it, and each printed the frame below cell for cell. Use whichever reads best for the case, and mix them freely in one program.

To run one, make a console project and replace its `Program.cs`:

```bash
dotnet new console -n Servers
cd Servers
dotnet add package ConsoleMonster
# Replace Program.cs with one of the programs below.
dotnet run
```

## Fluent

A `With...` method for every settable property and `Add...` methods for every collection, plus typed tables: `Table.For(items)` reads each column's header from its expression. The whole layout is one expression that reads top to bottom. The docs and the gallery use this style.

{{< cm-frame id="three-fluent" >}}

## Object initializers

The canonical model: each widget is a variable, set with properties (`new Table { ShowTotals = true }`) and filled with `Add` calls. It reads best when you set many options at once, fill rows in a loop, or keep a widget to change later.

{{< cm-frame id="three-objects" frame="false" caption="Prints the frame above." >}}

## Ui factories

`using static ConsoleMonster.Ui;` brings short factories (`Rows`, `Columns`, `Panel`, `Table`, `KeyValue`, `Text`, `Sparkline` and others), so the code has the shape of the screen. The `With...` methods are extension methods in each widget's own namespace, so `ConsoleMonster.Layout` and `ConsoleMonster.Widgets` stay among the `using` lines. With the factories, write `Table(items)` rather than `Table.For(items)`, because the factory takes the name `Table`.

{{< cm-frame id="three-composed" frame="false" caption="Prints the frame above." >}}

## When to use which

- **One widget, configured in one place:** fluent.
- **Many options, rows from a loop, or a widget you update later:** object initializers.
- **A layout of panels, rows and columns:** the Ui factories, or fluent if you prefer `new`.
- **A live dashboard:** `DashboardBuilder`, which hands each live widget back through an `out` parameter for its typed handle (see [Live sessions](/docs/live-sessions/)).

The fluent methods are generated at build time from each widget's public properties and compiled into `ConsoleMonster.dll`, so every property has one, on every target framework, with nothing generated or reflected at run time.
