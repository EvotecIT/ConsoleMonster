Describe 'Interactive cmdlets' {
    BeforeAll {
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        Import-Module $script:ModulePath -Force

        # Runs a script in a child process of the same runtime with redirected input and output, so prompts cannot reach a
        # user. Extra environment variables can switch modes (CONSOLEMONSTER_ACCESSIBLE) or feed scripted keys.
        function Invoke-PromptChild {
            param(
                [Parameter(Mandatory)][string] $Script,
                [string] $InputText,
                [switch] $NonInteractive
            )
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_ACCESSIBLE', 'CONSOLEMONSTER_NONINTERACTIVE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add($Script)
            $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(($lines -join "`n")))
            $arguments = @('-NoLogo', '-NoProfile')
            if ($NonInteractive) { $arguments += '-NonInteractive' }
            $arguments += @('-EncodedCommand', $encoded)
            $info = [System.Diagnostics.ProcessStartInfo]::new($script:Runtime, ($arguments -join ' '))
            $info.RedirectStandardInput = $true
            $info.RedirectStandardOutput = $true
            $info.RedirectStandardError = $true
            $info.UseShellExecute = $false
            $process = [System.Diagnostics.Process]::Start($info)
            if ($InputText) { $process.StandardInput.Write($InputText) }
            $process.StandardInput.Close()
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit(60000)) { $process.Kill(); throw 'The child process did not finish (a prompt waited for input).' }
            ($stdout.Result + $stderr.Result) -replace "`r", ''
        }
    }

    It 'formats ordered command arguments with native types and shared escaping' {
        Import-Module $script:ModulePath -Force
        $arguments = [ordered] @{
            ComputerName = @('WEB01', "O'Brien lab")
            ThrottleLimit = 3
            WhatIf = $true
            Notify = $false
            Empty = ''
            Missing = $null
        }
        $command = Format-ConsoleCommand -Command Invoke-FleetCheck -Arguments $arguments
        $command | Should -BeExactly "Invoke-FleetCheck -ComputerName WEB01,'O''Brien lab' -ThrottleLimit 3 -WhatIf"
    }

    It 'formats native switch parameters as switches without collapsing case-sensitive names' {
        Import-Module $script:ModulePath -Force
        $arguments = [ordered] @{ WhatIf = [switch]$false; Confirm = [switch]$true }
        Format-ConsoleCommand -Command Invoke-FleetCheck -Arguments $arguments | Should -BeExactly 'Invoke-FleetCheck -Confirm'
        $options = [Collections.Specialized.OrderedDictionary]::new([StringComparer]::Ordinal)
        $options.Add('o', 'app')
        $options.Add('O', [switch]$true)
        Format-ConsoleCommand -Command cc -Arguments $options -Style Posix | Should -BeExactly 'cc -o app -O'
    }

    It 'prints initialized prompts without returning answers or reading input in snapshot mode' {
        $body = @'
$profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(80, 30, 'None', 'Ascii')
$target = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Output.RecordingTarget'))::new($profile)
Use-ConsoleTarget -Target $target {
    $answers = @(
        Read-ConsoleText 'Project' -Default orbit -StaticSnapshot
        Read-ConsoleText 'Notes' -Multiline -Default 'one' -StaticSnapshot
        Read-ConsoleText -Title 'Token' -StaticSnapshot -AsSecret
        Read-ConsoleNumber 'Port' -Default 443 -Integer -StaticSnapshot
        Read-ConsolePath 'Folder' -Default './orbit' -StaticSnapshot
        Read-ConsoleConfirm 'Continue?' -Default $true -StaticSnapshot
        Read-ConsoleConfirm 'Remove?' -TypedConfirmation orbit -StaticSnapshot
        Read-ConsoleSelect 'Target' -Choices api, worker -StaticSnapshot
        Read-ConsoleSelect -Title 'Checks' -Choices dns, tls -StaticSnapshot -Multiple
    )
    if ($answers.Count) { throw 'A prompt snapshot returned an answer' }
}
if (([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired) { throw 'A snapshot retained the input lease' }
foreach ($title in 'Project', 'Notes', 'Token', 'Port', 'Folder', 'Continue?', 'Remove?', 'Target', 'Checks') {
    if (-not $target.Output.Contains($title)) { throw "Missing prompt: $title" }
}
'SNAPSHOTS_OK'
'@
        Invoke-PromptChild -Script $body | Should -Match 'SNAPSHOTS_OK'
    }

    Context 'Without a user (redirected input, -NonInteractive)' {
        It 'rejects 300 unexpected objects once for commands that do not accept pipeline input' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
$commands = 'Get-ConsoleCapability', 'Get-ConsoleDemo', 'New-ConsoleActivityList', 'New-ConsoleColumnFormat', 'New-ConsoleFooterBar', 'New-ConsoleMenuItem', 'New-ConsoleProgress', 'New-ConsoleRule', 'New-ConsoleSimulatedClock', 'New-ConsoleSummaryBar', 'Show-ConsoleOverview', 'Show-ConsoleStatusLine', 'Add-ConsoleRecordingMarker', 'Clear-ConsoleScreen', 'Invoke-ConsolePester', 'Stop-ConsoleRecording', 'Read-ConsoleConfirm', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleText'
$service = Get-Service | Select-Object -First 1
foreach ($command in $commands) {
    $Error.Clear()
    try { 1..300 | ForEach-Object { $service } | & $command -ErrorAction Stop | Out-Null }
    catch { "RESULT=$command ID=$($_.FullyQualifiedErrorId.Split(',')[0]) COUNT=$($Error.Count)" }
}
'@
            foreach ($command in 'Get-ConsoleCapability', 'Get-ConsoleDemo', 'New-ConsoleActivityList', 'New-ConsoleColumnFormat', 'New-ConsoleFooterBar', 'New-ConsoleMenuItem', 'New-ConsoleProgress', 'New-ConsoleRule', 'New-ConsoleSimulatedClock', 'New-ConsoleSummaryBar', 'Show-ConsoleOverview', 'Show-ConsoleStatusLine', 'Add-ConsoleRecordingMarker', 'Clear-ConsoleScreen', 'Invoke-ConsolePester', 'Stop-ConsoleRecording', 'Read-ConsoleConfirm', 'Read-ConsoleNumber', 'Read-ConsolePath', 'Read-ConsoleText') {
                $text | Should -Match ([regex]::Escape("RESULT=$command ID=UnexpectedPipelineInput COUNT=1"))
            }
        }

        It 'rejects invalid tabs once and still accepts a pipeline of pages' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
try { 1..300 | New-ConsoleTabs -ErrorAction Stop | Out-Null } catch { "ERROR=$($_.FullyQualifiedErrorId.Split(',')[0])" }
$tabs = @(New-ConsoleTab -Title First; New-ConsoleTab -Title Second) | New-ConsoleTabs
"TYPE=$($tabs.GetType().Name)"
'@
            $text | Should -Match 'ERROR=ExpectedConsoleTab'
            $text | Should -Match 'TYPE=Tabs'
        }

        It 'reports a missing dashboard once for a pipeline of log entries' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
$errors = @()
1..300 | Write-ConsoleLogEntry -ErrorAction SilentlyContinue -ErrorVariable +errors
"ERRORS=$($errors.Count) ID=$($errors[0].FullyQualifiedErrorId.Split(',')[0])"
'@
            $text | Should -Match 'ERRORS=1 ID=ConsoleMonsterNoSession'
        }

        It 'supports optional prompt titles' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
