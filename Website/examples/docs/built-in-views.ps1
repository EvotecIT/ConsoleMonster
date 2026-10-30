<#
.SYNOPSIS
Shows everyday PowerShell views using disposable, synthetic examples.
.PARAMETER Surface
The built-in command to demonstrate.
.PARAMETER Native
Shows PowerShell's native display for a before-and-after comparison.
.EXAMPLE
./built-in-views.ps1 -Surface Member
#>
[CmdletBinding()]
param(
    [ValidateSet('Member', 'Command', 'Module', 'History')]
    [string] $Surface = 'Member',

    [switch] $Native
)

Import-Module ConsoleMonster
if (-not $Native) {
    Enable-ConsoleFormatting -Include Output, FormatCommands
}

switch ($Surface) {
    'Member' {
        'synthetic' | Get-Member -Name Length, Substring | Out-Default
        $worker = New-Object -TypeName System.ComponentModel.BackgroundWorker
        try {
            $worker | Get-Member -Name DoWork, IsBusy, RunWorkerAsync | Out-Default
        } finally {
            $worker.Dispose()
        }
    }
    'Command' {
        $module = New-Module -Name Demo.Inventory -ScriptBlock {
            function Get-DemoItem { 'synthetic' }
            function Test-DemoItem { $true }
            Set-Alias gdi Get-DemoItem
            Export-ModuleMember -Function Get-DemoItem, Test-DemoItem -Alias gdi
        }
        Import-Module $module
        Get-Command -Module Demo.Inventory -CommandType Alias, Function | Out-Default
    }
    'Module' {
        $module = New-Module -Name Demo.Inventory -ScriptBlock {
            function Get-DemoItem { 'synthetic' }
            Export-ModuleMember -Function Get-DemoItem
        }
        Import-Module $module
        # Give this in-memory demo an example version and path; no file is created or opened.
        Get-Module Demo.Inventory | Add-Member -NotePropertyMembers @{
            Path = (Join-Path ([IO.Path]::GetTempPath()) 'Demo/Demo.Inventory.psm1')
            Version = [version]'1.2.3'
        } -Force -PassThru | Out-Default
    }
    'History' {
        $start = [datetime]'2026-10-22T10:00:00'
        Add-History -InputObject ([pscustomobject]@{
            CommandLine = 'Get-DemoItem -Name "synthetic"'
            ExecutionStatus = 'Completed'
            StartExecutionTime = $start
            EndExecutionTime = $start.AddMilliseconds(380)
        })
        Add-History -InputObject ([pscustomobject]@{
            CommandLine = 'Test-DemoItem -Verbose'
            ExecutionStatus = 'Failed'
            StartExecutionTime = $start.AddSeconds(1)
            EndExecutionTime = $start.AddSeconds(3)
        })
        Get-History -Count 2 | Out-Default
    }
}
