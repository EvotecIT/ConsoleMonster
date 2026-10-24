# Turn a storage inventory into threshold bars and a directory breakdown.
param([switch] $StaticSnapshot, [ValidateRange(0, 0)][int] $Step = 0)
Import-Module ConsoleMonster

# Data: synthetic sizes in GiB and platform-neutral paths; no filesystem scan is performed.
$used = [ordered] @{ Workspace = 62; Cache = 88; Artifacts = 94; Archive = 35 }
$folders = [ordered] @{
    './workspace' = [ordered] @{ src = '12 GiB'; packages = '31 GiB'; fixtures = '19 GiB' }
    './cache' = [ordered] @{ dependencies = '63 GiB'; downloads = '25 GiB' }
    './artifacts' = [ordered] @{ current = '18 GiB'; history = '76 GiB' }
}

# Layout: bars answer where pressure is; the tree explains what occupies the space.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule '[bold]Storage review[/] [muted]· capacity planning[/]'
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Used / 100 GiB' -Content {
            New-ConsoleBarChart -InputObject $used -Maximum 100 -Unit '%' -Warning 80 -Critical 90
        }
        New-ConsolePanel -Title 'Largest folders' -Content {
            New-ConsoleTree -Title './' -InputObject $folders
        }
    }
    New-ConsolePanel -Title 'Next action' -Content {
        '[warn]Artifacts is at 94%.[/] Review the retention policy before the next build.'
        '[muted]Keep the current release; archive history older than 30 days.[/]'
    }
}

# Behaviour: print once. Review recommendations before connecting any cleanup action.
$report | Show-Console