$one = Read-ConsoleSelect -Choices 'alpha', 'beta' -Default 'beta'
$two = Read-ConsoleText -Title 'Name' -Default 'worker'
"ONE=$one TWO=$two"
'@
            $text | Should -Match 'ONE=beta TWO=worker'
        }

        It 'renders one explorer for multiple piped objects' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
$values = @([pscustomobject]@{Name='alpha'}, [pscustomobject]@{Name='beta'})
$result = @($values | Show-ConsoleObject -AsString -Depth 2)
"COUNT=$($result.Count)"
$result
'@
            $text | Should -Match 'COUNT=1'
            $text | Should -Match 'alpha'
            $text | Should -Match 'beta'
        }

        It 'returns the default instead of waiting' {
            $text = Invoke-PromptChild -Script @'
$v = Read-ConsoleSelect 'Environment' -Choices 'Production', 'Staging' -Default 'Staging'
"value=$v"
$c = Read-ConsoleConfirm 'Go?' -Default $true
"confirm=$c"
$n = Read-ConsoleNumber 'Count' -Integer -Default 5
"number=$n type=$($n.GetType().Name)"
$t = Read-ConsoleText 'Name' -Default 'srv01'
"text=$t"
'@
            $text | Should -Match 'value=Staging'
            $text | Should -Match 'confirm=True'
            $text | Should -Match 'number=5 type=Int32'
            $text | Should -Match 'text=srv01'
        }

        It 'fails with a message that names the parameter to pass' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
