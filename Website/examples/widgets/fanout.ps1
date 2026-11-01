Import-Module ConsoleMonster

New-ConsoleStatusList -Layout Strip -WidgetId migration -Content {
    @{ Key = 'Discover'; Status = 'Pass'; Detail = '2.1s' }
    @{ Key = 'Snapshot'; Status = 'Pass'; Detail = '12s' }
    @{ Key = 'Migrate'; Status = 'Running'; Detail = '6/12' }
    'Verify', 'Report'
    # Items of the running phase: failed and running ones stay visible, the rest fold into +N more.
    @{ ParentKey = 'Migrate'; Key = 'alice'; Status = 'Pass'; Detail = '1.1s' }
    @{ ParentKey = 'Migrate'; Key = 'bob'; Status = 'Pass'; Detail = '1.5s' }
    @{ ParentKey = 'Migrate'; Key = 'carol'; Status = 'Pass'; Detail = '1.9s' }
    @{ ParentKey = 'Migrate'; Key = 'dave'; Status = 'Pass'; Detail = '2.3s' }
    @{ ParentKey = 'Migrate'; Key = 'erin'; Status = 'Running'; Detail = '40%' }
    @{ ParentKey = 'Migrate'; Key = 'frank'; Status = 'Fail'; Detail = 'access denied' }
    'grace', 'heidi', 'ivan', 'judy', 'mallory', 'niaj' |
        ForEach-Object { @{ ParentKey = 'Migrate'; Key = $_ } }
} | Show-Console
