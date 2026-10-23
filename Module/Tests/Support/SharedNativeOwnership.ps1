function Test-SharedNativeOwnership {
    <# .SYNOPSIS
    Exercises real shared editor and PSStyle objects across two native PowerShell runspaces.
    #>
    param([string] $ModulePath, [string] $SupportPath, [string] $FirstRelease, [switch] $RemoveModules, [switch] $Reapply)
    $first = [powershell]::Create()
    $second = [powershell]::Create()
    function Invoke-OwnershipRunspace($shell, [string] $body) {
        $shell.Commands.Clear()
        $shell.Streams.Error.Clear()
        $result = $shell.AddScript($body).Invoke()
        if ($shell.HadErrors) { throw ($shell.Streams.Error | Out-String) }
        $result
    }
    $setup = @(
        "Import-Module '$($ModulePath.Replace("'", "''"))' -ErrorAction Stop"
        "Import-Module '$((Join-Path (Get-Module PSReadLine).ModuleBase 'PSReadLine.psd1').Replace("'", "''"))' -ErrorAction Stop"
        ". '$((Join-Path $SupportPath 'ReadLineHarness.ps1').Replace("'", "''"))'"
        ". '$((Join-Path $SupportPath 'StreamsHarness.ps1').Replace("'", "''"))'"
    ) -join "`n"
    try {
        $null = Invoke-OwnershipRunspace $first $setup
        $null = Invoke-OwnershipRunspace $second $setup
        $editorA = Invoke-OwnershipRunspace $first 'Get-PSReadLineOption'
        $editorB = Invoke-OwnershipRunspace $second 'Get-PSReadLineOption'
        if (-not [object]::ReferenceEquals($editorA.PSObject.BaseObject, $editorB.PSObject.BaseObject)) { throw 'The real PSReadLine options are not shared in this process.' }
        if ($PSVersionTable.PSVersion -ge [version] '7.2') {
            foreach ($owner in 'Formatting', 'Progress') {
                $objectA = Invoke-OwnershipRunspace $first "`$PSStyle.$owner"
                $objectB = Invoke-OwnershipRunspace $second "`$PSStyle.$owner"
                if (-not [object]::ReferenceEquals($objectA.PSObject.BaseObject, $objectB.PSObject.BaseObject)) { throw "The native $owner oracle is not shared." }
            }
        }
        $null = Invoke-OwnershipRunspace $first @'
Set-ReadLineBaseline
Set-StreamsBaseline
$global:editorBaseline = Get-ReadLineState
$global:streamsBaseline = Get-StreamsState
$global:unownedEditor = Get-ReadLineUnowned
Set-ConsoleTheme Light
Enable-ConsoleFormatting -Feature ReadLine, Streams
$global:editorDesired = Get-ReadLineState
$global:streamsDesired = Get-StreamsState
Assert-ReadLineTheme
Assert-StreamsState (Get-StreamsExpected)
'@
        $null = Invoke-OwnershipRunspace $second @'
Set-ConsoleTheme HighContrast
Enable-ConsoleFormatting -Feature ReadLine, Streams
$global:editorDesired = Get-ReadLineState
$global:streamsDesired = Get-StreamsState
Assert-ReadLineTheme
Assert-StreamsState (Get-StreamsExpected)
'@
        if ($Reapply) {
            $null = Invoke-OwnershipRunspace $first @'
Set-ReadLineBaseline -Variant 1
Set-StreamsBaseline -Variant 1
Set-ConsoleTheme Dark
Enable-ConsoleFormatting -Feature ReadLine, Streams
$global:editorDesired = Get-ReadLineState
$global:streamsDesired = Get-StreamsState
Assert-ReadLineTheme
Assert-StreamsState (Get-StreamsExpected)
'@
        }
        $released = if ($FirstRelease -eq 'A') { $first } else { $second }
        $survivor = if ($FirstRelease -eq 'A') { $second } else { $first }
        $release = if ($RemoveModules) { 'Remove-Module ConsoleMonster -ErrorAction Stop' } else { 'Disable-ConsoleFormatting -Feature ReadLine, Streams' }
        $null = Invoke-OwnershipRunspace $released $release
        $null = Invoke-OwnershipRunspace $survivor @'
Assert-NativeState $global:editorDesired (Get-ReadLineState)
Assert-StreamsState $global:streamsDesired
Set-ReadLineBaseline -Variant 2
Set-StreamsBaseline -Variant 2
if ($PSVersionTable.PSVersion -ge [version] '7.2') { $PSStyle.Progress.View = 'Classic' }
'@
        $null = Invoke-OwnershipRunspace $survivor $release
        $null = Invoke-OwnershipRunspace $first @'
Assert-NativeState $global:editorBaseline (Get-ReadLineState)
Assert-StreamsState $global:streamsBaseline
Assert-NativeState $global:unownedEditor (Get-ReadLineUnowned)
if ($PSVersionTable.PSVersion -ge [version] '7.2' -and $PSStyle.Progress.View -ne 'Classic') { throw 'A release changed the unowned progress View.' }
'@
        'SHARED_NATIVE_OWNERSHIP_OK'
    } finally {
        foreach ($shell in $first, $second) {
            try { $null = Invoke-OwnershipRunspace $shell 'if (Get-Module ConsoleMonster) { Disable-ConsoleFormatting; Remove-Module ConsoleMonster }' } finally { $shell.Dispose() }
        }
    }
}