try {
    Read-ConsoleSelect 'Environment' -Choices 'Production', 'Staging' -ParameterName '-Environment' -ErrorAction Stop
} catch {
    "id=$($_.FullyQualifiedErrorId.Split(',')[0])"
    "message=$($_.Exception.Message)"
}
'@
            $text | Should -Match 'id=PromptNeedsValue'
            $text | Should -Match "message=Prompt 'Environment' needs a value in a non-interactive session; pass -Environment or set a default\."
        }

        It 'returns the chosen objects themselves, not their text' {
            $text = Invoke-PromptChild -Script @'
$items = @([pscustomobject] @{ Name = 'a'; Size = 1 }, [pscustomobject] @{ Name = 'b'; Size = 2 }, [pscustomobject] @{ Name = 'c'; Size = 3 })
$picked = @(Read-ConsoleSelect -Title 'Items' -Choices $items -Display Name -Multiple -Default 'a', 'c')
"count=$($picked.Count) sizes=$(($picked | ForEach-Object Size) -join ',') type=$($picked[0].GetType().Name)"
$one = $items | Read-ConsoleSelect 'One' -Display Name -Default 'b'
"one=$($one.Size)"
'@
            $text | Should -Match 'count=2 sizes=1,3 type=PSCustomObject'
            $text | Should -Match 'one=2'
        }

        It 'fills a form from its defaults and reports the first field that needs a value' {
            $text = Invoke-PromptChild -Script @'
$form = New-ConsoleForm 'Share' -Command 'New-Share' -Content {
    New-ConsoleField Name 'Share name' -Default 'Reports' -ParameterName Name
    New-ConsoleField ReadOnly 'Read only' -Type Toggle -Default $true -ParameterName ReadOnly
    New-ConsoleField Audit 'Audit access' -Type Toggle -ParameterName Audit
}
$answers = Read-ConsoleForm $form
"name=$($answers.Name) readonly=$($answers.ReadOnly)"
$result = Read-ConsoleForm $form -PassThru
"command=$($result.CommandLine)"
$missing = New-ConsoleForm 'Share' -Content { New-ConsoleField Path 'Folder' -Required -ParameterName Path }
try { Read-ConsoleForm $missing -ErrorAction Stop } catch { "error=$($_.Exception.Message)" }
'@
            $text | Should -Match 'name=Reports readonly=True'
            $text | Should -Match 'command=New-Share\n'
            $text | Should -Match "error=Prompt 'Folder' needs a value in a non-interactive session; pass -Path or set a default\."
        }

        It 'refuses to show a menu without a user' {
            $text = Invoke-PromptChild -Script @'
$menu = New-ConsoleMenu 'Tool' -Content { New-ConsoleMenuItem 'Run' -Action { 'ran' } }
try { Show-ConsoleMenu $menu -ErrorAction Stop } catch { "error=$($_.Exception.Message)" }
'@
            $text | Should -Match "error=Menu 'Tool' needs an interactive session\."
        }
    }

    Context 'Accessible and line modes' {
        It 'labels service choices by identity while returning the service itself' {
            $text = Invoke-PromptChild -InputText "1`n" -Script @'
$service = Get-Service | Select-Object -First 1
$answer = $service | Read-ConsoleSelect -Title 'Service' -Mode Accessible
"EXPECTED=$($service.Name)"
"ANSWER=$($answer.Name)"
'@
            $name = [regex]::Match($text, '(?m)^EXPECTED=(.+)$').Groups[1].Value.Trim()
            $name | Should -Not -BeNullOrEmpty
            $text | Should -Match ([regex]::Escape("1) $name"))
            $text | Should -Match ([regex]::Escape("ANSWER=$name"))
            $text | Should -Not -Match 'System\.ServiceProcess\.ServiceController'
        }

        It 'collects pipeline objects once and returns the selected object' {
            $text = Invoke-PromptChild -InputText "2`n" -Script @'
$items = @([pscustomobject]@{ Name = 'alpha'; Number = 1 }, [pscustomobject]@{ Name = 'beta'; Number = 2 })
$picked = $items | Show-ConsoleMenu -Mode Accessible
"picked=$($picked.Name):$($picked.Number)"
'@
            $text | Should -Match '1\) alpha'
            $text | Should -Match '2\) beta'
            $text | Should -Match 'picked=beta:2'
        }

        It 'emits one terminating error for a 300-object pipeline without an interactive user' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
$Error.Clear()
try { 1..300 | ForEach-Object { [pscustomobject]@{ Name = "item$_" } } | Show-ConsoleMenu -Mode None -ErrorAction Stop }
catch { "id=$($_.FullyQualifiedErrorId.Split(',')[0])" }
"errors=$($Error.Count)"
'@
            $text | Should -Match 'id=PromptNeedsValue'
            $text | Should -Match 'errors=1'
        }

        It 'rejects non-form pipeline objects once and names the selection command' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
