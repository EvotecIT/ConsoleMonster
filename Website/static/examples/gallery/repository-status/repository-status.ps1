# Review several repositories without leaving the terminal.
param([switch] $StaticSnapshot, [ValidateRange(0, 0)][int] $Step = 0)
Import-Module ConsoleMonster

# Data: cached fictional git summaries; no git process or remote request runs here.
$repositories = @(
    [pscustomobject] @{ Repository = 'orbit-api'; Branch = 'feature/cache'; Modified = 3; Staged = 1; Ahead = 2; Behind = 0 }
    [pscustomobject] @{ Repository = 'orbit-cli'; Branch = 'main'; Modified = 0; Staged = 0; Ahead = 0; Behind = 4 }
    [pscustomobject] @{ Repository = 'orbit-docs'; Branch = 'docs/setup'; Modified = 2; Staged = 0; Ahead = 0; Behind = 0 }
    [pscustomobject] @{ Repository = 'orbit-tests'; Branch = 'main'; Modified = 0; Staged = 0; Ahead = 0; Behind = 0 }
)

# Layout: numeric columns keep local changes separate from branch divergence.
$report = New-ConsoleRows -Gap 1 {
    New-ConsoleRule '[bold]Repository workspace[/] [muted]· ./work[/]'
    New-ConsoleTable -InputObject $repositories -Property Repository, Branch, Modified, Staged, Ahead, Behind -Expand
    New-ConsolePanel -Title 'Suggested next steps' {
        New-ConsoleStatusList {
            New-ConsoleStatusItem 'orbit-api' -Status Info -Detail 'Review the staged diff before committing'
            New-ConsoleStatusItem 'orbit-cli' -Status Warn -Detail 'Working tree clean; inspect four incoming commits'
            New-ConsoleStatusItem 'orbit-docs' -Status Info -Detail 'Two local edits need review'
            New-ConsoleStatusItem 'orbit-tests' -Status Pass -Detail 'Clean and up to date'
        }
    }
}

# Behaviour: print once. Keep cached summaries fresh in a separate local collection step.
$report | Write-ConsoleWidget
