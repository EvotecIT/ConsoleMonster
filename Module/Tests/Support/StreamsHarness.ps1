# Helpers run only in the isolated child process used by ConsoleMonster.Streams.Tests.ps1.
function Get-StreamsProperties {
    <# .SYNOPSIS
    Returns the native properties owned by Streams on the current runtime.
    #>
    if ($PSVersionTable.PSVersion -ge [version] '7.2') {
        foreach ($name in 'Warning', 'Verbose', 'Debug', 'Error') { $PSStyle.Formatting.PSObject.Properties[$name] }
    } else {
        foreach ($stream in 'Warning', 'Verbose', 'Debug') {
            foreach ($suffix in 'ForegroundColor', 'BackgroundColor') { $Host.PrivateData.PSObject.Properties[$stream + $suffix] }
        }
    }
}

function Get-StreamsState {
    <# .SYNOPSIS
    Copies native property values so later writes cannot mutate the baseline.
    #>
    $state = @{}
    foreach ($property in Get-StreamsProperties) { $state[$property.Name] = $property.Value }
    $state
}

function Set-StreamsBaseline {
    <# .SYNOPSIS
    Seeds distinctive native preferences before enabling Streams, without changing commands.
    #>
    param([int] $Variant = 0)
    foreach ($property in Get-StreamsProperties) {
        if ($PSVersionTable.PSVersion -ge [version] '7.2') {
            $property.Value = "$([char]27)[$(35 + $Variant)m"
        } else {
            $property.Value = [ConsoleColor] (5 + $Variant)
        }
    }
}

function Assert-StreamsState {
    <# .SYNOPSIS
    Compares every owned native property with a saved or independently resolved value.
    #>
    param([hashtable] $Expected)
    $actual = Get-StreamsState
    if ($actual.Count -ne $Expected.Count) { throw 'Native stream property count changed.' }
    foreach ($name in $Expected.Keys) {
        if ($actual[$name] -cne $Expected[$name]) { throw "Native stream property mismatch: $name" }
    }
}

function Get-StreamsExpected {
    <# .SYNOPSIS
    Resolves expectations through the public theme, SGR encoder and console colour map.
    #>
    $roles = @{ Warning = 'Warn'; Verbose = 'Info'; Debug = 'Muted'; Error = 'Fail' }
    $theme = [ConsoleMonster.Terminal]::Theme
    $expected = Get-StreamsState
    if ($PSVersionTable.PSVersion -ge [version] '7.2') {
        foreach ($stream in 'Warning', 'Verbose', 'Debug', 'Error') {
            $style = $theme.Resolve([ConsoleMonster.Styling.ThemeToken] $roles[$stream], 'TrueColor')
            $expected[$stream] = [ConsoleMonster.Rendering.SgrEncoder]::Encode($style, 'TrueColor')
        }
    } else {
        $profile = [ConsoleMonster.Terminal]::Profile.WithColorDepth('Ansi16')
        $context = [ConsoleMonster.Canvas.RenderContext]::new($profile, $theme, $null, 0, [timespan]::Zero)
        $map = [ConsoleMonster.Styling.ConsoleColorMap]::new($context)
        foreach ($stream in 'Warning', 'Verbose', 'Debug') {
            $style = $context.Style([ConsoleMonster.Styling.ThemeToken] $roles[$stream])
            # Stream preferences need concrete palette colours, including colours shared with text-like roles.
            $expected[$stream + 'ForegroundColor'] = $map.Background($style.Foreground)
            $background = $map.Background($style.Background)
            if ($null -eq $background) {
                try { $background = $Host.UI.RawUI.BackgroundColor } catch [System.NotSupportedException] { }
            }
            if ($null -ne $background) { $expected[$stream + 'BackgroundColor'] = $background }
        }
    }
    $expected
}

function Get-StreamsUnownedState {
    <# .SYNOPSIS
    Captures neighbouring preferences that Streams must leave with the caller.
    #>
    $state = @{ ErrorView = [string] $ErrorView }
    foreach ($property in $Host.PrivateData.PSObject.Properties) {
        if ($property.Name -notmatch 'Color$') { continue }
        if ($PSVersionTable.PSVersion -lt [version] '7.2' -and $property.Name -match '^(Warning|Verbose|Debug)') { continue }
        $state['Host.' + $property.Name] = $property.Value
    }
    if ($PSVersionTable.PSVersion -ge [version] '7.2') {
        foreach ($name in 'TableHeader', 'ErrorAccent', 'FormatAccent') { $state[$name] = $PSStyle.Formatting.$name }
        $state.OutputRendering = $PSStyle.OutputRendering
    }
    $state
}

