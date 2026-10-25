function Get-ReadLineState {
    <# .SYNOPSIS
    Copies every available colour owned by the ReadLine feature.
    #>
    $options = PSReadLine\Get-PSReadLineOption
    $state = @{}
    foreach ($name in 'Command', 'Parameter', 'String', 'Variable', 'Number', 'Operator', 'Comment', 'Keyword', 'Type', 'Error', 'Selection', 'InlinePrediction') {
        $property = $options.PSObject.Properties[$name + 'Color']
        if ($property) { $state[$property.Name] = $property.Value }
    }
    $state
}

function Get-ReadLineUnowned {
    <# .SYNOPSIS
    Copies neighbouring colour and editing preferences that cosmetic styling must preserve.
    #>
    $options = PSReadLine\Get-PSReadLineOption
    $state = @{}
    $owned = Get-ReadLineState
    foreach ($property in $options.PSObject.Properties) {
        if ($owned.ContainsKey($property.Name)) { continue }
        if ($property.Name -match 'Color$|^EditMode$|^Prediction|^HistorySaveStyle$|^BellStyle$|^MaximumHistoryCount$') {
            $state[$property.Name] = $property.Value
        }
    }
    $state
}

function Assert-NativeState {
    <# .SYNOPSIS
    Compares native preference values without normalising escape sequences or enum values.
    #>
    param([hashtable] $Expected, [hashtable] $Actual)
    if ($Actual.Count -ne $Expected.Count) { throw 'Native preference count changed.' }
    foreach ($name in $Expected.Keys) {
        if ($Actual[$name] -cne $Expected[$name]) { throw "Native preference mismatch: $name" }
    }
}

function Set-ReadLineBaseline {
    <# .SYNOPSIS
    Seeds distinctive colour values while preserving all non-colour preferences.
    #>
    param([int] $Variant = 0)
    $options = PSReadLine\Get-PSReadLineOption
    foreach ($name in (Get-ReadLineState).Keys) {
        $options.PSObject.Properties[$name].Value = "$([char]27)[$(31 + $Variant);4m"
    }
}

function Assert-ReadLineTheme {
    <# .SYNOPSIS
    Checks public semantic role colours, a selection background and prediction when supported.
    #>
    $roles = @{
        CommandColor = 'Series6'; ParameterColor = 'Series1'; StringColor = 'Series2'
        VariableColor = 'Accent2'; NumberColor = 'Series3'; OperatorColor = 'Series5'
        CommentColor = 'Muted'; KeywordColor = 'Series5'; TypeColor = 'Series6'
        ErrorColor = 'Fail'; SelectionColor = 'Focus'; InlinePredictionColor = 'Muted'
    }
    $actual = Get-ReadLineState
    if ($actual.Count -lt 11) { throw 'The native PSReadLine oracle did not expose syntax and selection colours.' }
    foreach ($name in $actual.Keys) {
        $style = [ConsoleMonster.Terminal]::Theme.Resolve([ConsoleMonster.Styling.ThemeToken] $roles[$name], 'TrueColor')
        if ($name -eq 'CommentColor') { $style = $style.With(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Styling.TextDecoration'))::Italic) }
        $expected = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Rendering.SgrEncoder'))::Encode($style, 'TrueColor')
        if ($actual[$name] -cne $expected) { throw "Wrong native syntax colour: $name" }
    }
    if ($actual.SelectionColor -notmatch '48;2;') { throw 'Selection has no theme background.' }
}

function Assert-NativeCommands {
    <# .SYNOPSIS
    Verifies that cosmetic features keep real Write cmdlets and install no default-output shim.
    #>
    param([switch] $AllowOutputShim)
    foreach ($name in 'Write-Progress', 'Write-Warning', 'Write-Verbose', 'Write-Debug', 'Write-Information', 'Write-Error', 'Write-Output', 'Write-Host') {
        $command = Get-Command $name
        if ($command.CommandType -ne 'Cmdlet' -or $command.ModuleName -cne 'Microsoft.PowerShell.Utility') {
            throw "Cosmetic formatting replaced $name."
        }
    }
    if (-not $AllowOutputShim -and (Get-Command Out-Default -CommandType Function -ErrorAction Ignore)) { throw 'Cosmetic formatting installed Out-Default.' }
}
