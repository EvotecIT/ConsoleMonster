Import-Module ConsoleMonster

# Each source keeps one colour; -Level shows warnings and worse, and hidden lines stay in the history.
New-ConsoleLogRegion -Height 5 -NoElapsed -Level Warn -Content {
    @{ Message = 'Snapshot of 12 mailboxes taken'; Tag = 'snapshot' }
    @{ Message = 'Mailbox for frank is over quota (51 GB)'; Level = 'Warn'; Tag = 'migrate' }
    @{ Message = 'alice moved in 1.1s'; Tag = 'migrate' }
    @{ Message = 'frank: access denied on the target database'; Level = 'Error'; Tag = 'migrate' }
    @{ Message = 'Throttling: 3 requests delayed'; Level = 'Warn'; Tag = 'exchange-online' }
} | Show-Console