function Assert-StreamsUnownedState {
    <# .SYNOPSIS
    Fails when a neighbouring native preference has been overwritten.
    #>
    param([hashtable] $Expected)
    $actual = Get-StreamsUnownedState
    foreach ($name in $Expected.Keys) {
        if ($actual[$name] -cne $Expected[$name]) { throw "Unowned preference changed: $name" }
    }
}

function Get-StreamsWarningOracle {
    <# .SYNOPSIS
    Captures native warning redirection, WarningVariable and preference termination together.
    #>
    param([string] $Preference, [string] $Path, [string] $Command = 'Write-Warning')
    $WarningPreference = $Preference
    $captured = @()
    $completed = $false
    $failure = $null
    $success = @()
    try {
        $success = @(& $Command -Message 'streams warning oracle' -WarningVariable captured 3> $Path)
        $completed = $true
        $success += 'native success'
    } catch {
        $failure = [pscustomobject]@{
            Type = $_.Exception.GetType().FullName
            Id = $_.FullyQualifiedErrorId
            Message = $_.Exception.Message
        }
    }
    [pscustomobject]@{
        Completed = $completed
        Success = $success -join '|'
        WarningTypes = @($captured | ForEach-Object { $_.GetType().FullName }) -join '|'
        WarningMessages = @($captured | ForEach-Object { $_.Message }) -join '|'
        WarningCount = @($captured).Count
        Failure = $failure
        FileBytes = if (Test-Path -LiteralPath $Path) { [Convert]::ToBase64String([IO.File]::ReadAllBytes($Path)) } else { '' }
    }
}

function Get-StreamsCaptureOracle {
    <# .SYNOPSIS
    Records native verbose, debug and information objects, InformationVariable and redirected bytes.
    #>
    param([string] $Directory)
    $VerbosePreference = 'Continue'
    $DebugPreference = 'Continue'
    $InformationPreference = 'Continue'
    $informationRecords = @()
    $verbose = @(Write-Verbose 'streams verbose oracle' 4>&1)
    $debug = @(Write-Debug 'streams debug oracle' 5>&1)
    $information = @(Write-Information 'streams information oracle' -Tags 'streams', 'native' -InformationVariable informationRecords 6>&1)
    $files = @{}
    $verbosePath = Join-Path $Directory 'verbose.txt'
    $debugPath = Join-Path $Directory 'debug.txt'
    $informationPath = Join-Path $Directory 'information.txt'
    Write-Verbose 'streams verbose oracle' 4> $verbosePath
    Write-Debug 'streams debug oracle' 5> $debugPath
    Write-Information 'streams information oracle' -Tags 'streams', 'native' 6> $informationPath
    foreach ($name in 'verbose', 'debug', 'information') {
        $files[$name] = [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $Directory ($name + '.txt'))))
    }
    [pscustomobject]@{
        VerboseType = $verbose[0].GetType().FullName
        VerboseCount = $verbose.Count
        VerboseMessage = $verbose[0].Message
        DebugType = $debug[0].GetType().FullName
        DebugCount = $debug.Count
        DebugMessage = $debug[0].Message
        InformationType = $information[0].GetType().FullName
        InformationCount = $information.Count
        InformationData = [string] $information[0].MessageData
        InformationTags = $information[0].Tags -join '|'
        InformationVariable = @($informationRecords | ForEach-Object { $_.MessageData }) -join '|'
        VerboseBytes = $files.verbose
        DebugBytes = $files.debug
        InformationBytes = $files.information
    }
}

function Assert-StreamsOracle {
    <# .SYNOPSIS
    Compares bounded native observations without comparing timestamps or invocation objects.
    #>
    param($Before, $After)
    $expected = $Before | ConvertTo-Json -Depth 8 -Compress
    $actual = $After | ConvertTo-Json -Depth 8 -Compress
    if ($actual -cne $expected) { throw "Native stream oracle changed.`nExpected: $expected`nActual: $actual" }
}
