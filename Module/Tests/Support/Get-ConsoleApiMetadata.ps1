function Get-ConsoleApiFluentTypeName {
    param([string] $TypeName)
    $namespace = $TypeName.Substring(0, $TypeName.LastIndexOf('.'))
    $name = $namespace.Substring($namespace.LastIndexOf('.') + 1)
    return "$namespace.$($name)Fluent"
}

function Get-ConsoleApiAssemblies {
    param([Parameter(Mandatory = $true)][Reflection.Assembly] $ModuleAssembly, [string] $RepositoryRoot, [switch] $IncludeIntegrations)
    $directory = Split-Path $ModuleAssembly.Location
    $framework = Split-Path $directory -Leaf
    $configuration = Split-Path (Split-Path $directory) -Leaf
    $names = @('ConsoleMonster', 'ConsoleMonster.PowerShell', 'ConsoleMonster.PowerShell.Display', 'ConsoleMonster.Markdown', 'ConsoleMonster.Graphics')
    if ($IncludeIntegrations) { $names += 'ConsoleMonster.Hosting', 'ConsoleMonster.Extensions.Logging' }
    foreach ($name in $names) {
        $path = Join-Path $directory ($name + '.dll')
        if ($IncludeIntegrations -and $RepositoryRoot -and $name -in @('ConsoleMonster.Hosting', 'ConsoleMonster.Extensions.Logging')) {
            $consumerPath = Join-Path $RepositoryRoot "ConsoleMonster.Extensions.Tests/bin/$configuration/$framework/$name.dll"
            if (Test-Path -LiteralPath $consumerPath) { $path = $consumerPath }
        }
        if (-not (Test-Path -LiteralPath $path) -and $RepositoryRoot) {
            $path = Join-Path $RepositoryRoot "$name/bin/$configuration/$framework/$name.dll"
        }
        if (-not (Test-Path -LiteralPath $path)) { throw "Build the shipped assembly before checking its API: $path" }
        if ($PSVersionTable.PSEdition -eq 'Core' -and $name -notin @('ConsoleMonster.Hosting', 'ConsoleMonster.Extensions.Logging')) {
            $context = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($ModuleAssembly)
            $context.LoadFromAssemblyPath([IO.Path]::GetFullPath($path))
        } else {
            [Reflection.Assembly]::LoadFrom($path)
        }
    }
}

function Get-ConsoleApiFluentMethods {
    param([Type] $FluentType, [Type] $OwnerType)
    foreach ($method in $FluentType.GetMethods([Reflection.BindingFlags]'Public,Static,DeclaredOnly')) {
        $parameters = $method.GetParameters()
        if ($parameters.Count -eq 0) { continue }
        if ($parameters[0].ParameterType -eq $OwnerType) { $method; continue }
        if ($method.IsGenericMethodDefinition -and $method.GetGenericArguments().Count -eq 1 -and $parameters[0].ParameterType.IsGenericParameter) {
            $constraints = @($parameters[0].ParameterType.GetGenericParameterConstraints())
            if ($constraints.Count -gt 0 -and @($constraints | Where-Object { -not $_.IsAssignableFrom($OwnerType) }).Count -eq 0) {
                $method.MakeGenericMethod([Type[]] @($OwnerType))
            }
        }
    }
}

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

function Get-ConsoleApiOwnerName {
    param([Type] $Type)
    if ($Type.IsGenericType) { return $Type.GetGenericTypeDefinition().FullName }
    return $Type.FullName
}

function Test-ConsoleApiPromptType {
    param([Type] $Type)
    for ($current = $Type; $null -ne $current; $current = $current.BaseType) {
        if ((Get-ConsoleApiOwnerName $current) -eq 'ConsoleMonster.Interactive.Prompt`1') { return $true }
    }
    return $false
}
