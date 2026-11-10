param(
    [ValidateSet('All', 'Table', 'Cards', 'Lanes', 'WriteColor', 'Padding', 'Status', 'Log')][string] $Part = 'All',
    [string] $LogFolder = [IO.Path]::GetTempPath()
)
# Rich text everywhere: multicolour cells and headers, cards and tree labels, coloured lane names in a live dashboard,
# Literal and rich text, padding, centring and gradients, an updating status line and log files.
# The data is synthetic.
Import-Module ConsoleMonster

function Show([string] $Name) { $Part -in 'All', $Name }

if (Show 'Table') {
    $checks = @(
        [pscustomobject] @{ Server = 'DC01'; Site = 'Warsaw'; Result = 'pass'; Failed = 0; Note = 'healthy' }
        [pscustomobject] @{ Server = 'DC02'; Site = 'Krakow'; Result = 'warn'; Failed = 0; Note = 'replication lag 42 min' }
        [pscustomobject] @{ Server = 'DC03'; Site = 'Berlin'; Result = 'fail'; Failed = 3; Note = 'no inbound replication for 4h' }
    )
    $checks | Show-ConsoleTable -Title (New-ConsoleText 'Forest health', ' · rich cells' -Color Accent, Muted -Decoration Bold) -Property @(
        @{ Name = 'Server'; Header = (New-ConsoleText 'Ser', 'ver' -Color Cyan, Magenta); Expression = { New-ConsoleText $_.Server.Substring(0, 2), $_.Server.Substring(2) -Color Cyan, Yellow } }
        @{ Name = 'Site'; Header = (New-ConsoleText 'Site' -Color Green) }
        @{ Name = 'Result'; Expression = {
                switch ($_.Result) {
                    'pass' { New-ConsoleText '√ ', 'PASS' -Role Pass -Decoration Bold }
                    'warn' { New-ConsoleText '▲ ', 'WARN' -Role Warn -Decoration Bold }
                    default { New-ConsoleText '× ', 'FAIL' -Role Fail -Decoration Bold }
                } } }
        @{ Name = 'Note'; Expression = { if ($_.Failed -gt 0) { New-ConsoleText $_.Note -Role Fail -Decoration Italic } else { New-ConsoleText $_.Note -Role Muted } } }
    )
}

if (Show 'Cards') {
    $certificate = @(
        [pscustomobject] @{ Name = 'mail.evotec.xyz'; Issuer = "Let's Encrypt R11"; Expires = (New-ConsoleText 'in 22 days' -Role Warn -Decoration Bold); Key = (New-ConsoleText 'ECDSA ', 'P-256' -Color Gray, Cyan) }
        [pscustomobject] @{ Name = 'autodiscover.evotec.xyz'; Issuer = "Let's Encrypt R11"; Expires = (New-ConsoleText 'in 8 days' -Role Fail -Decoration Bold); Key = (New-ConsoleText 'RSA ', '2048' -Color Gray, Yellow) }
    )
    $certificate | Show-ConsoleTable -Width 44 -Title (New-ConsoleText 'Certificates' -Color Cyan -Decoration Bold)
    New-ConsoleTree (New-ConsoleText 'ad.', 'evotec.xyz' -Color DarkGray, Cyan) -Content {
        [pscustomobject]@{ Label = (New-ConsoleText 'Site ', 'Warsaw' -Color DarkGray, Green); Status = 'Pass'; Children = @(& {
            [pscustomobject]@{ Label = (New-ConsoleText 'DC', '01' -Color Cyan, Yellow); Status = 'Pass'; Detail = 'healthy' }
            [pscustomobject]@{ Label = (New-ConsoleText 'DC', '02' -Color Cyan, Yellow); Status = 'Warn'; Detail = 'lag 42 min' }
        }) }
    } | Show-Console
}

