[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $AssemblyDirectory,
    [Parameter(Mandatory = $true)] [string] $ResultsPath
)
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.dll')
Add-Type -Path (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.Display.dll')
$fixtureOptions=@{}
# PS7 may compile the net8 fixture references on .NET 10; suppress only assembly-version unification CS1701.
if($PSVersionTable.PSVersion.Major -eq 7){$fixtureOptions.CompilerOptions=@('/nowarn:1701')}
Add-Type -Path (Join-Path $PSScriptRoot 'Fixtures/RuntimeTypeBuilder.cs') @fixtureOptions -ReferencedAssemblies @(
    (Join-Path $AssemblyDirectory 'ConsoleMonster.dll'),
    (Join-Path $AssemblyDirectory 'ConsoleMonster.PowerShell.Display.dll'), [Console].Assembly.Location)
Add-Type -TypeDefinition @'
namespace DisplayContract {
    public sealed class Row {
        public string Name { get; set; }
        public string Scope { get; set; }
        public string Status { get; set; }
        public string StartupType { get; set; }
        public int LazyCalls;
        public string Lazy { get { LazyCalls++; throw new System.InvalidOperationException("Do not read a lazy property."); } }
        public Row(string name) { Name=name; Scope="scope"; Status="Stopped"; StartupType="Automatic"; }
    }
    public sealed class TranscriptState { internal bool IsTranscribing { get; set; } public TranscriptState(bool value) { IsTranscribing=value; } }
    public sealed class TreeRow { public string Name { get; set; } public TreeRow[] Children { get; set; } }
    public struct UnsafeValue : System.IFormattable {
        public static int Calls;
        public override string ToString() { Calls++; throw new System.InvalidOperationException("Application ToString ran."); }
        public string ToString(string format, System.IFormatProvider provider) { Calls++; throw new System.InvalidOperationException("Application formatter ran."); }
        public override bool Equals(object value) { Calls++; throw new System.InvalidOperationException("Application comparison ran."); }
        public override int GetHashCode() { Calls++; throw new System.InvalidOperationException("Application hashing ran."); }
    }
    public sealed class ScalarRow {
        public UnsafeValue Value;
        public int Number = 42;
        public System.DateTime When = new System.DateTime(2026, 10, 27);
        public ScalarRow[] Children;
    }
    public sealed class ThrowingTranscriptState { internal bool IsTranscribing { get { throw new System.InvalidOperationException(); } } }
}
'@
$script:DisplayResults = New-Object 'System.Collections.Generic.List[object]'
function Assert-Display {
    param([bool] $Condition, [string] $Message)
    if (-not $Condition) { throw $Message }
}
function Assert-Throws {
    param([scriptblock] $Action)
    $thrown = $false
    try { & $Action } catch { $thrown = $true }
    Assert-Display $thrown 'Expected rejection did not occur.'
}
function Test-DisplayContract {
    param([string] $Name, [scriptblock] $Action)
    try { & $Action; $script:DisplayResults.Add([pscustomobject]@{ Name=$Name; Passed=$true; Error=$null }) }
    catch { $script:DisplayResults.Add([pscustomobject]@{ Name=$Name; Passed=$false; Error=$_.Exception.ToString() }) }
}
$script:DisplayLeases = New-Object 'System.Collections.Generic.List[System.IDisposable]'
$script:DisplayBuilder = [Func[System.Collections.Generic.IReadOnlyList[object], ConsoleMonster.PowerShell.Display.DefaultDisplayContext, ConsoleMonster.Layout.IWidget]] {
    param($objects, $context)
    [ConsoleMonster.Widgets.TextBlock]::Plain('built ' + $objects.Count)
}
function New-DisplayLease {
    param([string] $Name, [string[]] $Types = @('DisplayContract.Row'), [int] $Level = 1,
        [ConsoleMonster.PowerShell.Display.DefaultDisplaySource] $Source = 'Product')
    $lease = [ConsoleMonster.PowerShell.Display.DefaultDisplay]::RegisterModule($Name, [version]'1.0.0', $Types, $Level, $Source)
    $script:DisplayLeases.Add($lease)
    return $lease
}
function Add-DisplayView {
    param($Lease, [string] $View = 'contract.row', [string] $Version = '1.0.0')
    $Lease.Register('DisplayContract.Row', $View, [version]$Version,
        [ConsoleMonster.PowerShell.Display.DefaultDisplayBatch]::Single, [string[]]@('Scope'), $script:DisplayBuilder, $null)
}
$beforeDefault = Get-Command Out-Default
$beforeMode = Get-Command Set-ConsoleDefaultDisplay -ErrorAction Ignore
$beforeEnv = $env:CONSOLEMONSTER_DEFAULT_VIEWS
$env:CONSOLEMONSTER_DEFAULT_VIEWS = $null
$displayProfile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed(80, 30).WithMode([ConsoleMonster.Capabilities.OutputMode]::Plain)
$context = [ConsoleMonster.PowerShell.Display.DefaultDisplayContext]::new($displayProfile, 'scope', [ConsoleMonster.PowerShell.Display.DefaultDisplayMode]::Plain)
try {
    Test-DisplayContract 'ProductWhitelist_ForbiddenAndUnownedTypes_Rejected' {
        foreach ($type in @('System.String', 'System.Management.Automation.PSCustomObject', 'DisplayContract.*', 'Deserialized.DisplayContract.Row')) {
            Assert-Throws { New-DisplayLease -Name 'Invalid' -Types @($type) | Out-Null }
        }
        $lease = New-DisplayLease 'Ownership'
        Assert-Throws { $lease.Register('Other.Row', 'other', [version]'1.0.0', 'Single', $null, $script:DisplayBuilder, $null) }
        $lease.Dispose()
    }
    Test-DisplayContract 'Resolve_ExactClrType_PreservesObjectAndTypeNames' {
        $lease = New-DisplayLease 'Identity'
        $view = Add-DisplayView $lease
        $row = [DisplayContract.Row]::new('original')
        $names = @($row.PSObject.TypeNames)
        $resolved = [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($row)
        Assert-Display ([object]::ReferenceEquals($view, $resolved)) 'The exact registered view was not selected.'
        Assert-Display (($names -join '|') -ceq ($row.PSObject.TypeNames -join '|')) 'Type names were changed.'
        Assert-Display ($row.Name -ceq 'original') 'The object was changed.'
        $row.PSObject.TypeNames.Insert(0, 'Deserialized.DisplayContract.Row')
        Assert-Display ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($row)) 'Deserialized data selected a rich view.'
        $lease.Dispose()
    }
    Test-DisplayContract 'Resolve_CompatibleVersionsAndLeaseRelease_RestoresEarlierView' {
        $first = New-DisplayLease 'First'
        $old = Add-DisplayView $first
        $second = New-DisplayLease 'Second'
        $equal = Add-DisplayView $second
        Assert-Display ([object]::ReferenceEquals($old, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a')))) 'Equal version replaced the earlier implementation.'
        $newer = Add-DisplayView $second -Version '1.1.0'
        Assert-Display ([object]::ReferenceEquals($newer, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a')))) 'Higher compatible version did not win.'
        $second.Dispose()
        Assert-Display ([object]::ReferenceEquals($old, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a')))) 'Releasing one lease removed another module view.'
        $first.Dispose()
    }
    Test-DisplayContract 'Resolve_UserCommandProductPrecedence_IsStable' {
        $product = New-DisplayLease 'Product'
        $p = Add-DisplayView $product
        $command = New-DisplayLease 'Command' -Source CommandModule
        $c = Add-DisplayView $command
        $user = New-DisplayLease 'User' -Source User
        $u = Add-DisplayView $user
        Assert-Display ([object]::ReferenceEquals($u, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a')))) 'User precedence failed.'
        $user.Dispose()
        Assert-Display ([object]::ReferenceEquals($c, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a')))) 'Command precedence failed.'
        $command.Dispose(); $product.Dispose()
    }
    Test-DisplayContract 'Resolve_SameFullNameDifferentAssemblies_UsesMatchingTypedBuilderAndIndependentLeases' {
        $types = @('DisplayContextA', 'DisplayContextB') | ForEach-Object {
            $name = [Reflection.AssemblyName]::new($_)
            if ($PSVersionTable.PSVersion.Major -eq 5) {
                $assembly = [AppDomain]::CurrentDomain.DefineDynamicAssembly($name, [Reflection.Emit.AssemblyBuilderAccess]::Run)
            } else {
                $assembly = [Reflection.Emit.AssemblyBuilder]::DefineDynamicAssembly($name, [Reflection.Emit.AssemblyBuilderAccess]::Run)
            }
            $assembly.DefineDynamicModule($_).DefineType('DisplayContract.PrivateRow', [Reflection.TypeAttributes]::Public).CreateType()
        }
        $a = New-DisplayLease 'ContextA' -Types @('DisplayContract.PrivateRow')
        $b = New-DisplayLease 'ContextB' -Types @('DisplayContract.PrivateRow')
        $backup = New-DisplayLease 'ContextABackup' -Types @('DisplayContract.PrivateRow')
        $factory = [DisplayContract.RuntimeTypeBuilder].GetMethod('Create')
        $builderA = $factory.MakeGenericMethod($types[0]).Invoke($null, $null)
        $builderB = $factory.MakeGenericMethod($types[1]).Invoke($null, $null)
        $viewA = $a.Register('DisplayContract.PrivateRow', 'private.row', [version]'1.0.0', 'Single', $null, $builderA, $null, $null, $types[0])
        $viewB = $b.Register('DisplayContract.PrivateRow', 'private.row', [version]'1.0.0', 'Single', $null, $builderB, $null, $null, $types[1])
        $viewBackup = $backup.Register('DisplayContract.PrivateRow', 'private.row', [version]'1.0.0', 'Single', $null, $builderA, $null, $null, $types[0])
        Assert-Throws { $a.Register('DisplayContract.PrivateRow', 'wrong.type', [version]'1.0.0', 'Single', $null, $builderA, $null, $null, [DisplayContract.Row]) }
        $objects = @([Activator]::CreateInstance($types[0]), [Activator]::CreateInstance($types[1]))
        $expected = @($viewA, $viewB)
        for ($i = 0; $i -lt 2; $i++) {
            $selected = [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($objects[$i])
            Assert-Display ([object]::ReferenceEquals($expected[$i], $selected)) 'A different assembly identity selected the typed builder.'
            $items = New-Object 'System.Collections.Generic.List[object]'; $items.Add($objects[$i])
            $text = [ConsoleMonster.Terminal]::RenderText($selected.Build($items, $context), $displayProfile)
            Assert-Display ($text -match @('DisplayContextA','DisplayContextB')[$i]) 'The matching typed cast did not render.'
        }
        $user = New-DisplayLease 'PrivateUser' -Types @('DisplayContract.PrivateRow') -Source User
        $userView = $user.Register('DisplayContract.PrivateRow', 'user.row', [version]'1.0.0', 'Single', $null, $script:DisplayBuilder)
        foreach ($value in $objects) {
            Assert-Display ([object]::ReferenceEquals($userView, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($value))) 'Name-based user priority was lost.'
        }
        $user.Dispose(); $a.Dispose()
        Assert-Display ([object]::ReferenceEquals($viewBackup, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($objects[0]))) 'Equal-version backup for the same runtime type was lost.'
        Assert-Display ([object]::ReferenceEquals($viewB, [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($objects[1]))) 'Releasing one context affected the other.'
        $backup.Dispose()
        Assert-Display ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($objects[0])) 'The remaining foreign typed builder accepted the released context.'
        $b.Dispose()
    }
    Test-DisplayContract 'Modes_UserAndEnvironmentOptOut_OverrideModuleDefault' {
        $lease = New-DisplayLease 'ModeTest'
        $view = Add-DisplayView $lease
        [ConsoleMonster.PowerShell.Display.DefaultDisplay]::SetMode('Off', 'ModeTest')
        Assert-Display ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a'))) 'Off did not delegate natively.'
        [ConsoleMonster.PowerShell.Display.DefaultDisplay]::SetMode('Auto', 'ModeTest')
        $env:CONSOLEMONSTER_DEFAULT_VIEWS = '0'
        Assert-Display ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a'))) 'Process opt-out was ignored.'
        $env:CONSOLEMONSTER_DEFAULT_VIEWS = $null
        $lease.Dispose()
    }
    Test-DisplayContract 'Lease_NewerFeatureLevel_ImportsWithoutViews' {
        $lease = New-DisplayLease 'Future' -Level 999
        $view = Add-DisplayView $lease
        Assert-Display (-not $lease.IsCompatible) 'Newer feature requirement was accepted.'
        Assert-Display ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('a'))) 'Incompatible module enabled richness.'
        $lease.Dispose()
    }
    Test-DisplayContract 'Registry_SeparateRunspaces_DoNotShareViewsOrModes' {
        $lease=New-DisplayLease 'ParentRunspace'
        $view=Add-DisplayView $lease
        $runspace=[Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
        $runspace.Open()
        $pipeline=[PowerShell]::Create();$pipeline.Runspace=$runspace
        try {
            $source=@'
$absent=$null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('child'))
[ConsoleMonster.PowerShell.Display.DefaultDisplay]::SetMode('Off',$null)
[pscustomobject]@{ParentAbsent=$absent}
'@
            $null=$pipeline.AddScript($source)
            $receipt=$pipeline.Invoke()
            Assert-Display (-not $pipeline.HadErrors -and $receipt.Count -eq 1 -and $receipt[0].ParentAbsent) 'The child runspace saw parent registrations.'
            Assert-Display ([object]::ReferenceEquals($view,[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.Row]::new('parent')))) 'A child mode change affected the parent runspace.'
        } finally {$pipeline.Dispose();$runspace.Dispose();$lease.Dispose()}
    }
    Test-DisplayContract 'HostGate_TranscriptActiveUnknownOrThrowing_FailsClosed' {
        Assert-Display ([ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::IsTranscriptInactive([DisplayContract.TranscriptState]::new($false))) 'Known inactive transcription was rejected.'
        Assert-Display (-not [ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::IsTranscriptInactive([DisplayContract.TranscriptState]::new($true))) 'Active transcription was admitted.'
        Assert-Display (-not [ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::IsTranscriptInactive([object]::new())) 'Unknown transcription was admitted.'
        Assert-Display (-not [ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::IsTranscriptInactive([DisplayContract.ThrowingTranscriptState]::new())) 'Throwing transcription state was admitted.'
        Assert-Display ([ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::IsTranscriptInactive($Host.UI)) 'Actual host transcription state could not be read.'
    }
    Test-DisplayContract 'HostGate_PlainProfile_RequiresWindowsHandleOrActualUnixTty' {
        $physical = [ConsoleMonster.Capabilities.TerminalProfile]::new([ConsoleMonster.Capabilities.ColorDepth]::None, [ConsoleMonster.Capabilities.GlyphTier]::Ascii,
            [ConsoleMonster.Capabilities.OutputMode]::Plain, 80, 30, [ConsoleMonster.Capabilities.TerminalKind]::WindowsTerminal, $false, $false, $true, $false)
        Assert-Display ([ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::SupportsPhysicalProfile($physical)) 'Plain output disabled a physical host layout.'
        $unixTty = [Environment]::OSVersion.Platform -eq [PlatformID]::Unix -and -not [Console]::IsOutputRedirected
        Assert-Display ([ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::SupportsPhysicalProfile($displayProfile) -eq $unixTty) 'A handle-free profile must follow the actual Unix stdout route.'
        $redirected = [ConsoleMonster.Capabilities.TerminalProfile]::new([ConsoleMonster.Capabilities.ColorDepth]::None, [ConsoleMonster.Capabilities.GlyphTier]::Ascii,
            [ConsoleMonster.Capabilities.OutputMode]::Plain, 80, 30, [ConsoleMonster.Capabilities.TerminalKind]::Redirected, $false, $true, $false, $false)
        Assert-Display (-not [ConsoleMonster.PowerShell.Display.DefaultDisplayHostGate]::SupportsPhysicalProfile($redirected)) 'A redirected profile passed the physical host gate.'
    }
    Test-DisplayContract 'SchemaV2_ConditionalTonesTemplatesAndScope_RenderWithoutLazyGetters' {
        $document = [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{
            SchemaVersion=2; Views=@(@{ TypeName='DisplayContract.Row'; View='Table'; Batch='Stream'; ScopeKey=@('Scope');
                Property=@('Name','Status','Lazy'); Title='{Name}'; Priority=@{ Lazy=-1 };
                Tones=@(@{Property='Status'; Fail='Stopped'; When=@{StartupType='Automatic'}}); Budget=@{Records=2;Bytes=4096;Nodes=10;Depth=2} })
        })
        $lease = New-DisplayLease 'Schema'
        $views = $document.Register($lease, [version]'1.0.0')
        $row = [DisplayContract.Row]::new('name[bold]')
        $items = New-Object 'System.Collections.Generic.List[object]'; $items.Add($row)
        $widget = $views[0].Build($items, $context)
        $text = [ConsoleMonster.Terminal]::RenderText($widget, $displayProfile)
        Assert-Display ($text -match 'name\[bold\]') 'A template value was interpreted as markup.'
        Assert-Display ($text -match 'Stopped') 'The status property was omitted.'
        Assert-Display ($row.LazyCalls -eq 0) 'A lazy getter ran.'
        Assert-Display ($views[0].Batch -eq 'Stream' -and $views[0].ScopeKey[0] -eq 'Scope') 'Batch or scope metadata was lost.'
        $lease.Dispose()
    }
    Test-DisplayContract 'SchemaV2_ExecutableNestedDataUnknownKeysAndExcessBudgets_Rejected' {
        Assert-Throws { [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.Row';Tones=@(@{Property='Status';Fail=@{hidden={throw 'executed'}}})})}) }
        Assert-Throws { [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.Row';Unknown='x'})}) }
        Assert-Throws { [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.Row';Budget=@{Records=2001}})}) }
        Assert-Throws { [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.Row';Title='{Name.ToString()}'})}) }
    }
    Test-DisplayContract 'SchemaV2_CustomValueType_NeverInvokesApplicationFormattingInAnyPresentation' {
        $lease = New-DisplayLease 'SafeScalars' -Types @('DisplayContract.ScalarRow')
        $items = New-Object 'System.Collections.Generic.List[object]'; $items.Add([DisplayContract.ScalarRow]::new())
        [DisplayContract.UnsafeValue]::Calls = 0
        foreach ($kind in @('Table', 'Cards', 'Record', 'Tree')) {
            $doc = [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{
                TypeName='DisplayContract.ScalarRow';View=$kind;Property=@('Value','Number','When');Children='Children';Title='value {Value}';Cycle='Value';Repeat='Value'
            })})
            $registered = $doc.Register($lease)
            $text = [ConsoleMonster.Terminal]::RenderText($registered[0].Build($items, $context), $displayProfile)
            Assert-Display ($text -match 'UnsafeValue') ('The safe type label was lost in '+$kind)
            if ($kind -ne 'Tree') { Assert-Display ($text -match '42') ('Safe numeric data was lost in '+$kind) }
        }
        Assert-Display ([DisplayContract.UnsafeValue]::Calls -eq 0) 'Application-defined value formatting was invoked.'
        $lease.Dispose()
    }
    Test-DisplayContract 'SchemaV1_ExistingColumnPresenterFile_IsAccepted' {
        $document = [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=1;Views=@(@{TypeName='DisplayContract.Row';View='Table';Property=@('Name',@{Name='Health';Expression='Status';Presenter='StatusWord'});GroupBy='Scope'})})
        Assert-Display ($document.SchemaVersion -eq 1 -and $document.TypeNames[0] -ceq 'DisplayContract.Row') 'Schema 1 failed to parse.'
    }
    Test-DisplayContract 'ParseText_LiteralResourceData_RegistersAndRendersWithoutLazyGetters' {
        $source = @'
# Text read directly from an embedded resource; no module path or temporary file.
@{
    SchemaVersion = 2
    Views = @(@{ TypeName = 'DisplayContract.Row'; View = 'Table'; Batch = 'Single'; Property = @('Name', 'Status') })
}
'@
        $doc = [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::ParseText($source)
        $lease = New-DisplayLease 'TextResource'
        $views = $doc.Register($lease)
        $items = New-Object 'System.Collections.Generic.List[object]'; $row = [DisplayContract.Row]::new('embedded-table'); $items.Add($row)
        $text = [ConsoleMonster.Terminal]::RenderText($views[0].Build($items, $context), $displayProfile)
        Assert-Display ($doc.SchemaVersion -eq 2 -and $text -match 'embedded-table' -and $text -match 'Stopped' -and $row.LazyCalls -eq 0) 'Resource data did not produce its declared table.'
        $v1 = [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::ParseText("@{ Views = @(@{ TypeName='DisplayContract.Row'; View='Table'; Property='Name' }) }")
        Assert-Display ($v1.SchemaVersion -eq 1 -and $v1.TypeNames[0] -ceq 'DisplayContract.Row') 'Schema 1 text was not accepted.'
        $lease.Dispose()
    }
    Test-DisplayContract 'ParseText_ExecutableMalformedOrMultipleStatements_RejectsWithoutExecution' {
        $global:DisplayParserExecution = 0
        try {
            foreach ($source in @(
                '@{ Views=@() }; $global:DisplayParserExecution++',
                'param($x) @{ Views=@() }',
                'begin { $global:DisplayParserExecution++ }; end { @{ Views=@() } }',
                'trap { $global:DisplayParserExecution++ }; @{ Views=@() }',
                '@{ Views=@() } | ForEach-Object { $global:DisplayParserExecution++ }',
                '@{ Views=@() } > unexpected-file',
                '@{ Views=@(); Hidden={ $global:DisplayParserExecution++ } }',
                '@{ Views=$($global:DisplayParserExecution++; @()) }',
                '@{ Views=(& { $global:DisplayParserExecution++; @() }) }',
                '@{ Views=@(',
                'using namespace System; @{ Views=@() }',
                ''
            )) { Assert-Throws { [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::ParseText($source) } }
            Assert-Display ($global:DisplayParserExecution -eq 0) 'Document parsing executed input code.'
            Assert-Throws { [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::ParseText('@{ SchemaVersion=2; Views=@(@{ TypeName="DisplayContract.Row"; Unknown=1 }) }') }
            Assert-Throws { [ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::ParseText('@{ SchemaVersion=2; Views=@(@{ TypeName="DisplayContract.Row"; Budget=@{ Records=2001 } }) }') }
        } finally { Remove-Variable DisplayParserExecution -Scope Global }
    }
    Test-DisplayContract 'Registration_CompiledBuilderAndScopeSelector_UsesOriginalReference' {
        $lease=New-DisplayLease 'Compiled'
        $selector=[Func[object,string]]{param($value) 'run:'+ $value.Scope}
        $view=$lease.Register('DisplayContract.Row','compiled.row',[version]'1.0.0','Single',$null,$script:DisplayBuilder,$null,$selector)
        $row=[DisplayContract.Row]::new('one')
        Assert-Display ($view.ScopeSelector.Invoke($row) -ceq 'run:scope' -and $row.LazyCalls -eq 0) 'The scope selector changed data or read a lazy property.'
        $lease.RegisterBuilder('compiled',$script:DisplayBuilder)
        $doc=[ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.Row';Builder='compiled'})})
        $registered=$doc.Register($lease,[version]'1.0.0')
        $items=New-Object 'System.Collections.Generic.List[object]';$items.Add($row)
        Assert-Display ([ConsoleMonster.Terminal]::RenderText($registered[0].Build($items,$context),$displayProfile) -match 'built 1') 'A named compiled builder was not resolved from its owning lease.'
        $lease.Dispose()
    }
    Test-DisplayContract 'SchemaV2_CompiledBuilderRuntimeType_UsesExactProducerIdentity' {
        $lease=New-DisplayLease 'CompiledRuntimeType'
        try {
            $lease.RegisterBuilder('typed', $script:DisplayBuilder)
            $doc=[ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.Row';Builder='typed'})})
            $types=New-Object 'System.Collections.Generic.Dictionary[string,type]'
            $types.Add('DisplayContract.Row',[DisplayContract.Row])
            $views=$doc.Register($lease,[version]'1.0.0',$types)
            Assert-Display ([object]::ReferenceEquals($views[0].RuntimeType,[DisplayContract.Row])) 'The document lost its importing producer CLR identity.'
            $row=[DisplayContract.Row]::new('literal')
            Assert-Display ([object]::ReferenceEquals($views[0],[ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($row))) 'The actual producer did not resolve its compiled view.'
            $other=[pscustomobject]@{Name='literal'}; $other.PSObject.TypeNames.Insert(0,'DisplayContract.Row')
            Assert-Display ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve($other)) 'A type-name impersonation selected a compiled producer view.'
        } finally { $lease.Dispose() }
    }
    Test-DisplayContract 'SchemaV2_InvalidRuntimeType_RejectsTheWholeDocumentBeforeRegistration' {
        $lease=New-DisplayLease 'InvalidRuntimeType' -Types @('DisplayContract.ScalarRow','DisplayContract.TreeRow')
        try {
            $doc=[ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.ScalarRow';Property='Number'},@{TypeName='DisplayContract.TreeRow';Property='Name'})})
            $types=New-Object 'System.Collections.Generic.Dictionary[string,type]'
            $types.Add('DisplayContract.ScalarRow',[DisplayContract.ScalarRow]); $types.Add('DisplayContract.TreeRow',[DisplayContract.Row])
            Assert-Throws { $doc.Register($lease,[version]'1.0.0',$types) }
            Assert-Display ($null -eq [ConsoleMonster.PowerShell.Display.DefaultDisplay]::Resolve([DisplayContract.ScalarRow]::new())) 'An invalid later binding left an earlier partial registration.'
        } finally { $lease.Dispose() }
    }
    Test-DisplayContract 'SchemaV2_TreeNodeBudget_FallsBackToCompleteTopLevelTable' {
        $lease=New-DisplayLease 'TreeBudget' -Types @('DisplayContract.TreeRow')
        $doc=[ConsoleMonster.PowerShell.Display.ConsoleViewDocument]::Parse(@{SchemaVersion=2;Views=@(@{TypeName='DisplayContract.TreeRow';View='Tree';Property=@('Name');Children='Children';Budget=@{Nodes=2;Depth=3};OverBudget='Table'})})
        $views=$doc.Register($lease,[version]'1.0.0')
        $root=[DisplayContract.TreeRow]::new();$root.Name='root'
        $a=[DisplayContract.TreeRow]::new();$a.Name='child-a'
        $b=[DisplayContract.TreeRow]::new();$b.Name='child-b'
        $root.Children=[DisplayContract.TreeRow[]]@($a,$b)
        $items=New-Object 'System.Collections.Generic.List[object]';$items.Add($root)
        $widget=$views[0].Build($items,$context)
        Assert-Display ($widget -is [ConsoleMonster.Widgets.Table] -and $widget.Rows.Count -eq 1 -and $widget.Caption -match 'top-level rows') 'The tree budget returned a partial tree or lost its complete top-level fallback.'
        $lease.Dispose()
    }
    Test-DisplayContract 'Renderer_PlainModeAndOutputBudget_NoSgrAndBoundedResult' {
        $widget = [ConsoleMonster.Widgets.TextBlock]::new('[bold][fail]failed[/][/]')
        $encoded = [ConsoleMonster.PowerShell.Display.DefaultDisplayRenderer]::Prepare($widget, $displayProfile, 4096)
        Assert-Display (-not $encoded.Contains([string][char]27)) 'Plain mode retained escape sequences.'
        Assert-Display ($encoded -match 'failed') 'Plain mode lost the layout text.'
        Assert-Throws { [ConsoleMonster.PowerShell.Display.DefaultDisplayRenderer]::Prepare($widget, $displayProfile, 1) }
    }
    Test-DisplayContract 'Stream_ImmediateRows_OneHeaderStableWidthsAndOneSummary' {
        $lease=New-DisplayLease 'Stream'
        $rowBuilder=[Func[System.Collections.Generic.IReadOnlyList[object], ConsoleMonster.PowerShell.Display.DefaultDisplayContext, ConsoleMonster.Layout.IWidget]] {
            param($objects,$ctx)
            $table=[ConsoleMonster.Widgets.Table]::new()
            $table.Title='scope'
            $column=[ConsoleMonster.Widgets.TableColumn]::new('Name'); $column.Width=12
            $null=$table.AddColumn($column)
            $null=$table.AddRow([ConsoleMonster.Widgets.TableCell[]]@([ConsoleMonster.Widgets.TableCell]::FromValue($objects[0].Name)))
            return $table
        }
        $view=$lease.Register('DisplayContract.Row','stream.row',[version]'1.0.0','Stream',[string[]]@('Scope'),$rowBuilder,$null)
        $stream=[ConsoleMonster.PowerShell.Display.DefaultDisplayStream]::new($view,$context)
        try {
            $first=$stream.Append([DisplayContract.Row]::new('first'))
            $second=$stream.Append([DisplayContract.Row]::new('second'))
            $closed=$stream.Complete()
            Assert-Display ($first -match 'Name' -and $first -match 'first') 'The first row did not render immediately with its header.'
            Assert-Display ($second -notmatch 'Name' -and $second -match 'second') 'A repeated header or missing second row occurred.'
            Assert-Display ($closed -match '2 rows') 'The group summary was not emitted on closure.'
            Assert-Display ($stream.Complete() -ceq '') 'The group summary was repeated.'
            Assert-Display (-not ($first+$second+$closed).Contains([string][char]27)) 'Plain stream emitted an escape sequence.'
        } finally { $stream.Dispose(); $lease.Dispose() }
    }
} finally {
    foreach ($lease in $script:DisplayLeases) { $lease.Dispose() }
    $env:CONSOLEMONSTER_DEFAULT_VIEWS = $beforeEnv
}
Test-DisplayContract 'Broker_LastLease_RemovesOnlyOwnedFunctions' {
    $after = Get-Command Out-Default
    Assert-Display ($after.CommandType -eq $beforeDefault.CommandType -and $after.ModuleName -ceq $beforeDefault.ModuleName) 'Out-Default was not restored.'
    Assert-Display ((Get-Command Set-ConsoleDefaultDisplay -ErrorAction Ignore) -eq $beforeMode) 'Mode command was not restored.'
}
$report = [pscustomobject]@{
    Runtime=$PSVersionTable.PSVersion.ToString(); Tests=@($script:DisplayResults.ToArray());
    Passed=@($script:DisplayResults | Where-Object Passed).Count; Failed=@($script:DisplayResults | Where-Object { -not $_.Passed }).Count
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ResultsPath -Encoding UTF8
$report.Tests | Format-Table Name, Passed -AutoSize
if ($report.Failed -gt 0) { throw "$($report.Failed) display contract tests failed. See $ResultsPath" }
