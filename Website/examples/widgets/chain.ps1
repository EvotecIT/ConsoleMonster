Import-Module ConsoleMonster

# One line per pipeline that folds to fit its column: neighbours of the current stage become +N earlier and +N more.
$stageNames = 'Fetch', 'Build', 'Test', 'Pack', 'Publish'
$runs = @(
    @{ Repository = 'ConsoleMonster'; Status = 'Pass', 'Pass', 'Running', 'Pending', 'Pending' }
    @{ Repository = 'PSPublishModule'; Status = 'Pass', 'Fail', 'Pending', 'Pending', 'Pending' }
    @{ Repository = 'DnsClientX'; Status = 'Pass', 'Pass', 'Pass', 'Pass', 'Pass' }
)
$runs | ForEach-Object {
    $run = $_
    [pscustomobject]@{
        Repository = $run.Repository
        Stages = New-ConsoleStatusList -Layout Chain -Content {
            for ($i = 0; $i -lt $stageNames.Count; $i++) {
                @{ Key = $stageNames[$i]; Status = $run.Status[$i] }
            }
        }
    }
} | Show-ConsoleTable
