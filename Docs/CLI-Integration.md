# Command-line integration

Use the PowerShell-native surface when working in a PowerShell session:

```powershell
Show-ConsoleHelp Get-ChildItem
Get-Process | Format-ConsoleTable -Property ProcessName, Id, WorkingSet64
```

.NET hosts can use the same presentation library while keeping their own parser and execution model. The
[compiled command-line help example](../Website/examples/docs/cli-help.cs) builds a `HelpTopic`, adds a
`CommandLineHelp` catalog, grouped `HelpCommand` entries and `HelpValue` notes, and draws a `HelpView`.
The [help guide](https://consolemonster.dev/docs/help-and-defaults/#command-line-help-for-net-hosts) shows both widths
and themes. Omitting `HelpTopic.CommandLine` retains the normal PowerShell-style help layout.

- `CommandLineHelp.Usage` contains the host's complete usage syntax. `Commands` and `Values` preserve host metadata;
  the renderer groups entries and aligns descriptions without dropping long names. `ValuesHeading` defaults to the
  profile's localized label when null. Set it for a custom catalog such as report types.
- `HelpSyntaxParameter.Named = false` and `HelpParameter.Named = false` describe positional arguments. Parameter
  names omit one renderer-added dash: `-output` produces `--output`. Aliases are literal. Parameters and examples can
  be hidden independently through `HelpView.ShowParameters` and `ShowExamples`.
- `Table.KeepAllColumns` repeats the key in consecutive column bands when the full grid does not fit.
  `WrapHeaders` preserves long headings. Spanning group sections, presenters and totals follow the parent table.
  `TitleIndent` changes only the grid title's indentation; record/card titles retain their existing layout.
- `Tree.WrapLabels` wraps labels, values and notes instead of shortening them. It is an explicit opt-in.
- `RunTracker.Add()` reserves a stable index while discovery continues. It rejects additions after completion.
  Snapshot and update operations retain the tracker's synchronization.
- `Terminal.UseProfilePolicy(profile => profile.WithColorDepth(ColorDepth.None))` applies a policy within the current
  asynchronous flow. Nested policies compose and disposal restores the preceding policy. The existing target,
  keyboard input, recording and changing console dimensions remain available. As with other terminal scopes,
  dispose nested scopes in reverse order within the flow that created them.
- Raw `RecordView.AddField` values remain literal and use shared presenters. Use explicit markup cells when markup
  is intended. Numeric form defaults accept the same numeric values as submitted form values.

No host-specific command names, parser dependencies or execution adapters are built into these APIs.
