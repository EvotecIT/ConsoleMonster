# Trusted built-in definitions. Native Format-* still owns its selected columns and expressions.
@{
    TypeName = 'Microsoft.PowerShell.Commands.MemberDefinition'
    Group = 'Member'
    GroupBy = 'MemberType'
    Property = @(
        'Name'
        @{ Name = 'MemberType'; Expression = { [string] $_.MemberType } }
        @{ Name = 'Definition'; Expression = 'Definition'; Presenter = 'MemberSignature'; MaxLines = 0 }
    )
}
@{
    TypeName = @(
        'System.Management.Automation.CmdletInfo'
        'System.Management.Automation.FunctionInfo'
        'System.Management.Automation.FilterInfo'
        'System.Management.Automation.AliasInfo'
        'System.Management.Automation.ApplicationInfo'
        'System.Management.Automation.ExternalScriptInfo'
        'System.Management.Automation.ConfigurationInfo'
    )
    Group = 'Command'
    GroupBy = 'ModuleName'
    Property = @(
        @{ Name = 'CommandType'; Expression = { [string] $_.CommandType }; Presenter = 'Chip' }
        @{ Name = 'Name'; Expression = { if ($_.CommandType -eq 'Alias') { $_.DisplayName } else { $_.Name } } }
        @{ Name = 'Version'; Expression = { if ($_.Version -and $_.Version -ne [version] '0.0') { [string] $_.Version } } }
        'Source'
    )
}
@{
    TypeName = 'System.Management.Automation.PSModuleInfo'
    Group = 'Module'
    Property = @(
        @{ Name = 'ModuleType'; Expression = { [string] $_.ModuleType } }
        @{ Name = 'Version'; Expression = { [string] $_.Version } }
        'Name'
        @{ Name = 'Path'; Expression = 'Path'; Presenter = 'Link' }
        @{ Name = 'Commands'; Expression = { $_.ExportedCommands.Count }; Presenter = 'Identifier' }
        @{ Name = 'Variables'; Expression = { $_.ExportedVariables.Count }; Presenter = 'Identifier' }
    )
}
@{
    TypeName = 'Microsoft.PowerShell.Commands.HistoryInfo'
    Group = 'History'
    Property = @(
        @{ Name = 'Id'; Expression = 'Id'; Presenter = 'Identifier' }
        @{ Name = 'Duration'; Expression = { $_.EndExecutionTime - $_.StartExecutionTime }; Presenter = 'Duration' }
        @{ Name = 'Status'; Expression = { [string] $_.ExecutionStatus }; Presenter = 'StatusWord' }
        @{ Name = 'CommandLine'; Expression = 'CommandLine'; Highlight = 'PowerShell' }
    )
}
