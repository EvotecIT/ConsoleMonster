<#
.SYNOPSIS
Compares original prompt error dispatch with the enabled fallback in either native runtime.
.DESCRIPTION
Uses real function calls and stream redirections. The same contracts also run with attached console
handles, where an escaped error could otherwise be hidden by a redirected test process.
.PARAMETER Route
Native error route to compare.
#>
function Assert-PromptFallback {
    param([ValidateSet('SimpleMerge', 'Merge', 'AllStreams', 'Suppress', 'File', 'ErrorVariable', 'AppendVariables', 'PreferenceContinue', 'PreferenceSilent', 'Stop', 'Throw', 'Recursive', 'Recording')][string] $Route)

    Disable-ConsoleFormatting -Feature Prompt
    $saved = ${function:global:prompt}
    $file = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName())
    $scope = $null
    try {
        function global:prompt {
            [CmdletBinding()]
            param([switch] $Recurse)
            $inputStatus = $?
            if ($Recurse) {
                cmd /c exit 11
                prompt
                "OUTER_EXIT:$global:LASTEXITCODE"
            } else {
                "INPUT:$inputStatus/$global:LASTEXITCODE"
                if ($script:promptFailureMode -eq 'Throw') {
                    throw 'SYNTHETIC_PROMPT_THROW'
                }
                if ($script:promptFailureMode -in 'Stop', 'PreferenceContinue', 'PreferenceSilent') {
                    Write-Error 'SYNTHETIC_PROMPT_ERROR'
                } else {
                    Write-Error 'SYNTHETIC_PROMPT_ERROR' -ErrorAction Continue
                }
                'PROMPT_TEXT'
            }
        }
        if ($Route -eq 'SimpleMerge') {
            function global:prompt {
                "INPUT:$?/$global:LASTEXITCODE"
                Write-Error 'SYNTHETIC_PROMPT_ERROR' -ErrorAction Continue
                'PROMPT_TEXT'
            }
        }
        $script:promptFailureMode = $Route
        if ($Route -eq 'PreferenceContinue') { $ErrorActionPreference = 'Continue' }
        if ($Route -eq 'PreferenceSilent') { $ErrorActionPreference = 'SilentlyContinue' }
        $preferences = $ErrorActionPreference, $WarningPreference, $VerbosePreference, $DebugPreference, $InformationPreference, $ProgressPreference
        if ($Route -eq 'Recording') {
            $previousTarget = [ConsoleMonster.Terminal]::Target
            $target = [ConsoleMonster.Output.RecordingTarget]::new([ConsoleMonster.Terminal]::GetCurrentProfile())
            $scope = [ConsoleMonster.Terminal]::Use($target)
        }
        $results = foreach ($enabled in $false, $true) {
            if ($enabled) {
                Enable-ConsoleFormatting -Feature Prompt
            }
            $Error.Clear()
            $observed = @()
            $caught = $null
            $variable = @()
            $outputVariable = @()
            if ($Route -eq 'AppendVariables') {
                $variable = @('existing-error')
                $outputVariable = @('existing-output')
            }
            # Every native call below sees the same failed input status and exit code.
            try {
                switch ($Route) {
                    'SimpleMerge' { cmd /c exit 7; $observed = @(prompt 2>&1) }
                    'Merge' { cmd /c exit 7; $observed = @(prompt 2>&1) }
                    'AllStreams' { cmd /c exit 7; $observed = @(prompt *>&1) }
                    'Suppress' { cmd /c exit 7; $observed = @(prompt 2>$null) }
                    'File' { cmd /c exit 7; $observed = @(prompt 2>$file) }
                    'ErrorVariable' { cmd /c exit 7; $observed = @(prompt -ErrorVariable variable 2>&1) }
                    'AppendVariables' { cmd /c exit 7; $observed = @(prompt -ErrorVariable +variable -OutVariable +outputVariable 2>&1) }
                    'PreferenceContinue' { cmd /c exit 7; $observed = @(prompt 2>&1) }
                    'PreferenceSilent' { cmd /c exit 7; $observed = @(prompt 2>&1) }
                    'Stop' { cmd /c exit 7; $observed = @(prompt -ErrorAction Stop 2>&1) }
                    'Throw' { cmd /c exit 7; $observed = @(prompt 2>&1) }
                    'Recursive' { cmd /c exit 7; $observed = @(prompt -Recurse 2>&1) }
                    'Recording' { cmd /c exit 7; $observed = @(prompt 2>&1) }
                }
            } catch {
                $caught = $_
            }
            $exitCode = $global:LASTEXITCODE
            $errorIds = @($Error | ForEach-Object { $_.FullyQualifiedErrorId })
            $items = @($observed | ForEach-Object {
                if ($_ -is [Management.Automation.ErrorRecord]) {
                    "ERROR:$($_.FullyQualifiedErrorId):$($_.CategoryInfo.Category):$($_.Exception.Message)"
                } else {
                    "TEXT:$_"
                }
            })
            $caughtId = if ($caught) { $caught.FullyQualifiedErrorId }
            $fileText = if ($Route -eq 'File') { Get-Content -LiteralPath $file -Raw }
            if ($Route -eq 'File' -and $fileText -notmatch 'SYNTHETIC_PROMPT_ERROR') { throw 'Native error file was empty.' }
            if ($Route -eq 'ErrorVariable' -and @($variable).Count -ne 1) { throw 'Native ErrorVariable capture changed.' }
            if ($Route -eq 'AppendVariables' -and (@($variable).Count -ne 2 -or @($outputVariable).Count -ne 3)) { throw 'Native append variable capture duplicated or lost records.' }
            if ($Route -eq 'PreferenceSilent' -and @($observed | Where-Object { $_ -is [Management.Automation.ErrorRecord] }).Count -ne 0) { throw 'Silent preference leaked an error.' }
            if ($Route -in 'Stop', 'Throw' -and $null -eq $caught) { throw 'Native terminating error did not reach catch.' }
            if ($Route -notin 'Stop', 'Throw' -and $null -ne $caught) { throw $caught }
            $expectedExitCode = if ($Route -eq 'Recursive' -and -not $enabled) { 11 } else { 7 }
            if ($exitCode -ne $expectedExitCode) { throw "Original prompt changed caller exit code on $Route." }
            [pscustomobject]@{
                Items = $items -join "`n"
                ErrorIds = $errorIds -join "`n"
                Caught = $caughtId
                Variable = @($variable | ForEach-Object { $_.FullyQualifiedErrorId }) -join "`n"
                OutputVariable = $outputVariable -join "`n"
            }
        }
        if (($results[0] | ConvertTo-Json -Compress) -cne ($results[1] | ConvertTo-Json -Compress)) {
            throw "Original prompt $Route changed: $($results | ConvertTo-Json -Compress)"
        }
        if ($Route -notin 'Stop', 'Throw') {
            $expectedExit = if ($Route -eq 'Recursive') { 11 } else { 7 }
            if ($results[1].Items -notmatch "TEXT:INPUT:False/$expectedExit") { throw 'Original input status was lost.' }
        }
        $after = $ErrorActionPreference, $WarningPreference, $VerbosePreference, $DebugPreference, $InformationPreference, $ProgressPreference
        if (($preferences -join '|') -cne ($after -join '|')) { throw 'Prompt changed preferences.' }
        if ($Route -eq 'Recording' -and $target.Output.Length -ne 0) { throw 'Native fallback wrote to recording target.' }
        $Route
    } finally {
        if ($null -ne $scope) {
            $scope.Dispose()
            if (-not [object]::ReferenceEquals($previousTarget, [ConsoleMonster.Terminal]::Target)) { throw 'Recording target was not restored.' }
        }
        Disable-ConsoleFormatting -Feature Prompt
        Set-Item Function:global:prompt $saved
        Remove-Variable promptFailureMode -Scope Script -ErrorAction Ignore
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
}
