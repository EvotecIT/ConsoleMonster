Describe 'ConsoleMonster PowerShell property binding' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = (Resolve-Path (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1')).Path
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        function Invoke-BindingChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                '$ErrorActionPreference = "Stop"'
                "Import-Module '$($script:ModulePath)' -Force -ErrorAction Stop"
                $Script
            )
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            if ($LASTEXITCODE -ne 0) {
                throw (($output | ForEach-Object { "$_" }) -join "`n")
            }
            $json = @($output | ForEach-Object { "$_" } | Where-Object { $_ -match '^BINDING:' } | Select-Object -Last 1)
            if ($json.Count -ne 1) {
                throw (($output | ForEach-Object { "$_" }) -join "`n")
            }
            $json[0].Substring(8) | ConvertFrom-Json
        }
    }

    It 'matches wildcard properties in first appearance order without duplicates' {
        $result = Invoke-BindingChild @'
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject([pscustomobject]@{ Name = 'a'; Count = 1 }))
$items.Add([System.Management.Automation.PSObject]::AsPSObject([pscustomobject]@{ Name = 'b'; Color = 'blue'; Count = 2 }))
$columns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, [object[]]@('N*', '*ount', 'Name'))
'BINDING:' + (@($columns | ForEach-Object Name) | ConvertTo-Json -Compress)
'@
        $result | Should -Be @('Name', 'Count')
    }

    It 'evaluates calculated ScriptBlock and string expressions' {
        $result = Invoke-BindingChild @'
$item = [pscustomobject]@{ Name = 'Ada'; Score = 42 }
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject($item))
$props = [object[]]@(
    @{ Name = 'Upper'; Expression = { $_.Name.ToUpperInvariant() } },
    @{ Name = 'Points'; Expression = 'Score' }
)
$columns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, $props)
'BINDING:' + (@($columns | ForEach-Object { $_.GetValue($item) }) | ConvertTo-Json -Compress)
'@
        $result | Should -Be @('ADA', 42)
    }

    It 'maps column options including presenter unit and tone rules' {
        $result = Invoke-BindingChild @'
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject([pscustomobject]@{ Score = 95 }))
$columns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, [object[]]@(@{
    Name = 'Score'; Header = 'Health'; Overflow = 'TruncateMiddle'; MaxLines = 2; Priority = 7; Key = $true
    MinWidth = 4; MaxWidth = 20; Width = 12; Alignment = 'Right'; Presenter = 'Number'; Unit = '%'
    Aggregate = 'Average'; Flexible = $true; Weight = 3; FailAbove = 90; Role = @{ Above = 80; Role = 'Warn' }
}))
$c = $columns[0].Column
'BINDING:' + (@{ Header = $c.Header; Overflow = $c.Overflow.ToString(); MaxLines = $c.MaxLines; Priority = $c.Priority; Key = $c.IsKey; Min = $c.MinWidth; Max = $c.MaxWidth; Width = $c.Width; Alignment = $c.Alignment.ToString(); Presenter = $c.Presenter.GetType().Name; Aggregate = $c.Aggregate.ToString(); Flexible = $c.Flexible; Weight = $c.Weight; ToneCount = $c.RoleRules.Count } | ConvertTo-Json -Compress)
'@
        $result.Header | Should -Be 'Health'
        $result.Overflow | Should -Be 'TruncateMiddle'
        $result.MaxLines | Should -Be 2
        $result.Priority | Should -Be 7
        $result.Key | Should -BeTrue
        $result.Min | Should -Be 4
        $result.Max | Should -Be 20
        $result.Width | Should -Be 12
        $result.Alignment | Should -Be 'Right'
        $result.Presenter | Should -Be 'NumberPresenter'
        $result.Aggregate | Should -Be 'Average'
        $result.Flexible | Should -BeTrue
        $result.Weight | Should -Be 3
        $result.ToneCount | Should -Be 2
    }

    It 'uses the supplied C# presenter instance in a table column' {
        $result = Invoke-BindingChild @'
$presenterType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Presenters.NumberPresenter')
$presenter = $presenterType::new('%')
$table = [pscustomobject] @{ Score = 95 } | New-ConsoleTable -Property @(@{ Name = 'Score'; Presenter = $presenter })
'BINDING:' + (@{ Same = [object]::ReferenceEquals($presenter, $table.Columns[0].Presenter); Text = ($table | Export-ConsoleFrame -Format Text) } | ConvertTo-Json -Compress)
'@
        $result.Same | Should -BeTrue
        $result.Text | Should -Match '95 %'
    }

    It 'rejects unknown calculated option keys with valid key names' {
        $result = Invoke-BindingChild @'
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject([pscustomobject]@{ A = 1 }))
try {
    ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, [object[]]@(@{ Name = 'A'; Bogus = 1 })) | Out-Null
    throw 'expected binder failure'
}
catch {
    'BINDING:' + ($_.Exception.Message | ConvertTo-Json -Compress)
}
'@
        $result | Should -Match "Unknown calculated property key 'Bogus'"
        $result | Should -Match 'Overflow'
    }

    It 'defaults the first column to key and records calculated expression failures' {
        $result = Invoke-BindingChild @'
$item = [pscustomobject]@{ Score = 9; Team = 'red' }
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject($item))
$columns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, [object[]]@(@{ Name = 'Score' }, @{ Name = 'Team'; Key = $true }))
$broken = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, [object[]]@(@{ Name = 'Broken'; Expression = { throw 'evaluation failed' } }))
$value = $broken[0].GetValue($items[0])
'BINDING:' + (@{ Keys = @($columns | ForEach-Object { $_.Column.IsKey }); Value = $value; Error = $broken[0].LastError } | ConvertTo-Json -Compress)
'@
        $result.Keys | Should -Be @($false, $true)
        # A failing expression is a warning marker in its cell (FieldError), never a silently empty value.
        $result.Value.Message | Should -Match 'evaluation failed'
        $result.Error | Should -Match 'evaluation failed'
    }

    It 'uses DefaultDisplayPropertySet and returns scalar items as Value' {
        $result = Invoke-BindingChild @'
$p = [pscustomobject]@{ First = 1; Second = 2; Extra = 3 }
$p.PSObject.TypeNames.Insert(0, 'ConsoleMonster.Binding.DisplaySample')
Update-TypeData -TypeName ConsoleMonster.Binding.DisplaySample -DefaultDisplayPropertySet @('Second', 'First') -Force
$wrapped = [System.Management.Automation.PSObject]::AsPSObject($p)
$objects = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$objects.Add($wrapped)
$columns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($objects, $null)
$scalars = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$scalars.Add([System.Management.Automation.PSObject]::AsPSObject(1))
$scalars.Add([System.Management.Automation.PSObject]::AsPSObject(2))
$scalars.Add([System.Management.Automation.PSObject]::AsPSObject(3))
$scalarColumns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($scalars, $null)
'BINDING:' + (@{ Names = @($columns | ForEach-Object Name); ScalarName = $scalarColumns[0].Name; ScalarValue = $scalarColumns[0].GetValue([System.Management.Automation.PSObject]::AsPSObject(2)) } | ConvertTo-Json -Compress)
'@
        $result.Names | Should -Be @('Second', 'First')
        $result.ScalarName | Should -Be 'Value'
        $result.ScalarValue | Should -Be 2
    }

    It 'keeps scalar values and later object properties in a mixed default selection' {
        $result = Invoke-BindingChild @'
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject(7))
$item = [pscustomobject]@{ Name = 'Ada'; Extra = 'included by the property union' }
$item.PSObject.TypeNames.Insert(0, 'ConsoleMonster.Binding.MixedScalarFirstSample')
Update-TypeData -TypeName ConsoleMonster.Binding.MixedScalarFirstSample -DefaultDisplayPropertySet @('Name') -Force
$items.Add([System.Management.Automation.PSObject]::AsPSObject($item))
$columns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, $null)
$objectName = $columns | Where-Object Name -eq 'Name' | ForEach-Object { $_.GetValue($items[1]) }
'BINDING:' + (@{ Names = @($columns | ForEach-Object Name); Scalar = $columns[0].GetValue($items[0]); ObjectName = $objectName } | ConvertTo-Json -Compress)
'@
        # The first record's default display set applies whatever comes before it (the same columns as when the record is first).
        $result.Names | Should -Be @('Value', 'Name')
        $result.Scalar | Should -Be 7
        $result.ObjectName | Should -Be 'Ada'
    }

    It 'keeps scalar values when the first record has a DefaultDisplayPropertySet' {
        $result = Invoke-BindingChild @'
$record = [pscustomobject]@{ Name = 'Ada'; Extra = 'ignored by the display set' }
$record.PSObject.TypeNames.Insert(0, 'ConsoleMonster.Binding.MixedDisplaySample')
Update-TypeData -TypeName ConsoleMonster.Binding.MixedDisplaySample -DefaultDisplayPropertySet @('Name') -Force
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject($record))
$items.Add([System.Management.Automation.PSObject]::AsPSObject(8))
$columns = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PropertyBinder'))::Bind($items, $null)
$objectName = $columns | Where-Object Name -eq 'Name' | ForEach-Object { $_.GetValue($items[0]) }
'BINDING:' + (@{ Names = @($columns | ForEach-Object Name); Scalar = $columns[0].GetValue($items[1]); ObjectName = $objectName } | ConvertTo-Json -Compress)
'@
        $result.Names | Should -Be @('Value', 'Name')
        $result.Scalar | Should -Be 8
        $result.ObjectName | Should -Be 'Ada'
    }

    It 'groups by property and ScriptBlock preserving first appearance' {
        $result = Invoke-BindingChild @'
$items = [System.Collections.Generic.List[System.Management.Automation.PSObject]]::new()
$items.Add([System.Management.Automation.PSObject]::AsPSObject([pscustomobject]@{ Name = 'a'; Team = 'red'; Score = 2 }))
$items.Add([System.Management.Automation.PSObject]::AsPSObject([pscustomobject]@{ Name = 'b'; Team = 'blue'; Score = 3 }))
$items.Add([System.Management.Automation.PSObject]::AsPSObject([pscustomobject]@{ Name = 'c'; Team = 'red'; Score = 4 }))
$byName = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.GroupBinder'))::Group($items, 'Team')
$byScript = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.GroupBinder'))::Group($items, {
    if ($_.Score -gt 2) {
        'high'
    }
    else {
        'low'
    }
})
'BINDING:' + (@{ Teams = @($byName | ForEach-Object Key); Counts = @($byName | ForEach-Object { $_.Value.Count }); Levels = @($byScript | ForEach-Object Key) } | ConvertTo-Json -Compress)
'@
        $result.Teams | Should -Be @('red', 'blue')
        $result.Counts | Should -Be @(2, 1)
        $result.Levels | Should -Be @('low', 'high')
    }

    It 'inspects a nested PSCustomObject through PSObjectMemberSource' {
        $result = Invoke-BindingChild @'
$item = [pscustomobject]@{ Name = 'root'; Child = [pscustomobject]@{ Value = 17 } }
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.InspectorOptions'))::new()
$options.UseReflection = $false
$options.Sources.Add([Activator]::CreateInstance((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PSObjectMemberSource'), $true))
$tree = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.ObjectInspector'))::Inspect($item, $options)
$child = $tree.Members | Where-Object Name -eq 'Child'
'BINDING:' + (@{ TypeName = $tree.TypeName; ChildType = $child.TypeName; Field = $child.Members[0].Name; Value = $child.Members[0].Value } | ConvertTo-Json -Compress)
'@
        $result.TypeName | Should -Be 'PSCustomObject'
        $result.ChildType | Should -Be 'PSCustomObject'
        $result.Field | Should -Be 'Value'
        $result.Value | Should -Be 17
    }

    It 'unwraps wrapped hashtables before reading their keys' {
        $result = Invoke-BindingChild @'
$value = @{ Name = 'root'; Child = @{ A = 1 } }
$wrapped = [System.Management.Automation.PSObject]::AsPSObject($value)
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.InspectorOptions'))::new()
$options.UseReflection = $false
$options.Sources.Add([Activator]::CreateInstance((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PSObjectMemberSource'), $true))
$options.Unwrap = [System.Delegate]::CreateDelegate(
    [System.Func[object, object]],
    ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PSObjectMemberSource')).GetMethod('UnwrapPSObject', [Reflection.BindingFlags]'Static,Public,NonPublic')
)
$tree = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.ObjectInspector'))::Inspect($wrapped, $options)
$child = $tree.Members | Where-Object Name -eq 'Child'
'BINDING:' + (@{
    TypeName = $tree.TypeName
    Names = @($tree.Members | ForEach-Object Name | Sort-Object)
    ChildNames = @($child.Members | ForEach-Object Name)
    ChildValue = $child.Members[0].Value
    HasAdaptedMembers = [bool]($tree.Members | Where-Object Name -in @('Keys', 'Values', 'SyncRoot'))
} | ConvertTo-Json -Compress)
'@
        $result.TypeName | Should -Be 'Hashtable'
        $result.Names | Should -Be @('Child', 'Name')
        $result.ChildNames | Should -Be @('A')
        $result.ChildValue | Should -Be 1
        $result.HasAdaptedMembers | Should -BeFalse
    }

    It 'detects a self-referencing wrapped hashtable after unwrapping' {
        $result = Invoke-BindingChild @'
$value = @{}
$value.Self = $value
$wrapped = [System.Management.Automation.PSObject]::AsPSObject($value)
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.InspectorOptions'))::new()
$options.UseReflection = $false
$options.Sources.Add([Activator]::CreateInstance((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PSObjectMemberSource'), $true))
$options.Unwrap = [System.Delegate]::CreateDelegate(
    [System.Func[object, object]],
    ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PSObjectMemberSource')).GetMethod('UnwrapPSObject', [Reflection.BindingFlags]'Static,Public,NonPublic')
)
$tree = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.ObjectInspector'))::Inspect($wrapped, $options)
$cycle = $tree.Members[0]
'BINDING:' + (@{ Kind = $cycle.Kind.ToString(); Path = $cycle.Path; TargetPath = $cycle.TargetPath } | ConvertTo-Json -Compress)
'@
        $result.Kind | Should -Be 'Cycle'
        $result.Path | Should -Be '$.Self'
        $result.TargetPath | Should -Be '$'
    }

    It 'unwraps ArrayList values and infers PSCustomObject columns' {
        $result = Invoke-BindingChild @'
$values = [System.Collections.ArrayList]::new()
[void]$values.Add([pscustomobject]@{ Name = 'first'; Score = 1 })
[void]$values.Add([pscustomobject]@{ Name = 'second'; Score = 2 })
$wrapped = [System.Management.Automation.PSObject]::AsPSObject($values)
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.InspectorOptions'))::new()
$options.UseReflection = $false
$options.Sources.Add([Activator]::CreateInstance((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PSObjectMemberSource'), $true))
$options.Unwrap = [System.Delegate]::CreateDelegate(
    [System.Func[object, object]],
    ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.PSObjectMemberSource')).GetMethod('UnwrapPSObject', [Reflection.BindingFlags]'Static,Public,NonPublic')
)
$tree = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Data.ObjectInspector'))::Inspect($wrapped, $options)
'BINDING:' + (@{
    Kind = $tree.Kind.ToString()
    Columns = @($tree.Columns)
    ItemKinds = @($tree.Items | ForEach-Object { $_.Kind.ToString() })
    FirstName = $tree.Items[0].Members[0].Value
} | ConvertTo-Json -Compress)
'@
        $result.Kind | Should -Be 'Collection'
        $result.Columns | Should -Be @('Name', 'Score')
        $result.ItemKinds | Should -Be @('Object', 'Object')
        $result.FirstName | Should -Be 'first'
    }
}
