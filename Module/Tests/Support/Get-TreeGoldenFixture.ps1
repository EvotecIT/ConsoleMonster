function Get-TreeGoldenFixture {
    param([Parameter(Mandatory)][ValidateSet('service', 'process', 'file', 'record')][string] $Kind)
    switch ($Kind) {
        'service' { $value = [pscustomobject]@{ Name = 'ExampleSvc'; DisplayName = 'Example Service'; Status = 'Running'; CanStop = $true; Optional = $null }; $names = @('Name', 'DisplayName', 'Status', 'CanStop', 'Optional'); $type = 'ConsoleMonster.Golden.Service' }
        'process' { $value = [pscustomobject]@{ Name = 'worker'; Id = 42; CPU = 3; Optional = $null }; $names = @('Name', 'Id', 'CPU', 'Optional'); $type = 'ConsoleMonster.Golden.Process' }
        'file' { $value = [pscustomobject]@{ Name = 'report.txt'; Length = 1536; Extension = '.txt'; Optional = $null }; $names = @('Name', 'Length', 'Extension', 'Optional'); $type = 'ConsoleMonster.Golden.File' }
        'record' { $value = [pscustomobject]@{ Name = 'worker-01'; Status = 'Running'; Ready = $true; Empty = $null }; $names = @('Name', 'Status', 'Ready', 'Empty'); $type = 'ConsoleMonster.Golden.Record' }
    }
    $set = [System.Management.Automation.PSPropertySet]::new('DefaultDisplayPropertySet', [string[]] $names)
    $value | Add-Member MemberSet PSStandardMembers ([System.Management.Automation.PSMemberInfo[]] @($set))
    $value.PSObject.TypeNames.Insert(0, $type)
    return $value
}
