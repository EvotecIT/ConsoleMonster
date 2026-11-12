---
title: Forms and dialogs
description: "Ask for several answers at once with forms and wizards, pick dates, and add toggles, buttons and dialogs to an app; the same forms run unattended from their defaults."
slug: forms-and-dialogs
order: 42
group: apps
card: migration-form
apis: [New-ConsoleForm, New-ConsoleField, Read-ConsoleForm, Set-ConsoleFormValue, Read-ConsoleDate, New-ConsoleDialog, Form, AppDialogs]
chips: [Forms, Wizards, Dates, Unattended]
summary: "Forms and wizards with a review page, date questions, app toggles, buttons and dialogs, and unattended runs."
---

This page shows how to ask for several answers in one screen: a form or a wizard that returns typed values, a date question, and the toggles, buttons and dialogs an interactive app is built from. Every form also runs without anyone at the keyboard, from its defaults.

{{< cm-frame ids="migration-form" script="true" caption="A one-field form with an illustrative default, before any key is pressed. The form only collects answers; the caller decides what to run." >}}

## A form

`New-ConsoleForm` takes fields from `New-ConsoleField` and `Read-ConsoleForm` shows it and returns one object with a property per field. In C#, a `Form` adds its fields with `Text`, `Number`, `Toggle`, `Select`, `MultiSelect`, `Path` and `Secret`, and `Show` returns a `FormResult`.

<div class="cm-lang" data-lang="powershell">

```powershell
Import-Module ConsoleMonster

$form = New-ConsoleForm -Title 'Share plan' -Content {
    New-ConsoleField -Key Name -Label 'Share name' -Required -Default 'reports'
    New-ConsoleField -Key Size -Label 'Quota (GB)' -Kind Number -Integer -Minimum 1 -Maximum 500 -Default 50
    New-ConsoleField -Key Site -Label 'Site' -Kind Select -Choices 'Warsaw', 'Dublin' -Default 'Warsaw'
    New-ConsoleField -Key ReadOnly -Label 'Read only' -Kind Toggle
}
$answers = Read-ConsoleForm $form
if ($answers) { "Creating $($answers.Name) in $($answers.Site)" }
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
Form form = new Form("Share plan");
TextField name = form.Text("Name", "Share name");
name.Required = true;
name.Default = "reports";
NumberField size = form.Number("Size", "Quota (GB)");
size.Integer = true;
size.Minimum = 1;
size.Maximum = 500;
size.Default = 50m;
form.Select("Site", "Site", new[] { new Choice("Warsaw"), new Choice("Dublin") });
form.Toggle("ReadOnly", "Read only");

FormResult result = form.Show();
if (result.Submitted) {
    Console.WriteLine("Creating " + result.Values.Get<string>("Name") + " in " + result.Values["Site"]);
}
```

</div>

Arrows and Tab move between fields, Enter edits one, choice fields open a list that filters as you type, and Escape cancels; a cancelled form returns nothing in PowerShell and `Submitted = false` in C#. `-Kind` is `Text` (the default; `-Multiline` or `-AsSecret` for those), `Number`, `Toggle`, `Select` (`-Multiple` for several) or `Path` (`-PathKind File` or `Folder`, `-MustExist`).

## Wizards and the review page

Fields with `-Page` are grouped into pages in the order the names first appear, which turns the form into a wizard with a progress strip. Unless `-NoReview` is given, a review page follows with every answer and the equivalent command line, built from `-Command` and each field's `-ParameterName`, so the person who filled it in learns how to automate it.

```powershell
$form = New-ConsoleForm 'Install agent' -Command 'Install-Agent' -Content {
    New-ConsoleField Region 'Region' -Kind Select -Choices 'eu', 'us' -Page Connection -ParameterName Region
    New-ConsoleField Proxy 'Use a proxy' -Kind Toggle -Page Connection -ParameterName UseProxy
    New-ConsoleField ProxyAddress 'Proxy address' -VisibleWhen { $Values.Proxy } -Page Connection -ParameterName Proxy
    New-ConsoleField Path 'Install folder' -Kind Path -PathKind Folder -Page Location -ParameterName Path
}
$answers = Read-ConsoleForm $form -AppName 'Agent setup' -RememberLastChoices
```

- `-VisibleWhen` shows a field only while its script block returns true; `$Values` holds the answers so far.
- `-DependsOn Region -LoadChoices { param($Values) ... }` reloads a field's choices when another field changes, with a spinner that Escape cancels.
- `-AppName` stores presets under that name, and `-RememberLastChoices` offers the last answers next time.
- `Read-ConsoleForm -PassThru` returns `Submitted`, `Values` and `CommandLine` instead of the answers alone.

## Filling a form in advance

`Set-ConsoleFormValue` sets fields before the form is shown, or while a menu that owns it is running. It takes a hashtable of field names and values, or the result of an earlier run, so a form can start from its last answers:

```powershell
$form = New-ConsoleForm 'Connection' -Content {
    New-ConsoleField Domain 'Domain' -Default 'lab.example.test'
    New-ConsoleField Parallel 'Parallel workers' -Kind Number -Integer -Minimum 1
}
Set-ConsoleFormValue -Form $form -Values @{ Domain = 'corp.example.test'; Parallel = 2 }
$last = Read-ConsoleForm $form -PassThru
if ($last) {
    Set-ConsoleFormValue -Form $form -Values $last
}
```

