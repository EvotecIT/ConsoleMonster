param($Action, $Body, $Saved)
if ($Action -eq 'Save') {
    $alias = Microsoft.PowerShell.Utility\Get-Alias -Name Out-GridView -Scope Global -ErrorAction Ignore
    $function = Microsoft.PowerShell.Management\Get-Item -LiteralPath Function:\Out-GridView -ErrorAction Ignore
    $savedAlias = if ($alias) {
        @{
            Definition = $alias.Definition
            Options = $alias.Options
            Description = $alias.Description
            Visibility = $alias.Visibility
        }
    }
    $savedFunction = if ($function) {
        @{
            ScriptBlock = $function.ScriptBlock
            Options = $function.Options
            Description = $function.Description
            Visibility = $function.Visibility
        }
    }
    @{
        Alias = $savedAlias
        'Function' = $savedFunction
    }
    return
}
if ($Action -eq 'Install') {
    $options = [System.Management.Automation.ScopedItemOptions]::None
    if ($Saved.Function) { $options = $Saved.Function.Options -band [System.Management.Automation.ScopedItemOptions]::AllScope }
    Microsoft.PowerShell.Management\Set-Item -LiteralPath Function:\global:Out-GridView -Value $Body -Options $options -Force -ErrorAction Stop
    return
}
if ($Action -eq 'Restore' -or $Action -eq 'Rollback') {
    $current = ${function:global:Out-GridView}
    if (($current -and $current.ToString() -ceq $Body.ToString()) -or $Action -eq 'Rollback') {
        if ($Saved.Function) {
            Microsoft.PowerShell.Management\Set-Item -LiteralPath Function:\global:Out-GridView -Value $Saved.Function.ScriptBlock -Options $Saved.Function.Options -Force -ErrorAction Stop
            $restored = Microsoft.PowerShell.Management\Get-Item -LiteralPath Function:\Out-GridView
            $restored.Description = $Saved.Function.Description
            $restored.Visibility = $Saved.Function.Visibility
        } else {
            Microsoft.PowerShell.Management\Remove-Item -LiteralPath Function:\Out-GridView -Force -ErrorAction Stop
        }
    }
}