if (Show 'Lanes') {
    $dashboard = New-ConsoleDashboard -Title (New-ConsoleText 'Patching', ' · rich lanes' -Color Accent, Muted -Decoration Bold) -Gap 1 -Content {
        New-ConsolePanel -Title (New-ConsoleText 'Servers ', '4 workers' -Color White, DarkGray) -Content { New-ConsoleLanes -WidgetId lanes }
        New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
    }
    Invoke-ConsoleDashboard $dashboard {
        $servers = 'SRV01', 'SRV02', 'SRV03', 'SRV04'
        foreach ($server in $servers) {
            Update-Console -Key $server -Label (New-ConsoleText $server.Substring(0, 3), $server.Substring(3) -Color Cyan, Yellow) -Total 6 -WidgetId 'lanes'
        }
        for ($step = 1; $step -le 6; $step++) {
            foreach ($server in $servers) {
                $result = if ($server -eq 'SRV03' -and $step -eq 4) { 'Fail' } elseif ($step -eq 5 -and $server -eq 'SRV02') { 'Warn' } else { 'Pass' }
                Update-Console -Key $server -Result $result -Detail (New-ConsoleText 'installing ', "KB50$step$($server.Substring(3))" -Color DarkGray, Green) -WidgetId 'lanes'
            }
            Write-ConsoleLogEntry -Message (New-ConsoleText "step $step ", 'done', ' on 4 servers' -Color White, Green, DarkGray)
            Start-Sleep -Milliseconds 900
        }
        foreach ($server in $servers) { Update-Console -Key $server -Complete -WidgetId 'lanes' }
    }
}

if (Show 'WriteColor') {
    Write-ConsoleText 'Text pieces with independent colours' -Role Muted
    Write-ConsoleText -Text 'Green ', 'red ', 'magenta ', 'and cyan' -Color Green, Red, Magenta, Cyan
    Write-ConsoleText '1. ', 'Option 1' -Color Yellow, Green -Indent 4
    Write-ConsoleText -Text 'My text', ' is ', 'colourful' -Color Yellow, Red, Green -BackgroundColor Green, Green, Yellow
}

if (Show 'Padding') {
    Write-ConsoleText ' MENU ' -Alignment Center -Width 40 -Color Cyan -Decoration Bold
    Write-ConsoleText '1.', ' Backup', ' (nightly)' -Color Yellow, White, DarkGray -Indent 4
    Write-ConsoleText '2.', ' Restore' -Color Yellow, White -Indent 4
    Write-ConsoleText '中文 ', '👍 ', 'emoji and CJK' -Color Magenta, White, Green -Alignment Right -Width 40
    Write-ConsoleText 'ConsoleMonster — rich text everywhere' -Gradient '#00c6ff', '#ff00aa' -Decoration Bold -Alignment Center -Width 60
    Write-ConsoleText '█████████████████████████████████████████' -Gradient Pass, Fail
    Write-ConsoleText '[info] ', "Path exists`n2. Line`n3. Line" -Color Yellow, White -ShowTimestamp -SplitLines "`n" -TimestampFormat 'HH:mm:ss'
    Write-ConsoleText 'A long message that wraps under the time stamp, so the left edge stays clean and readable even when narrow.' -ShowTimestamp -Wrap -Width 60 -TimestampFormat 'HH:mm:ss' -Color Gray
}

if (Show 'Status') {
    $files = 'config.json', 'reports.xlsx', 'Weekly-2026-41.pdf', 'archive.zip', 'users.csv'
    foreach ($file in $files) {
        Show-ConsoleStatusLine 'Copying ', $file -Color Gray, Cyan -Spinner -ShowElapsed -ShowTimestamp
        Start-Sleep -Milliseconds 1400
    }
    Show-ConsoleStatusLine 'Copied ', "$($files.Count) files" -Color Gray, Green -Status Pass -ShowElapsed -Complete
}

if (Show 'Log') {
    $path = Join-Path $LogFolder 'consolemonster-richtext.log'
    Write-ConsoleText 'Backup ', 'started' -Color White, Green -LogFile $path
    Write-ConsoleText 'Data disk: ', '91% full; consider cleanup' -Color Gray, Yellow -LogFile $path
    Write-ConsoleText 'Copy failed: ', 'backup storage is offline' -Color Gray, Red -LogFile $path
    Write-ConsoleText 'Plain-text log' -Role Accent -Decoration Bold
    Get-Content -LiteralPath $path -Encoding UTF8 | ForEach-Object { Write-ConsoleText '  ', $_ -Color DarkGray, Gray }
}