$Error.Clear()
try { 1..300 | Read-ConsoleForm -ErrorAction Stop }
catch { "id=$($_.FullyQualifiedErrorId.Split(',')[0]) message=$($_.Exception.Message)" }
"errors=$($Error.Count)"
'@
            $text | Should -Match 'id=ExpectedConsoleForm'
            $text | Should -Match 'Read-ConsoleSelect'
            $text | Should -Match 'errors=1'
        }

        It 'accepts forms from the pipeline' {
            $text = Invoke-PromptChild -NonInteractive -Script @'
$form = New-ConsoleForm 'Test' -Content { New-ConsoleField Name 'Name' -Default 'one' }
$answer = $form | Read-ConsoleForm -Mode None
"name=$($answer.Name)"
'@
            $text | Should -Match 'name=one'
        }

        It 'returns multiple original objects only when multiple selection is requested' {
            $text = Invoke-PromptChild -InputText "1,3`n" -Script @'
$items = 1..3 | ForEach-Object { [pscustomobject]@{ Name = "item$_"; Number = $_ } }
$picked = @($items | Show-ConsoleMenu -Mode Accessible -Multiple)
"picked=$(($picked | ForEach-Object Number) -join ',')"
'@
            $text | Should -Match 'picked=1,3'
        }

        It 'numbers the choices and reads the answer as a line' {
            $text = Invoke-PromptChild -InputText "9`n2`n" -Script @'
$v = Read-ConsoleSelect 'Environment' -Choices 'Production', 'Staging', 'Lab' -Mode Accessible
"value=$v"
'@
            $text | Should -Match '1\) Production'
            $text | Should -Match '3\) Lab'
            $text | Should -Match "'9' is not one of the numbers\."
            $text | Should -Match 'selected: Staging'
            $text | Should -Match 'value=Staging'
        }

        It 'reads several lines with -Multiline until an empty line' {
            $text = Invoke-PromptChild -InputText "first`nsecond`n`n" -Script @'
$v = Read-ConsoleText 'Reason' -Multiline -Mode Accessible
"value=$($v -replace "`n", '|')"
'@
            $text | Should -Match 'value=first\|second'
        }

        It 'uses the prompt vocabulary for field variants' {
            (New-ConsoleField Pick -Type Select -Choices 'a', 'b').Multiple | Should -BeFalse
            (New-ConsoleField Pick -Type Select -Multiple -Choices 'a', 'b').Multiple | Should -BeTrue
            (New-ConsoleField Password -Type Text -AsSecret).GetType().Name | Should -Be 'SecretField'
            (New-ConsoleField Name).GetType().Name | Should -Be 'TextField'
            { New-ConsoleField Pick -Type Text -Multiple } | Should -Throw '*Multiple applies to -Type Select*'
            { New-ConsoleField Port -Type Number -AsSecret } | Should -Throw '*AsSecret applies to -Type Text*'
            { New-ConsoleField Note -Type Text -AsSecret -Multiline } | Should -Throw '*Choose AsSecret or Multiline*'
            { New-ConsoleField Note -MaxLines 3 } | Should -Throw '*-Type Text -Multiline*'
            [Enum]::GetNames((Get-Command New-ConsoleField).Parameters['Type'].ParameterType) | Should -Be @('Text', 'Number', 'Toggle', 'Select', 'Path')
        }

        It 'creates a text area field' {
            $field = New-ConsoleField Reason 'Reason' -Type Text -Multiline -MaxLines 3 -Height 5
            $field.GetType().Name | Should -Be 'TextAreaField'
            $field.MaxLines | Should -Be 3
        }

        It 'accepts several numbers for a multi-selection' {
            $text = Invoke-PromptChild -InputText "1,3`n" -Script @'
$v = @(Read-ConsoleSelect -Title 'Pick' -Choices 'a', 'b', 'c' -Mode Accessible -Multiple)
"value=$($v -join '+')"
'@
            $text | Should -Match 'value=a\+c'
        }
    }

    Context 'Scripted keys (the rich prompts without a terminal)' {
        It 'filters real service objects by the identity shown in the picker' {
            $text = Invoke-PromptChild -Script @'
$services = @(Get-Service | Select-Object -First 2)
$target = $services[1]
$keys = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"' + $target.Name + '" Enter')
$keys.Paced = $true
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = $keys
$picked = $services | Read-ConsoleSelect -Title 'Services'
"EXPECTED=$($target.Name) PICKED=$($picked.Name)"
'@
            $text | Should -Match 'EXPECTED=(\S+) PICKED=\1'
            $text | Should -Not -Match 'System\.ServiceProcess\.ServiceController'
        }

        It 'drives a selection and a multi-selection with keys' {
            $text = Invoke-PromptChild -Script @'
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Down Enter')
$v = Read-ConsoleSelect 'Pick' -Choices 'alpha', 'beta', 'gamma'
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Space Down Down Space Enter')
$m = @(Read-ConsoleSelect -Title 'Pick' -Choices 'alpha', 'beta', 'gamma' -Multiple)
$keys = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"bad" Enter Ctrl+U "good" Enter'); $keys.Paced = $true
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = $keys
$t = Read-ConsoleText 'Name' -Validator { if ($_ -eq 'bad') { 'not that one' } }
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"s3cret" Enter')
$s = Read-ConsoleText -Title 'Token' -AsSecret
"value=$v multi=$($m -join '+') text=$t secret=$($s.GetType().Name) length=$($s.Length)"
'@
            $text | Should -Match 'value=beta multi=alpha\+gamma text=good secret=SecureString length=6'
            $text | Should -Match 'not that one'
        }

        It 'returns the original objects chosen in Show-ConsoleGrid' {
            $text = Invoke-PromptChild -Script @'
$rows = 1..12 | ForEach-Object { [pscustomobject] @{ Name = 'SRV{0:00}' -f $_; Role = @('web', 'sql', 'dc')[$_ % 3]; Cpu = ($_ * 37) % 100 } }
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('"sql" Space Down Space Ctrl+Enter')
$picked = @($rows | Show-ConsoleGrid -Title 'Servers')
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Ctrl+Home Ctrl+Enter')
$top = @($rows | Show-ConsoleGrid -Property Name, Cpu -SortBy Cpu -Descending -OutputMode Single)
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Ctrl+Enter')
$none = @($rows | Show-ConsoleGrid -OutputMode None)
"picked=$($picked.Name -join '+') type=$($picked[0].GetType().Name) top=$($top.Name) none=$($none.Count)"
try { $rows | Show-ConsoleGrid -Mode None -ErrorAction Stop } catch { "error=$($_.FullyQualifiedErrorId.Split(',')[0])" }
'@
            $text | Should -Match 'picked=SRV01\+SRV04 type=PSCustomObject top=SRV08 none=0'
            $text | Should -Match 'error=Prompt'
        }

        It 'prints the rows as a table with a warning when no one can pick, and returns nothing' {
            . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "Import-Module '$(Join-Path $PSScriptRoot '..\ConsoleMonster.psd1')' -Force"
                "`$rows = @([pscustomobject] @{ Name = 'alpha'; Role = 'web' }, [pscustomobject] @{ Name = 'beta'; Role = 'sql' })"
                "`$picked = @(`$rows | Show-ConsoleGrid -Title 'Servers' -WarningVariable warned -WarningAction SilentlyContinue)"
                "'PICKED=' + `$picked.Count"
                "'WARNED=' + (`$warned -join ' ')"
            )
            $text = (Invoke-TestChild -Runtime ([System.Diagnostics.Process]::GetCurrentProcess().Path) -Script ($lines -join "`n")) -join "`n"
            $text | Should -Match 'alpha'
            $text | Should -Match 'beta'
            $text | Should -Match 'PICKED=0'
            $text | Should -Match 'WARNED=.*interactive console'
        }

        It 'cancels with a PromptCanceled error on Esc' {
            $text = Invoke-PromptChild -Script @'
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Escape')
try { Read-ConsoleSelect 'Pick' -Choices 'a', 'b' -ErrorAction Stop } catch { "id=$($_.FullyQualifiedErrorId.Split(',')[0])" }
'@
            $text | Should -Match 'id=PromptCanceled'
        }

        It 'applies a built-in menu preset from the presets list and shows it in the review command' {
            # A fixed screen captured in memory: the preset list with its preview, then the form's review with the command.
            $text = Invoke-PromptChild -Script @'
$keys = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('wait:200 Enter wait:200 Down wait:200 Enter wait:200 Escape wait:200 Down wait:200 Enter wait:200 PageDown wait:400'); $keys.RealTime = $true
$options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default
$options.Input = $keys; $options.Mode = 'Rich'
$capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(120, 30, $false); $options.Capture = $capture
$options.Profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(120, 30, 'TrueColor', 'Full')
$form = New-ConsoleForm 'Backup' -Command 'Start-AppBackup' -Content {
    New-ConsoleField Target 'Target' -Type Select -Choices 'Daily', 'Weekly' -Default 'Daily' -ParameterName Target
    New-ConsoleField Copies 'Copies' -Type Number -Integer -Default 1 -ParameterName Copies
}
$menu = New-ConsoleMenu 'Backup helper' -Content {
    [pscustomobject]@{ Name = 'Quick'; Form = $form; Values = @{ Copies = 2 }; Preview = 'quick preview' }
    [pscustomobject]@{ Name = 'Sunday'; Form = $form; Values = @{ Target = 'Weekly'; Copies = 4 }; Preview = { New-ConsoleText 'sunday preview' } }
    New-ConsoleMenuItem 'Presets' -Presets
    New-ConsoleMenuItem 'Back up' -Form $form
    New-ConsoleMenuItem 'Quit' -Exit
}
# The keys end on the review, which ends the menu with an input error.
try { Show-ConsoleMenu $menu -Layout Shell -StorageRoot ([IO.Path]::GetTempPath()) -ErrorAction Stop } catch { 'ended=' + $_.FullyQualifiedErrorId.Split(',')[0] }
$frames = @($capture.Frames | ForEach-Object { $_.ToPlainText() })
foreach ($phrase in 'built in', 'sunday preview', "Preset 'Sunday' applied", 'Start-AppBackup -Target Weekly -Copies 4') {
    if ($frames | Where-Object { $_.Contains($phrase) }) { "saw=$phrase" } else { "missing=$phrase" }
}
'@
            $text | Should -Match 'saw=built in'
            $text | Should -Match 'saw=sunday preview'
            $text | Should -Match "saw=Preset 'Sunday' applied"
            $text | Should -Match 'saw=Start-AppBackup -Target Weekly -Copies 4'
        }

        It 'runs a menu run on a simulated clock whose result takes the clock''s time' {
            $text = Invoke-PromptChild -Script @'
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('r q')
$script:result = $null
$menu = New-ConsoleMenu 'Tool' -Content {
    New-ConsoleMenuItem 'Run' -Hotkey r -Action {
        $clock = New-ConsoleSimulatedClock -Speed 0
        $dashboard = New-ConsoleDashboard -Title 'Checks' -Content { New-ConsoleSummaryBar -Total 2; New-ConsoleActivityList -WidgetId running }
        $script:result = $MenuContext.Run($dashboard, {
            Update-Console -WidgetId running -Id 1 -Activity 'first check'
            $clock.Wait(42)
            Update-Console -Counter @{ passed = 2 } -WidgetId 'summary' -Value 2
            New-ConsoleMenuRunResult 'Checks finished'
        }, $clock)
    }
    New-ConsoleMenuItem 'Quit' -Hotkey q -Exit
}
Show-ConsoleMenu $menu -Layout Inline
"elapsed=$($script:result.Elapsed) status=$($script:result.Status)"
'@
            # The result was given no -Elapsed: its time comes from the session clock, which is the simulated one.
            $text | Should -Match 'elapsed=00:00:42 status=Pass'
            # The printed result shows that time next to its title.
            $text | Should -Match 'Checks finished\s+00:00:42'
        }

        It 'runs a menu whose scriptblock actions see the script scope' {
            $text = Invoke-PromptChild -Script @'
$script:log = [System.Collections.Generic.List[string]]::new()
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('b q')
$menu = New-ConsoleMenu 'Tool' -Content {
    New-ConsoleMenuItem 'Build' -Hotkey b -Action { $script:log.Add('built ' + $MenuContext.Item.Label) }
    New-ConsoleMenuItem 'Quit' -Hotkey q -Exit
}
Show-ConsoleMenu $menu -Layout Inline
"log=$($script:log -join ';')"
'@
            $text | Should -Match 'log=built Build'
        }
    }

    Context 'Choices, presets, run results and clocks (no screen)' {
        BeforeAll {
            $script:Checks = @(
                [pscustomobject] @{ Id = 'TimeOffset'; Name = 'Time offset'; Category = 'Identity'; Seconds = 0.8; Retired = $null }
                [pscustomobject] @{ Id = 'FreeSpace'; Name = 'Free space'; Category = 'Storage'; Seconds = 1.3; Retired = 'replaced by Volume health' }
                [pscustomobject] @{ Id = 'CertChain'; Name = 'Cert chain'; Category = 'Certificates'; Seconds = 3.5; Retired = $null }
            )
        }

        It 'builds choices with a muted badge, a disabled reason, a selection and a command-line value that keeps the object''s properties' {
            $field = New-ConsoleField Checks 'Checks' -Type Select -Multiple -Choices $script:Checks -Display Name -CommandValue Id -GroupBy Category -Badge { "~$($_.Seconds) s" } -DisabledReason Retired -Selected { $_.Id -eq 'CertChain' } -ParameterName Check
            $choices = @($field.Choices)
            ($choices | ForEach-Object Label) -join ',' | Should -Be 'Time offset,Free space,Cert chain'
            ($choices | ForEach-Object { [string] $_.Value }) -join ',' | Should -Be 'TimeOffset,FreeSpace,CertChain'
            $choices[0].Value.Category | Should -Be 'Identity'
            $choices[0].Badge.PlainText | Should -Be '~0.8 s'
            $choices[0].Badge.ToMarkup() | Should -Match '(?i)muted'
            $choices[1].DisabledReason | Should -Be 'replaced by Volume health'
            $choices[0].DisabledReason | Should -BeNullOrEmpty
            @($choices | Where-Object Selected).Label | Should -Be 'Cert chain'
        }

        It 'marks loaded choices with property names: badge, disabled reason and selection' {
            $domain = New-ConsoleField Domain 'Domain' -Type Select -Choices 'corp.example.test' -Default 'corp.example.test' -ParameterName Domain
            $servers = New-ConsoleField Servers 'Servers' -Type Select -Multiple -DependsOn Domain -ParameterName ComputerName -LoadChoices {
                param($Values)
                [pscustomobject] @{ Name = 'SRV01'; Role = 'Web'; Offline = $null; Online = $true; Domain = $Values.Domain }
                [pscustomobject] @{ Name = 'SRV02'; Role = 'File'; Offline = 'offline since 07:12'; Online = $false; Domain = $Values.Domain }
            } -Display Name -CommandValue Name -Badge Role -DisabledReason Offline -Selected Online
            $form = New-ConsoleForm 'Fleet' -Field $domain, $servers
            $loaded = @($servers.LoadChoices.Invoke($form.GetValues(), [System.Threading.CancellationToken]::None).GetAwaiter().GetResult())
            $loaded.Count | Should -Be 2
            $loaded[0].Badge.PlainText | Should -Be 'Web'
            $loaded[0].Selected | Should -BeTrue
            $loaded[1].Selected | Should -BeFalse
            $loaded[1].DisabledReason | Should -Be 'offline since 07:12'
            [string] $loaded[1].Value | Should -Be 'SRV02'
            $loaded[1].Value.Domain | Should -Be 'corp.example.test'
        }

        It 'rejects calculated selectors for loaded choices instead of silently dropping their metadata' {
            { New-ConsoleField -Key Server -Type Select -LoadChoices { 'api' } -Describe { "Server $_" } -ErrorAction Stop } | Should -Throw '*property names*'
            { New-ConsoleMenuItem -Label Targets -LoadChoices { 'api' } -Describe { "Server $_" } -ErrorAction Stop } | Should -Throw '*property names*'
        }

        It 'omits an unchanged loaded command value from the equivalent command' {
            $field = New-ConsoleField -Key Server -Type Select -Default SRV01 -Display Name -CommandValue Name -ParameterName ComputerName -LoadChoices {
                [pscustomobject] @{ Name = 'SRV01'; Role = 'Web' }
                [pscustomobject] @{ Name = 'SRV02'; Role = 'Database' }
            }
            $form = New-ConsoleForm -Title Fleet -Command Invoke-FleetCheck -Field $field
            $loaded = @($field.LoadChoices.Invoke($form.GetValues(), [Threading.CancellationToken]::None).GetAwaiter().GetResult())
            foreach ($choice in $loaded) { $field.Choices.Add($choice) }
            $field.LoadChoices = $null
            # Applying a preset resolves its scalar key to the loaded value, retaining its properties.
            Set-ConsoleFormValue -Form $form -Value @{ Server = 'SRV01' }
            $form.BuildCommand() | Should -BeExactly 'Invoke-FleetCheck'
            $field.Value.Role | Should -Be 'Web'
        }

        It 'applies page descriptions while preserving the form description and defaults for other pages' {
            $fields = @(
                New-ConsoleField -Key Domain -Label Domain -Page Scope
                New-ConsoleField -Key Checks -Label Checks -Page Checks
                New-ConsoleField -Key Parallel -Label Parallel -Page Options
            )
            $form = New-ConsoleForm -Title Fleet -Description 'Choose the scope, checks and run options.' -Field $fields -PageDescriptions @{
                Scope = 'Which servers to check.'
                Checks = 'What a healthy server reports appears in the preview.'
            }

            ($form.Pages | ForEach-Object -MemberName Title) -join ',' | Should -Be 'Scope,Checks,Options'
            $form.Pages[0].Description | Should -Be 'Which servers to check.'
            $form.Pages[1].Description | Should -Be 'What a healthy server reports appears in the preview.'
            $form.Pages[2].Description | Should -BeNullOrEmpty
            $form.Description | Should -Be 'Choose the scope, checks and run options.'

            $defaults = New-ConsoleForm -Title Fleet -Field $fields
            $defaults.Pages[0].Description | Should -BeNullOrEmpty
        }

        It 'gives a form built-in presets from its scriptblock and -Preset, with values matched to its choices' {
            $extra = [pscustomobject]@{ Name = 'Storage only'; Values = @{ Checks = 'Free space' }; Description = 'Only the storage check.' }
            $form = New-ConsoleForm 'Fleet' -Command 'Invoke-FleetCheck' -Preset $extra -Content {
                New-ConsoleField Checks 'Checks' -Type Select -Multiple -Choices $script:Checks -Display Name -CommandValue Id -ParameterName Check
                New-ConsoleField Parallel 'Servers at a time' -Type Number -Integer -Default 4 -ParameterName ThrottleLimit
                [pscustomobject]@{ Name = 'Nightly'; Values = @{ Checks = 'TimeOffset', 'Cert chain'; Parallel = 8 }; Preview = { 'two checks, eight at a time' } }
            }
            ($form.Presets | ForEach-Object Name) -join ',' | Should -Be 'Storage only,Nightly'
            $form.Presets[0].Description | Should -Be 'Only the storage check.'
            $form.Presets[1].Preview | Should -Not -BeNullOrEmpty
            # A title names its choice: the preset holds the choice's value.
            (@($form.Presets[1].Values['Checks']) | ForEach-Object { [string] $_ }) -join ',' | Should -Be 'TimeOffset,CertChain'
            Set-ConsoleFormValue $form $form.Presets[1].Values
            $form.BuildCommand() | Should -Be 'Invoke-FleetCheck -Check TimeOffset,CertChain -ThrottleLimit 8'
        }

        It 'sets form values from a hashtable or an earlier run and refuses anything else' {
            $form = New-ConsoleForm 'Fleet' -Command 'Invoke-FleetCheck' -Content {
                New-ConsoleField Checks 'Checks' -Type Select -Multiple -Choices $script:Checks -Display Name -CommandValue Id -ParameterName Check
                New-ConsoleField Notify 'Email the report' -Type Toggle -ParameterName Notify
            }
            Set-ConsoleFormValue -Form $form -Values @{ Checks = @('Free space'); Notify = $true; Unknown = 1 }
            $form.BuildCommand() | Should -Be 'Invoke-FleetCheck -Check FreeSpace -Notify'
            $earlier = $form.GetValues()
            Set-ConsoleFormValue $form @{ Checks = @(); Notify = $false }
            $form.BuildCommand() | Should -Be 'Invoke-FleetCheck'
            Set-ConsoleFormValue $form $earlier
            $form.BuildCommand() | Should -Be 'Invoke-FleetCheck -Check FreeSpace -Notify'
            { Set-ConsoleFormValue $form 'text' -ErrorAction Stop } | Should -Throw '*hashtable*'
        }

        It 'creates menu presets with form values, selections and a preview, and adds them to the menu' {
            $form = New-ConsoleForm 'Fleet' -Content { New-ConsoleField Checks 'Checks' -Type Select -Multiple -Choices $script:Checks -Display Name -CommandValue Id }
            $targets = New-ConsoleMenuItem 'Targets' -MultiSelect ([pscustomobject] @{ Name = 'SRV01' }), ([pscustomobject] @{ Name = 'SRV02' }) -Display Name
            $first = [pscustomobject]@{ Name = 'First'; Form = $form; Values = @{ Checks = 'Time offset' }; Item = $targets; Selection = 'SRV02'; Description = 'The first wave.'; Preview = { New-ConsoleText 'preview' } }
            $first.Description | Should -Be 'The first wave.'
            $first.Preview | Should -Not -BeNullOrEmpty
            $second = [pscustomobject]@{ Name = 'Second'; Form = $form; Values = @{ Checks = 'FreeSpace' } }
            $menu = New-ConsoleMenu 'Fleet' -Item $targets -Preset $first -Content {
                $second
                New-ConsoleMenuItem 'Presets' -Presets
            }
            ($menu.Presets | ForEach-Object Name) -join ',' | Should -Be 'First,Second'
            $menu.Items.Count | Should -Be 2
            { New-ConsoleMenu 'Broken' -Preset @{ Name = 'Broken'; Values = @{ Checks = 'x' } } -ErrorAction Stop } | Should -Throw '*Form*'
            { New-ConsoleMenu 'Broken' -Preset @{ Name = 'Broken'; Selection = 'x' } -ErrorAction Stop } | Should -Throw '*Item*'
        }

        It 'keeps choice badges and disabled reasons for static and asynchronously loaded menu items' {
            $objects = @(
                [pscustomobject] @{ Name = 'api'; Role = 'Web'; Offline = '' }
                [pscustomobject] @{ Name = 'db'; Role = 'Data'; Offline = 'maintenance' }
            )
            $item = New-ConsoleMenuItem Targets -MultiSelect $objects -Display Name -ChoiceBadge Role -ChoiceDisabledReason Offline
            $item.Choices[0].Badge.PlainText | Should -Be 'Web'
            $item.Choices[0].IsEnabled | Should -BeTrue
            $item.Choices[1].DisabledReason | Should -Be 'maintenance'
            $loadedItem = New-ConsoleMenuItem Targets -LoadChoices {
                [pscustomobject] @{ Name = 'api'; Role = 'Web'; Offline = '' }
                [pscustomobject] @{ Name = 'db'; Role = 'Data'; Offline = 'maintenance' }
            } -Display Name -ChoiceBadge Role -ChoiceDisabledReason Offline -Selected api
            $loaded = @($loadedItem.LoadChoices.Invoke($null, [threading.cancellationtoken]::None).GetAwaiter().GetResult())
            $loaded[0].Badge.PlainText | Should -Be 'Web'
            $loaded[0].Selected | Should -BeTrue
            $loaded[1].DisabledReason | Should -Be 'maintenance'
            { New-ConsoleMenuItem Targets -LoadChoices { 'api' } -ChoiceBadge { $_.Role } -ErrorAction Stop } | Should -Throw '*property names*'
        }

        It 'applies several menu selections from one native preset' {
            $text = Invoke-PromptChild -Script @'
$targets = New-ConsoleMenuItem Targets -MultiSelect 'api', 'db'
$tasks = New-ConsoleMenuItem Tasks -MultiSelect 'health', 'logs'
$preset = [pscustomobject]@{ Name = 'Nightly'; Selections = @{ $targets = @('db'); $tasks = @('health', 'logs') } }
$menu = New-ConsoleMenu 'Multi-selection preset' -Item $targets, $tasks -Preset $preset -Content {
    New-ConsoleMenuItem Presets -Hotkey p -Presets
    New-ConsoleMenuItem Quit -Hotkey q -Exit
}
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('p Enter q')
Show-ConsoleMenu $menu -Layout Inline
"targets=$($targets.Selection -join ',') tasks=$($tasks.Selection -join ',')"
'@
            $text | Should -Match 'targets=db tasks=health,logs'
            { New-ConsoleMenu Invalid -Preset @{ Name = 'Invalid'; Selections = @{ missing = @('db') } } -ErrorAction Stop } | Should -Throw '*MenuItem*'
        }

        It 'preserves original object answers and defaults instead of choosing the first custom object' {
            $targets = @([pscustomobject] @{ Name = 'SRV01' }, [pscustomobject] @{ Name = 'SRV02' })
            $single = New-ConsoleField Target 'Target' -Type Select -Choices $targets -Display Name -Default $targets[1]
            $multiple = New-ConsoleField Targets 'Targets' -Type Select -Multiple -Choices $targets -Display Name -Default @($targets[1])
            $form = New-ConsoleForm 'Deployment' -Field $single, $multiple
            $form.GetValues()['Target'].Name | Should -Be 'SRV02'
            @($form.GetValues()['Targets']).Count | Should -Be 1
            $form.GetValues()['Targets'][0].Name | Should -Be 'SRV02'
            Set-ConsoleFormValue $form @{ Target = $targets[0] }
            Set-ConsoleFormValue $form @{ Target = $targets[1] }
            $form.GetValues()['Target'].Name | Should -Be 'SRV02'
        }

        It 'keeps original custom objects through form and menu presets' {
            $text = Invoke-PromptChild -Script @'
$targets = @([pscustomobject] @{ Name = 'SRV01' }, [pscustomobject] @{ Name = 'SRV02' })
$preset = [pscustomobject]@{ Name = 'Second'; Values = @{ Target = $targets[1] } }
$form = New-ConsoleForm 'Deployment' -Preset $preset -Content { New-ConsoleField Target 'Target' -Type Select -Choices $targets -Display Name }
Set-ConsoleFormValue $form $form.Presets[0].Values
"form=$($form.GetValues()['Target'].Name)"
$item = New-ConsoleMenuItem 'Targets' -MultiSelect $targets -Display Name
$menuPreset = [pscustomobject]@{ Name = 'Second'; Item = $item; Selection = $targets[1]; Form = $form; Values = @{ Target = $targets[1] } }
$menu = New-ConsoleMenu 'Deployment' -Item $item -Preset $menuPreset -Content {
    New-ConsoleMenuItem 'Presets' -Hotkey p -Presets
    New-ConsoleMenuItem 'Quit' -Hotkey q -Exit
}
([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions'))::Default.Input = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('p Enter q')
Show-ConsoleMenu $menu -Layout Inline
"menu=$(@($item.Selection | ForEach-Object Name) -join ',') formAfter=$($form.GetValues()['Target'].Name)"
'@
            $text | Should -Match 'form=SRV02'
            $text | Should -Match 'menu=SRV02 formAfter=SRV02'
        }

        It 'colours run result figures by status and keeps a given elapsed time' {
            $result = New-ConsoleMenuRunResult 'Checks finished' -Status Warn -Figure ([ordered] @{ passed = 41; warnings = 3; servers = 11 }) -FigureStatus @{ passed = 'Success'; Warnings = 'Warn' } -ElapsedSeconds 86
            $result.Elapsed | Should -Be ([timespan]::FromSeconds(86))
            ($result.Figures | ForEach-Object Key) -join ',' | Should -Be 'passed,warnings,servers'
            $result.FigureStatus['passed'] | Should -Be 'Pass'
            $result.FigureStatus['warnings'] | Should -Be 'Warn'
            $result.FigureStatus.ContainsKey('servers') | Should -BeFalse
            (New-ConsoleMenuRunResult 'Real time').Elapsed | Should -Be ([timespan]::Zero)
            { New-ConsoleMenuRunResult 'Bad' -Figure @{ failed = 1 } -FigureStatus @{ failed = 'Purple' } -ErrorAction Stop } | Should -Throw '*not a status*'
        }

        It 'moves a simulated clock without sleeping at speed 0, paces it at a speed, and converts it to session options' {
            $clock = New-ConsoleSimulatedClock -Speed 0
            $watch = [System.Diagnostics.Stopwatch]::StartNew()
            $clock.Wait(90)
            $clock.Advance(1.5)
            $clock.WaitUntil(10)
            $clock.Hold(5)
            $watch.ElapsedMilliseconds | Should -BeLessThan 1000
            $clock.Elapsed | Should -Be ([timespan]::FromSeconds(91.5))
            "$clock" | Should -Be '00:01:31'
            $options = [Management.Automation.LanguagePrimitives]::ConvertTo($clock, ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.LiveSessionOptions')))
            $options.Clock.Invoke() | Should -Be ([timespan]::FromSeconds(91.5))
            $paced = New-ConsoleSimulatedClock -Speed 20
            $watch.Restart()
            $paced.Wait(4)
            $watch.ElapsedMilliseconds | Should -BeGreaterOrEqual 150
            $paced.Seconds | Should -Be 4
        }
    }
}
