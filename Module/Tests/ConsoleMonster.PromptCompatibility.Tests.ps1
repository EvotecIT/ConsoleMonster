Describe 'Original prompt callable contracts' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:Module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
    }

    It 'preserves <Contract> before enable while enabled and after restoration' -ForEach @(
        @{ Contract = 'dynamic parameters'; Definition = @'
function global:prompt {
    [CmdletBinding()] param()
    dynamicparam {
        $attributes = [Collections.ObjectModel.Collection[Attribute]]::new()
        $attributes.Add([Management.Automation.ParameterAttribute]::new())
        $dictionary = [Management.Automation.RuntimeDefinedParameterDictionary]::new()
        $dictionary.Add('Extra', [Management.Automation.RuntimeDefinedParameter]::new('Extra', [string], $attributes))
        $dictionary
    }
    end { "DYNAMIC:$($PSBoundParameters['Extra'])" }
}
'@; Call = 'prompt -Extra VALUE'; Expected = 'DYNAMIC:VALUE' }
        @{ Contract = 'pipeline begin process end'; Definition = @'
function global:prompt {
    [CmdletBinding()] param([Parameter(ValueFromPipeline)] $Value)
    begin { 'BEGIN' }
    process { "PIPE:$Value" }
    end { 'END' }
}
'@; Call = '1,2 | prompt'; Expected = 'BEGIN|PIPE:1|PIPE:2|END' }
        @{ Contract = 'unbound pipeline input'; Definition = @'
function global:prompt { 'INPUT:' + (@($input) -join ',') }
'@; Call = '1,2 | prompt'; Expected = 'INPUT:1,2' }
        @{ Contract = 'scope-qualified pipeline input'; Definition = @'
function global:prompt { 'INPUT:' + (@($local:input) -join ',') }
'@; Call = '1,2 | prompt'; Expected = 'INPUT:1,2' }
    ) {
        $scriptText = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__'
__DEFINITION__
$before = @(__CALL__) -join '|'
if ($before -cne '__EXPECTED__') { throw "Invalid native fixture: $before" }
Enable-ConsoleFormatting -Feature Prompt -WarningAction SilentlyContinue
$during = @(__CALL__) -join '|'
if ($during -cne $before) { throw "Original prompt changed while enabled: $during" }
Disable-ConsoleFormatting
$after = @(__CALL__) -join '|'
if ($after -cne $before) { throw "Original prompt changed after restore: $after" }
'PROMPT_COMPATIBILITY_OK'
'@
        $scriptText = $scriptText.Replace('__MODULE__', $script:Module.Replace("'", "''")).Replace('__DEFINITION__', $Definition).Replace('__CALL__', $Call).Replace('__EXPECTED__', $Expected)
        $scriptText = "try {`n$scriptText`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }"
        $result = Invoke-TestChild -Runtime $script:Runtime -Script $scriptText
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'PROMPT_COMPATIBILITY_OK'
    }
}
