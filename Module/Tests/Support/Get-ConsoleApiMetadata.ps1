function Get-ConsoleApiTypeName {
    param([Parameter(Mandatory = $true)][Type] $Type)
    if ($Type.IsByRef) { return (Get-ConsoleApiTypeName $Type.GetElementType()) + '&' }
    if ($Type.IsArray) { return (Get-ConsoleApiTypeName $Type.GetElementType()) + '[' + (',' * ($Type.GetArrayRank() - 1)) + ']' }
    if ($Type.IsGenericParameter) { return $Type.Name }
    if ($Type.IsGenericType) {
        $arguments = @($Type.GetGenericArguments() | ForEach-Object { Get-ConsoleApiTypeName $_ })
        return $Type.GetGenericTypeDefinition().FullName + '[' + ($arguments -join ',') + ']'
    }
    return $Type.FullName
}

function Get-ConsoleApiParameters {
    param([Parameter(Mandatory = $true)][System.Management.Automation.CmdletInfo] $Command)
    $instance = [Activator]::CreateInstance($Command.ImplementingType)
    foreach ($entry in ($Command.Parameters.GetEnumerator() | Sort-Object Key)) {
        $property = $Command.ImplementingType.GetProperty($entry.Key)
        if ($null -eq $property -or $property.DeclaringType.Namespace -notlike 'ConsoleMonster*') { continue }
        $value = $property.GetValue($instance, $null)
        if ($value -is [System.Management.Automation.SwitchParameter]) { $value = $value.IsPresent }
        elseif ($null -ne $value -and $value.GetType().IsEnum) { $value = $value.ToString() }
        elseif ($null -ne $value -and $value -isnot [string] -and $value -isnot [ValueType]) { $value = $null }
        [pscustomobject]@{
            Name = $entry.Key
            Type = Get-ConsoleApiTypeName $property.PropertyType
            Default = $value
            Sets = @($entry.Value.Attributes | Where-Object { $_ -is [System.Management.Automation.ParameterAttribute] } | ForEach-Object {
                [pscustomobject]@{ Name = $_.ParameterSetName; Position = $_.Position; Mandatory = $_.Mandatory }
            })
        }
    }
}

function Get-ConsoleApiReferences {
    param([Type] $Type, [hashtable] $ApiTypes)
    if ($null -eq $Type -or $Type.IsGenericParameter -or [Delegate].IsAssignableFrom($Type)) { return }
    if ($Type.IsArray -or $Type.IsByRef) { Get-ConsoleApiReferences $Type.GetElementType() $ApiTypes; return }
    $definition = if ($Type.IsGenericType) { $Type.GetGenericTypeDefinition() } else { $Type }
    if ($ApiTypes.ContainsKey($definition.FullName)) { $definition }
    if ($Type.IsGenericType) {
        foreach ($argument in $Type.GetGenericArguments()) { Get-ConsoleApiReferences $argument $ApiTypes }
    }
}

function Get-ConsoleApiTypeGraph {
    param([Type[]] $Roots, [hashtable] $ApiTypes, [switch] $IncludeItemMethods)
    $result = @{}
    $pending = [Collections.Generic.Queue[Type]]::new()
    foreach ($type in $Roots) { $pending.Enqueue($type) }
    while ($pending.Count -gt 0) {
        $type = $pending.Dequeue()
        if ($result.ContainsKey($type.FullName)) { continue }
        $result[$type.FullName] = $type
        foreach ($property in $type.GetProperties()) {
            if (-not $IncludeItemMethods -and $property.GetMethod.IsStatic) { continue }
            foreach ($next in @(Get-ConsoleApiReferences $property.PropertyType $ApiTypes)) { $pending.Enqueue($next) }
        }
        if ($IncludeItemMethods) {
            foreach ($method in $type.GetMethods([Reflection.BindingFlags]'Public,Instance,Static,DeclaredOnly')) {
                if ($method.IsSpecialName -or ($method.Name -notlike 'Add*' -and $method.Name -notlike 'Set*')) { continue }
                foreach ($parameter in $method.GetParameters()) {
                    foreach ($next in @(Get-ConsoleApiReferences $parameter.ParameterType $ApiTypes)) { $pending.Enqueue($next) }
                }
            }
        }
    }
    return $result
}

function Test-ConsoleApiConfigurationProperty {
    param([Reflection.PropertyInfo] $Property)
    if ($Property.GetIndexParameters().Count -gt 0) { return $false }
    if ($null -ne $Property.GetSetMethod($false)) { return $true }
    $name = Get-ConsoleApiTypeName $Property.PropertyType
    foreach ($prefix in 'System.Collections.Generic.IList`', 'System.Collections.Generic.IDictionary`', 'System.Collections.Generic.ICollection`', 'System.Collections.Generic.List`', 'System.Collections.Generic.Dictionary`', 'System.Collections.IList', 'System.Collections.IDictionary') {
        if ($name.StartsWith($prefix, [StringComparison]::Ordinal)) { return $true }
    }
    return $false
}
