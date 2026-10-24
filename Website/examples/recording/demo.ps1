# demo: setup
. {
    function global:prompt { 'PS demo> ' }
    $env:CONSOLEMONSTER_MODE = 'interactive'
    $env:CONSOLEMONSTER_COLOR = 'truecolor'
    $env:CONSOLEMONSTER_GLYPHS = 'full'
    $env:CI = $null
    $env:NO_COLOR = $null
    $demo = if ($env:CM_DEMO_ROOT) { $env:CM_DEMO_ROOT } else { Join-Path ([IO.Path]::GetTempPath()) ('ConsoleMonsterTalk-' + [guid]::NewGuid().ToString('N') + '/demo') }
    $null = New-Item -ItemType Directory -Path $demo -Force
    $null = New-PSDrive -Name demo -PSProvider FileSystem -Root $demo -Scope Global
    Set-Location demo:
    $env:GIT_PAGER = 'cat'
    $env:GIT_AUTHOR_DATE = '2026-01-01T12:00:00Z'
    $env:GIT_COMMITTER_DATE = $env:GIT_AUTHOR_DATE
    $env:API_TOKEN = 'demo-token'
    git init --quiet --initial-branch main
    foreach ($subject in @('Start sample app', 'Add synthetic fixtures', 'Render report table', 'Track build progress', 'Document local recording')) {
        Add-Content (Join-Path $demo 'history.txt') $subject
        git add history.txt
        git -c user.name='Demo' -c user.email='demo@example.invalid' -c commit.gpgsign=false commit --quiet -m $subject
        if ($LASTEXITCODE) { throw 'Cannot prepare the synthetic Git repository.' }
    }
    function global:Show-DemoReport {
        Write-Host 'Build report' -ForegroundColor Cyan
        @(
            [pscustomobject]@{ Check = 'Compile'; Result = 'Passed'; Duration = '1.8 s' }
            [pscustomobject]@{ Check = 'Unit tests'; Result = '24 passed'; Duration = '2.4 s' }
            [pscustomobject]@{ Check = 'Package'; Result = 'Ready'; Duration = '0.6 s' }
        ) | Show-ConsoleTable -Width 70
    }
    function global:Invoke-DemoBuild {
        Use-ConsoleProgress {
            foreach ($step in 1..10) {
                Write-Progress -Activity 'Building sample app' -Status "Step $step of 10" -PercentComplete ($step * 10)
                Start-Sleep -Milliseconds 100
            }
            Write-Progress -Activity 'Building sample app' -Completed
        }
        Write-Host 'Build succeeded. 24 tests passed.' -ForegroundColor Green
    }
    function global:Show-DemoToken {
        param([string] $Value)
        Write-Host ("API_TOKEN={0} (synthetic)" -f $Value) -ForegroundColor DarkGray
    }
}
# demo: wpm 180
# demo: chapter Inspect
git log --oneline --graph --color -5
# demo: pause 1
# demo: chapter Build
Invoke-DemoBuild
# demo: wait Build succeeded
# demo: pause 1
# demo: chapter Report
Show-DemoReport
# demo: pause 1
# demo: chapter Errors and configuration
Get-Item ./missing.json 2>&1 | Write-ConsoleText -Color Red
Show-DemoToken $env:API_TOKEN
# demo: pause 2
