param(
    [ValidateSet('Classic', 'Shell', 'Inline')][string] $Layout = 'Classic'
)
# A small script menu in the Classic layout: actions that ask questions in front of the menu, a settings form, a page
# and a run into a live dashboard that comes back with a result. Actions are plain scriptblocks run on the pipeline
# thread; $MenuContext offers Toast, Confirm, Ask, Run and ShowResult. Nothing is really backed up; the data is invented.
Import-Module ConsoleMonster

$backups = @(
    [pscustomobject] @{ Name = 'Nightly 16 Oct 23:00'; Size = '4.2 GB'; Kind = 'Full'; When = (Get-Date).AddHours(-11) }
    [pscustomobject] @{ Name = 'Nightly 15 Oct 23:00'; Size = '1.1 GB'; Kind = 'Incremental'; When = (Get-Date).AddHours(-35) }
    [pscustomobject] @{ Name = 'Weekly 13 Oct 02:00'; Size = '4.0 GB'; Kind = 'Full'; When = (Get-Date).AddDays(-4) }
)

$settings = New-ConsoleForm 'Settings' -NoReview -SubmitText 'Save' -Content {
    New-ConsoleField Destination 'Destination' -Type Path -Kind Folder -Default 'D:\Backups' -Help 'Where backups are written.'
    New-ConsoleField Keep 'Days to keep' -Type Number -Integer -Minimum 1 -Maximum 365 -Default 14
    New-ConsoleField Compress 'Compress' -Type Toggle -Default $true -Help 'Smaller files, slower backups.'
}

$menu = New-ConsoleMenu 'Backup helper' -Description 'Back up and restore the reports share.' -Content {
    New-ConsoleMenuItem 'Back up now' -Hotkey b -Description 'Copies changed files to the destination.' -Action {
        if (-not $MenuContext.Confirm('Start a backup of \\fs01\reports now?', $true)) { return }
        $steps = New-ConsoleStatusList -Item 'Scan files', 'Copy changes', 'Verify', 'Prune old backups'
        $dashboard = New-ConsoleDashboard -Title 'Backup' -Content { New-ConsolePanel -Title 'Steps' -Content { $steps } }
        # The run's script block returns the result screen the menu shows when the dashboard closes.
        $null = $MenuContext.Run($dashboard, {
            foreach ($step in 'Scan files', 'Copy changes', 'Verify', 'Prune old backups') {
                Update-Console -Key $step -Status Running -WidgetId 'status'
                Start-Sleep -Milliseconds 600
                Update-Console -Key $step -Status Pass -WidgetId 'status'
            }
            New-ConsoleMenuRunResult 'Backup finished' -Summary '1 284 files copied, 312 MB.' -Figure ([ordered] @{ files = '1 284'; copied = '312 MB' }) -Next @{
                'Show where the log is' = { $MenuContext.Toast('Log: D:\Backups\backup.log') }
            }
        })
    }
    New-ConsoleMenuItem 'Restore' -Hotkey r -Description 'Puts a backup back on the share.' -Action {
        $backup = Read-ConsoleSelect 'Backup to restore' -Choices $backups -Display Name -Badge Size -Describe Kind
        if (Read-ConsoleConfirm "Overwrite \\fs01\reports with '$($backup.Name)'?" -TypedConfirmation 'reports' -Description 'Files changed since then are lost.') {
            $MenuContext.Toast("Restored $($backup.Name)")
        }
    }
    New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings -OnSubmit { $MenuContext.Toast("Saved: keep $($Values.Keep) days") }
    New-ConsoleMenuItem 'History' -Hotkey h -Page { New-ConsoleTable -InputObject $backups -Property Name, Kind, Size -Border Minimal } -Detail { "$($backups.Count) backups" }
    New-ConsoleMenuItem -Separator
    New-ConsoleMenuItem -Exit
}

Show-ConsoleMenu $menu -Layout $Layout -AppName 'ConsoleMonster backup helper'
