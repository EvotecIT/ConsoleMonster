Describe 'PowerShell and C# API parity' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/Get-ConsoleApiMetadata.ps1')
        $script:Parity = Get-Content (Join-Path $PSScriptRoot '../../Tests/Api/round2-parity.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        $script:ApiCommands = @(Get-Command -Module ConsoleMonster -CommandType Cmdlet)
        $assemblies = @([ConsoleMonster.Terminal].Assembly, (Get-Command Show-Console).ImplementingType.Assembly)
        foreach ($name in 'ConsoleMonster.Markdown', 'ConsoleMonster.Graphics') {
            $path = Join-Path (Split-Path (Get-Command Show-Console).ImplementingType.Assembly.Location) ($name + '.dll')
            $assemblies += [Reflection.Assembly]::LoadFrom($path)
        }
        $nullabilityType = [object].Assembly.GetType('System.Reflection.NullabilityInfoContext')
        $script:ApiNullability = if ($null -ne $nullabilityType) { [Activator]::CreateInstance($nullabilityType) } else { $null }
        $script:ApiTypes = @{}
        foreach ($assembly in $assemblies) {
            foreach ($type in $assembly.GetExportedTypes()) { $script:ApiTypes[$type.FullName] = $type }
        }
    }

    It 'maps every exported command and domain parameter, including inherited metadata and defaults' {
        $script:ApiCommands.Count | Should -Be $script:Parity.ExpectedCount
        Compare-Object @($script:ApiCommands.Name | Sort-Object) @($script:Parity.Commands.Command | Sort-Object) | Should -BeNullOrEmpty
        foreach ($entry in $script:Parity.Commands) {
            $command = $script:ApiCommands | Where-Object Name -EQ $entry.Command
            $parameters = @(Get-ConsoleApiParameters $command)
            Compare-Object @($parameters.Name | Sort-Object) @($entry.Bindings.Parameter | Sort-Object) | Should -BeNullOrEmpty -Because $entry.Command
            foreach ($binding in $entry.Bindings) {
                $actual = $parameters | Where-Object Name -EQ $binding.Parameter
                $actual.Type | Should -BeExactly $binding.ParameterType -Because "$($entry.Command) -$($binding.Parameter)"
                $actual.Default | Should -Be $binding.Default -Because "$($entry.Command) -$($binding.Parameter) default"
                $actual.Sets.Count | Should -Be $binding.Sets.Count
                foreach ($set in $binding.Sets) {
                    $actualSet = $actual.Sets | Where-Object Name -EQ $set.Name
                    $actualSet | Should -Not -BeNullOrEmpty -Because "$($entry.Command) -$($binding.Parameter) set $($set.Name)"
                    $actualSet.Position | Should -Be $set.Position
                    $actualSet.Mandatory | Should -Be $set.Mandatory
                }
                $scope = if ($binding.AppliesToSets) { @($binding.AppliesToSets) } else { @($binding.Sets.Name) }
                $overrides = @($entry.SetBindings | Where-Object Parameter -EQ $binding.Parameter)
                $effectiveSets = @($scope) + @($overrides.ParameterSet)
                Compare-Object @($binding.Sets.Name | Sort-Object) @($effectiveSets | Sort-Object) | Should -BeNullOrEmpty -Because "$($entry.Command) -$($binding.Parameter) needs exactly one mapping per set"
                @($effectiveSets | Group-Object | Where-Object Count -GT 1).Count | Should -Be 0
            }
            foreach ($override in $entry.SetBindings) {
                $binding = @($entry.Bindings | Where-Object Parameter -EQ $override.Parameter)
                $binding.Count | Should -Be 1
                $override.ParameterSet | Should -BeIn @($binding.Sets.Name)
            }
        }
        foreach ($job in ($script:Parity.Commands | Group-Object Job | Where-Object Count -GT 1)) {
            $job.Name | Should -Be Show-Console
            @($job.Group | Where-Object ShortcutOf -EQ Show-Console).Command | Sort-Object | Should -Be @('Show-ConsoleList', 'Show-ConsoleTable')
        }
    }

    It 'resolves every mapped C# member and argument against the actual shipped assemblies' {
        foreach ($entry in $script:Parity.Commands) {
            $script:ApiTypes.ContainsKey($entry.CSharpType) | Should -BeTrue -Because "$($entry.Command) needs a C# owner"
            foreach ($binding in @($entry.Bindings) + @($entry.SetBindings | Where-Object { $null -ne $_ })) {
                $label = "$($entry.Command) -$($binding.Parameter)"
                if ($binding.MemberKind -eq 'HostPolicy') {
                    $binding.Reason | Should -Not -BeNullOrEmpty -Because $label
                    $binding.Conversion | Should -Not -Be Identity -Because $label
                    continue
                }
                if ($binding.Conversion -eq 'BooleanInversion') {
                    $binding.CSharpTypeName | Should -BeIn @('System.Boolean', 'System.Nullable`1[System.Boolean]') -Because "$label inverts a Boolean, not a similarly named data collection"
                }
                if ($binding.Conversion -in @('SecondsToTimeSpan', 'MillisecondsToTimeSpan')) {
                    $binding.CSharpTypeName | Should -BeIn @('System.TimeSpan', 'System.Nullable`1[System.TimeSpan]') -Because "$label has an explicit unit conversion"
                }
                if ($binding.Conversion -eq 'SingleIndex') {
                    $binding.CSharpTypeName | Should -BeExactly 'System.Int32'
                    $binding.ParameterSet | Should -BeExactly 'Run'
                    $binding.Reason | Should -Not -BeNullOrEmpty
                }
                $type = $script:ApiTypes[$binding.CSharpType]
                $type | Should -Not -BeNullOrEmpty -Because $label
                if ($binding.FluentMember) {
                    $fluent = $script:ApiTypes[$binding.CSharpType + 'Fluent']
                    $fluent | Should -Not -BeNullOrEmpty -Because "$label needs a real fluent collection operation"
                    @($fluent.GetMethods() | Where-Object {
                        $_.Name -eq $binding.FluentMember -and $_.GetParameters()[0].ParameterType -eq $type
                    }).Count | Should -BeGreaterThan 0 -Because "$label generates this call in How it maps"
                }
                if ($binding.MemberKind -eq 'Property') {
                    $member = $type.GetProperty($binding.Member)
                    $member | Should -Not -BeNullOrEmpty -Because $label
                    (Get-ConsoleApiTypeName $member.PropertyType) | Should -BeExactly $binding.CSharpTypeName -Because $label
                    ($null -ne $member.GetSetMethod($false)) | Should -Be $binding.Writable -Because $label
                    $member.GetMethod.IsStatic | Should -Be $binding.Static -Because $label
                    if ($null -ne $script:ApiNullability) {
                        $script:ApiNullability.Create($member).WriteState.ToString() | Should -BeExactly $binding.Nullable -Because $label
                    }
                } elseif ($binding.MemberKind -eq 'Event') {
                    $member = $type.GetEvent($binding.Member)
                    $member | Should -Not -BeNullOrEmpty -Because $label
                    (Get-ConsoleApiTypeName $member.EventHandlerType) | Should -BeExactly $binding.CSharpTypeName -Because $label
                } else {
                    $members = if ($binding.MemberKind -eq 'Constructor') { @($type.GetConstructors()) } else { @($type.GetMethods() | Where-Object Name -EQ $binding.Member) }
                    $members.Count | Should -BeGreaterThan 0 -Because $label
                    if ($binding.Argument) {
                        $matching = @($members | ForEach-Object { $_.GetParameters() } | Where-Object {
                            $_.Name -ceq $binding.Argument -and (Get-ConsoleApiTypeName $_.ParameterType) -ceq $binding.CSharpTypeName
                        })
                        $matching.Count | Should -BeGreaterThan 0 -Because "$label maps to $($binding.CSharpType).$($binding.Member)($($binding.Argument))"
                    }
                }
            }
        }
    }
    It 'covers every public configuration member through an exact binding, typed options or an individual decision' {
        $owners = @($script:Parity.Commands.CSharpType)
        $seeds = @($script:ApiTypes.Values | Where-Object {
            -not [System.Management.Automation.PSCmdlet].IsAssignableFrom($_) -and
            -not $_.Name.EndsWith('Fluent', [StringComparison]::Ordinal) -and
            (($_.FullName -ne 'ConsoleMonster.Layout.IWidget' -and $script:ApiTypes['ConsoleMonster.Layout.IWidget'].IsAssignableFrom($_)) -or $_.Name.EndsWith('Options', [StringComparison]::Ordinal) -or $_.FullName -in $owners -or (Test-ConsoleApiPromptType $_) -or $_.FullName -in @('ConsoleMonster.Styling.ThemeFile', 'ConsoleMonster.Live.WidgetUpdate', 'ConsoleMonster.Live.RunUpdate'))
        })
        Compare-Object @($seeds.FullName | Sort-Object) @($script:Parity.ConfigurableRoots | Sort-Object) | Should -BeNullOrEmpty
        $graph = Get-ConsoleApiTypeGraph $seeds $script:ApiTypes -IncludeItemMethods
        Compare-Object @($graph.Keys | Sort-Object) @($script:Parity.ConfigurableTypes | Sort-Object) | Should -BeNullOrEmpty
        $typedRoots = @($script:ApiCommands | ForEach-Object {
            $command = $_
            foreach ($entry in $command.Parameters.GetEnumerator()) {
                $property = $command.ImplementingType.GetProperty($entry.Key)
                if ($null -ne $property -and $property.DeclaringType.Namespace -like 'ConsoleMonster*') {
                    Get-ConsoleApiReferences $property.PropertyType $script:ApiTypes
                }
            }
        })
        $typed = Get-ConsoleApiTypeGraph $typedRoots $script:ApiTypes
        $covered = @{}
        foreach ($entry in $script:Parity.Commands) {
            foreach ($binding in @($entry.Bindings) + @($entry.SetBindings | Where-Object { $null -ne $_ })) {
                if ($binding.MemberKind -eq 'Property') {
                    $property = $script:ApiTypes[$binding.CSharpType].GetProperty($binding.Member)
                    $covered[(Get-ConsoleApiOwnerName $property.DeclaringType) + '.' + $property.Name] = $true
                }
            }
        }
        foreach ($route in $script:Parity.RelatedBindings) {
            $route.MemberId | Should -BeExactly ($route.CSharpType + '.' + $route.Member) -Because 'generated translation notes resolve this exact property identity'
            $property = $script:ApiTypes[$route.CSharpType].GetProperty($route.Member)
            $property | Should -Not -BeNullOrEmpty
            (Get-ConsoleApiTypeName $property.PropertyType) | Should -BeExactly $route.CSharpTypeName
            $entry = $script:Parity.Commands | Where-Object Command -EQ $route.Command
            @($entry.Bindings | Where-Object Parameter -EQ $route.Parameter).Count | Should -Be 1
            $route.Reason | Should -Not -BeNullOrEmpty
            $covered[(Get-ConsoleApiOwnerName $property.DeclaringType) + '.' + $property.Name] = $true
        }
        $decisions = @{}
        foreach ($decision in $script:Parity.CSharpOnly) {
            $property = $script:ApiTypes[$decision.CSharpType].GetProperty($decision.Member)
            $property | Should -Not -BeNullOrEmpty
            (Get-ConsoleApiTypeName $property.PropertyType) | Should -BeExactly $decision.CSharpTypeName
            $decision.Reason | Should -Not -BeNullOrEmpty
            $decision.Alternative | Should -Not -BeNullOrEmpty
            $key = (Get-ConsoleApiOwnerName $property.DeclaringType) + '.' + $property.Name
            $decisions.ContainsKey($key) | Should -BeFalse -Because "$key has one decision"
            $covered.ContainsKey($key) | Should -BeFalse -Because "$key must not retain an obsolete exception"
            if (-not $decision.OverrideTypedInput) {
                ($typed.ContainsKey((Get-ConsoleApiOwnerName $property.DeclaringType)) -and -not $property.GetMethod.IsStatic) | Should -BeFalse -Because "$key is already reachable as typed configuration"
            }
            $decisions[$key] = $true
        }
        foreach ($type in $graph.Values) {
            foreach ($property in $type.GetProperties()) {
                if (-not (Test-ConsoleApiConfigurationProperty $property)) { continue }
                $key = (Get-ConsoleApiOwnerName $property.DeclaringType) + '.' + $property.Name
                $reachable = $covered.ContainsKey($key) -or $decisions.ContainsKey($key) -or
                    (-not $property.GetMethod.IsStatic -and ($typed.ContainsKey($type.FullName) -or $typed.ContainsKey((Get-ConsoleApiOwnerName $property.DeclaringType))))
                $reachable | Should -BeTrue -Because "$key needs a PowerShell route or a specific C#-only decision"
            }
        }
    }

    It 'keeps pipeline endings on the same C# display and export operations' {
        foreach ($ending in $script:Parity.Endings) {
            @($script:ApiCommands | Where-Object Name -EQ $ending.Command).Count | Should -Be 1
            $methods = @($script:ApiTypes[$ending.CSharpType].GetMethods() | Where-Object Name -EQ $ending.Member)
            @($methods | ForEach-Object { $_.GetParameters() } | Where-Object Name -EQ $ending.Argument).Count | Should -BeGreaterThan 0
            $fluent = @($script:ApiTypes[$ending.FluentType].GetMethods() | Where-Object Name -EQ $ending.FluentMember)
            $fluent.Count | Should -BeGreaterThan 0
            if ($ending.FluentArgument) {
                @($fluent | ForEach-Object { $_.GetParameters() } | Where-Object Name -EQ $ending.FluentArgument).Count | Should -BeGreaterThan 0
            }
        }
    }

    It 'records and enforces the delegate-property fluent exception' {
        $rules = @($script:Parity.FluentExceptions)
        $rules.Count | Should -Be 1
        $rules[0].Kind | Should -BeExactly DelegateProperty
        $rules[0].Method | Should -BeExactly Configure
        $rules[0].Pattern | Should -Match '^\.Configure\(item => item\.\{Member\} = .+\)$'
        $rules[0].Reason | Should -Not -BeNullOrEmpty
        foreach ($name in 'ConsoleMonster.Interactive.MenuItem', 'ConsoleMonster.Interactive.SelectQuestionOptions') {
            $type = $script:ApiTypes[$name]
            $methods = @($script:ApiTypes[($name + 'Fluent')].GetMethods())
            @($methods | Where-Object Name -EQ Configure).Count | Should -Be 1
            foreach ($property in $type.GetProperties()) {
                if ($null -ne $property.GetSetMethod($false) -and [Delegate].IsAssignableFrom($property.PropertyType)) {
                    @($methods | Where-Object Name -EQ $property.Name).Count | Should -Be 0
                }
            }
        }
    }
    It 'resolves display-specific operations and their fluent factories from the parity map' {
        foreach ($operation in $script:Parity.Operations) {
            $command = $script:ApiCommands | Where-Object Name -EQ $operation.Command
            $command | Should -Not -BeNullOrEmpty
            $command.Parameters.ContainsKey($operation.DiscriminatorParameter) | Should -BeTrue
            $type = $script:ApiTypes[$operation.CSharpType]
            $type | Should -Not -BeNullOrEmpty
            $methods = @($type.GetMethods() | Where-Object Name -EQ $operation.Member)
            @($methods | ForEach-Object { $_.GetParameters() } | Where-Object Name -EQ $operation.Argument).Count | Should -BeGreaterThan 0
            @($type.GetMethods() | Where-Object Name -EQ $operation.FluentFactory).Count | Should -BeGreaterThan 0
            @($script:Parity.Endings | Where-Object FluentMember -EQ $operation.FluentMember).Count | Should -Be 1
        }
    }

    It 'validates the explicitly mapped native baseline without counting it as a module command' {
        foreach ($operation in $script:Parity.ExternalOperations) {
            (Get-Command $operation.Command -CommandType Cmdlet).ModuleName | Should -Not -Be ConsoleMonster
            $operation.CSharpType | Should -BeExactly 'System.Console'
            @([Console].GetMethods() | Where-Object Name -EQ $operation.Member).Count | Should -BeGreaterThan 0
            $operation.Reason | Should -Not -BeNullOrEmpty
        }
    }

}