In C#, `form.Apply(values)` does the same with a dictionary or a `FormValues`.

## Dates

`Read-ConsoleDate` asks for one date, or an inclusive range with `-Range`, on a calendar: arrows move the cursor, Page Up and Page Down change the month, and Enter commits. It returns a `DateTime` at midnight, or a `DateRange`. Line and accessible modes accept `yyyy-MM-dd` and dates in the current culture.

<div class="cm-lang" data-lang="powershell">

```powershell
$window = Read-ConsoleDate -Title 'Maintenance window' -Range -Default ([datetime] '2026-11-18')
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
object window = new DatePrompt("Maintenance window") { Range = true, Default = new DateTime(2026, 11, 18) }.Show();
```

</div>

`-Selection` takes the bounds, the first day of the week and a fixed "today": `-Selection @{ Today = [datetime] '2026-11-18'; FirstDayOfWeek = 'Monday' }` (`DateSelectionOptions` in C#).

## Toggles, buttons and dates inside an app

An [interactive app](/docs/interactive-apps/) keeps its controls on screen and stores each value in the app state under the control's key:

| PowerShell | C# | Value in the state |
|---|---|---|
| `New-ConsoleToggle -Key Enabled -Label 'Run nightly checks' -Value $true` | `new Toggle("Enabled", true) { Label = "Run nightly checks" }` | `$true` or `$false`; Space flips it |
| `New-ConsoleDatePicker -Key Window -Range -Selection $dates` | `dates.CreatePicker("Window", true)` | a date or a `DateRange` |
| `New-ConsoleButton -Key Apply -Label 'Apply' -OnClick { ... }` | `new Button("Apply", "Apply")` | how many times it was pressed |
| `New-ConsoleTextInput -Key Query` | `new TextInput("Query")` | the text |

```powershell
$dates = @{ Today = [datetime] '2026-11-18'; FirstDayOfWeek = 'Monday' }
Show-ConsoleApp -Title Maintenance -Content {
    New-ConsoleToggle -Key Enabled -Label 'Run nightly checks' -Value $true
    New-ConsoleDatePicker -Key Window -Range -Selection $dates
    New-ConsoleBinding -Content {
        param($State)
        "[muted]Checks[/] $(if ($State.Enabled) { 'on' } else { 'off' })  [muted]Window[/] $($State.Window)"
    }
}
```

The [interactive page](/interactive/) records the same controls in tabs, with a toggle flipped and a range picked.

## Dialogs

A dialog is a small form shown over a running app: Tab stays inside it, Escape cancels and puts the focus back where it was, and Ctrl+Enter accepts from a field. `New-ConsoleDialog` (`Ui.Dialog()` in C#) builds one from ordinary inputs; nothing is read until the app opens it. In C#, the app's `AppDialogs` service opens it from an app-thread command and calls back with the answers:

<div class="cm-lang" data-lang="powershell">

```powershell
$dialog = New-ConsoleDialog -Title 'Destination' -InitialFocus Path -Content {
    New-ConsoleTextInput -Key Path -Value 'backup'
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
var dialogs = new AppDialogs();
var dialog = Ui.Dialog().Title("Destination")
    .Content(c => c.Add(Ui.TextInput().Key("Path").Value("backup").Build()))
    .InitialFocus("Path").Build();
// From an app-thread command, with ConsoleAppOptions.Dialogs = dialogs:
dialogs.Show(dialog, answer => {
    if (answer.Accepted) { destination = answer.Value!.Get<string>("Path"); }
});
```

</div>

`-AcceptText` and `-CancelText` rename the buttons, `-DefaultAccept` focuses the affirmative one first, and `-Validate` checks the answers before the dialog closes. `Show-ConsoleApp -Dialogs` passes the service to an app in PowerShell.

## When nobody can answer

A form needs no changes to run in CI, a scheduled task or a pipeline with redirected input. Without an interactive user, `Read-ConsoleForm` uses each field's default, and fails with an error naming the first field that needs a value when there is none. The same rule holds for `Read-ConsoleDate`, which needs a valid `-Default`.

- **Detected.** CI, redirected input, `pwsh -NonInteractive` and `CONSOLEMONSTER_NONINTERACTIVE=1` all count as no user.
- **Forced.** `Read-ConsoleForm -Mode None` uses the defaults even at a keyboard, which is how a script runs the same form unattended on purpose. In C#, pass `new FormOptions { Interactive = new InteractiveOptions { Mode = InteractionMode.None } }` to `Show`.
- **Other hosts.** In remoting and the ISE the fields are asked one by one through the host, and `-Mode Accessible` (or `CONSOLEMONSTER_ACCESSIBLE=1`) asks numbered questions that confirm each answer as text.

```powershell
$answers = Read-ConsoleForm $form -Mode None
```

## Next

- [App parts](/docs/app-parts/): tabs, splitters, outlines and the other parts of a full-screen app.
- [Help and defaults](/docs/help-and-defaults/): what users see when they ask for help or skip a value.
- [Testing](/docs/testing/): drive a form or an app with scripted keys and check what it shows.
