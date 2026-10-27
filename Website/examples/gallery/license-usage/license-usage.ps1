# Compare purchased and assigned Microsoft 365 licenses and price the unused seats.
Import-Module -Name ConsoleMonster

# Data: a fictional tenant with illustrative monthly prices; no Graph or admin API is called.
$skus = @(
    [pscustomobject] @{ Sku = 'Microsoft 365 E3'; Purchased = 450; Assigned = 431; Price = 36 }
    [pscustomobject] @{ Sku = 'Microsoft 365 E5'; Purchased = 120; Assigned = 74; Price = 57 }
    [pscustomobject] @{ Sku = 'Office 365 E1'; Purchased = 300; Assigned = 288; Price = 10 }
    [pscustomobject] @{ Sku = 'Exchange Online Plan 1'; Purchased = 80; Assigned = 63; Price = 4 }
    [pscustomobject] @{ Sku = 'Power BI Pro'; Purchased = 150; Assigned = 121; Price = 14 }
    [pscustomobject] @{ Sku = 'Visio Plan 2'; Purchased = 40; Assigned = 12; Price = 15 }
    [pscustomobject] @{ Sku = 'Project Plan 3'; Purchased = 25; Assigned = 24; Price = 30 }
)

# Behaviour: 90% assigned is healthy, under 70% is waste worth reclaiming; waste is unused seats × price.
$usage = foreach ($sku in $skus) {
    $percent = [math]::Round(100 * $sku.Assigned / $sku.Purchased)
    $status = if ($percent -ge 90) { 'Pass' } elseif ($percent -ge 70) { 'Warn' } else { 'Fail' }
    $unused = $sku.Purchased - $sku.Assigned
    [pscustomobject] @{ Sku = $sku.Sku; Purchased = $sku.Purchased; Assigned = $sku.Assigned; Unused = $unused; 'Used %' = $percent; 'Waste $' = $unused * $sku.Price; Status = $status }
}
$reclaim = @($usage | Where-Object -Property Status -NE -Value 'Pass' | Sort-Object -Property 'Waste $' -Descending | Select-Object -First 3)
$unusedSeats = ($usage | Measure-Object -Property Unused -Sum).Sum
$monthlyWaste = ($usage | Measure-Object -Property 'Waste $' -Sum).Sum

# Layout: utilisation bars beside the biggest savings, then the full ledger.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Microsoft 365 licenses[/] [muted]· adatum.example.test · October 2026[/]'
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Assigned of purchased' -Content {
            $usage | ForEach-Object -Process { [pscustomobject] @{ Label = $_.Sku; Value = $_.'Used %'; Role = $_.Status } } |
                New-ConsoleChart -Type Bar -Maximum 100 -Unit '%'
        }
        New-ConsolePanel -Title 'Reclaim first' -Content {
            New-ConsoleStatusList -Content {
                foreach ($sku in $reclaim) {
                    [pscustomobject] @{ Label = $sku.Sku; Status = $sku.Status; Detail = ('{0} unused · ${1:N0}/mo' -f $sku.Unused, $sku.'Waste $') }
                }
            }
        }
    }
    New-ConsoleTable -InputObject $usage -Property Sku, Purchased, Assigned, Unused, 'Used %', 'Waste $', Status -Expand
    ('[warn]${0:N0} a month[/] pays for {1} unassigned seats. [muted]Reclaim before the renewal date.[/]' -f $monthlyWaste, $unusedSeats)
}

# Behaviour: print once; review the reclaim list with each license owner before removing seats.
$report | Show-Console
