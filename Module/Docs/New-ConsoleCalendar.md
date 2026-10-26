---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleCalendar
## SYNOPSIS
Creates a month or year calendar with dated operational outcomes.

Returns a passive widget. Pipeline rows provide Date, Status and Label properties, with explicit property overrides. Duplicate dates retain every row; the displayed status uses Fail, Warn, Running, Pending, Pass, Info, Skip precedence. Dates retain calendar components without time-zone conversion. ISO week labels describe the Monday in each row, including with a Sunday-first display.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleCalendar [-Year <int>] [-Month <int>] [-View <CalendarView>] [-InputObject <psobject[]>] [-DateProperty <string>] [-StatusProperty <string>] [-LabelProperty <string>] [-FirstDayOfWeek <DayOfWeek>] [-Culture <cultureinfo>] [-Today <DateTime>] [-NoLegend] [<CommonParameters>]
```

## DESCRIPTION
Creates a month or year calendar with dated operational outcomes.

Returns a passive widget. Pipeline rows provide Date, Status and Label properties, with explicit property overrides. Duplicate dates retain every row; the displayed status uses Fail, Warn, Running, Pending, Pass, Info, Skip precedence. Dates retain calendar components without time-zone conversion. ISO week labels describe the Monday in each row, including with a Sunday-first display.

## EXAMPLES

### EXAMPLE 1
```powershell
$calendar = $backups | New-ConsoleCalendar -Year 2026 -Month 10; $calendar | Show-Console
```

Display backup outcomes for October.

### EXAMPLE 2
```powershell
New-ConsoleCalendar | Show-Console
```

Show this month.

### EXAMPLE 3
```powershell
New-ConsoleCalendar -Year 2026 -View Year -FirstDayOfWeek Monday
```

Build a year view with Monday-first weeks.

## PARAMETERS

### -Culture
Culture for month and weekday labels and the default week start.

```yaml
Type: CultureInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DateProperty
Date member (default Date). Convert timestamps to your business time zone before binding.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FirstDayOfWeek
Override the culture's first weekday.

```yaml
Type: DayOfWeek
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Rows to mark, retaining their original values for day details.

```yaml
Type: PSObject[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -LabelProperty
Description member (default Label).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Month
Gregorian month for Month view; without Year and Month the current month is shown.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoLegend
Omit the status symbol legend.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StatusProperty
Semantic outcome member (default Status).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Today
Civil date to emphasize as today, useful with a fixed report clock.

```yaml
Type: DateTime
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -View
Month (default) or Year.

```yaml
Type: CalendarView
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Month, Year

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Year
Gregorian year to display; default the current year (or the year of Today).

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject[]`

## OUTPUTS

- `ConsoleMonster.Widgets.Calendar`

## RELATED LINKS

- None
