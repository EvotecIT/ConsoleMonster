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

    Context 'Without a user (redirected input, -NonInteractive)' {
        It 'returns the default instead of waiting' {
            $text = Invoke-PromptChild -Script @'
$v = Read-ConsoleSelection 'Environment' -Choices 'Production', 'Staging' -Default 'Staging'
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
    Read-ConsoleSelection 'Environment' -Choices 'Production', 'Staging' -ParameterName '-Environment' -ErrorAction Stop
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
$picked = @(Read-ConsoleMultiSelection 'Items' -Choices $items -Display Name -Selected 'a', 'c')
"count=$($picked.Count) sizes=$(($picked | ForEach-Object Size) -join ',') type=$($picked[0].GetType().Name)"
$one = $items | Read-ConsoleSelection 'One' -Display Name -Default 'b'
"one=$($one.Size)"
'@
            $text | Should -Match 'count=2 sizes=1,3 type=PSCustomObject'
            $text | Should -Match 'one=2'
        }

        It 'fills a form from its defaults and reports the first field that needs a value' {
            $text = Invoke-PromptChild -Script @'
$form = New-ConsoleForm 'Share' -Command 'New-Share' {
    New-ConsoleField Name 'Share name' -Default 'Reports' -Parameter Name
    New-ConsoleField ReadOnly 'Read only' -Type Toggle -Default $true -Parameter ReadOnly
    New-ConsoleField Audit 'Audit access' -Type Toggle -Parameter Audit
}
$answers = Read-ConsoleForm $form
"name=$($answers.Name) readonly=$($answers.ReadOnly)"
$result = Read-ConsoleForm $form -PassThru
"command=$($result.CommandLine)"
$missing = New-ConsoleForm 'Share' { New-ConsoleField Path 'Folder' -Required -Parameter Path }
try { Read-ConsoleForm $missing -ErrorAction Stop } catch { "error=$($_.Exception.Message)" }
'@
            $text | Should -Match 'name=Reports readonly=True'
            $text | Should -Match 'command=New-Share\n'
            $text | Should -Match "error=Prompt 'Folder' needs a value in a non-interactive session; pass -Path or set a default\."
        }

        It 'refuses to show a menu without a user' {
            $text = Invoke-PromptChild -Script @'
$menu = New-ConsoleMenu 'Tool' { New-ConsoleMenuItem 'Run' -Action { 'ran' } }
try { Show-ConsoleMenu $menu -ErrorAction Stop } catch { "error=$($_.Exception.Message)" }
'@
            $text | Should -Match "error=Menu 'Tool' needs an interactive session\."
        }
    }

    Context 'Accessible and line modes' {
        It 'numbers the choices and reads the answer as a line' {
            $text = Invoke-PromptChild -InputText "9`n2`n" -Script @'
$v = Read-ConsoleSelection 'Environment' -Choices 'Production', 'Staging', 'Lab' -Mode Accessible
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

        It 'creates a text area field' {
            $field = New-ConsoleField Reason 'Reason' -Type TextArea -MaxLines 3 -Height 5
            $field.GetType().Name | Should -Be 'TextAreaField'
            $field.MaxLines | Should -Be 3
        }

        It 'accepts several numbers for a multi-selection' {
            $text = Invoke-PromptChild -InputText "1,3`n" -Script @'
$v = @(Read-ConsoleMultiSelection 'Pick' -Choices 'a', 'b', 'c' -Mode Accessible)
"value=$($v -join '+')"
'@
            $text | Should -Match 'value=a\+c'
        }
    }

    Context 'Scripted keys (the rich prompts without a terminal)' {
        It 'drives a selection and a multi-selection with keys' {
            $text = Invoke-PromptChild -Script @'
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('Down Enter')
$v = Read-ConsoleSelection 'Pick' -Choices 'alpha', 'beta', 'gamma'
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('Space Down Down Space Enter')
$m = @(Read-ConsoleMultiSelection 'Pick' -Choices 'alpha', 'beta', 'gamma')
$keys = [ConsoleMonster.Input.ScriptedInput]::new('"bad" Enter Ctrl+U "good" Enter'); $keys.Paced = $true
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = $keys
$t = Read-ConsoleText 'Name' -Validate { if ($_ -eq 'bad') { 'not that one' } }
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('"s3cret" Enter')
$s = Read-ConsoleSecret 'Token'
"value=$v multi=$($m -join '+') text=$t secret=$($s.GetType().Name) length=$($s.Length)"
'@
            $text | Should -Match 'value=beta multi=alpha\+gamma text=good secret=SecureString length=6'
            $text | Should -Match 'not that one'
        }

        It 'returns the original objects chosen in Show-ConsoleGrid' {
            $text = Invoke-PromptChild -Script @'
$rows = 1..12 | ForEach-Object { [pscustomobject] @{ Name = 'SRV{0:00}' -f $_; Role = @('web', 'sql', 'dc')[$_ % 3]; Cpu = ($_ * 37) % 100 } }
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('"sql" Space Down Space Ctrl+Enter')
$picked = @($rows | Show-ConsoleGrid -Title 'Servers')
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('Ctrl+Home Ctrl+Enter')
$top = @($rows | Show-ConsoleGrid -Property Name, Cpu -SortBy Cpu -Descending -OutputMode Single)
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('Ctrl+Enter')
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

        It 'cancels with a PromptCancelled error on Esc' {
            $text = Invoke-PromptChild -Script @'
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('Escape')
try { Read-ConsoleSelection 'Pick' -Choices 'a', 'b' -ErrorAction Stop } catch { "id=$($_.FullyQualifiedErrorId.Split(',')[0])" }
'@
            $text | Should -Match 'id=PromptCancelled'
        }

        It 'applies a built-in menu preset from the presets list and shows it in the review command' {
            # A fixed screen captured in memory: the preset list with its preview, then the form's review with the command.
            $text = Invoke-PromptChild -Script @'
$keys = [ConsoleMonster.Input.ScriptedInput]::new('wait:200 Enter wait:200 Down wait:200 Enter wait:200 Escape wait:200 Down wait:200 Enter wait:200 PageDown wait:400'); $keys.RealTime = $true
$options = [ConsoleMonster.Interactive.InteractiveOptions]::Default
$options.Input = $keys; $options.Mode = 'Rich'
$capture = [ConsoleMonster.Interactive.ScreenCapture]::new(120, 30, $false); $options.Capture = $capture
$options.Profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed(120, 30, 'TrueColor', 'Full')
$form = New-ConsoleForm 'Backup' -Command 'Start-AppBackup' {
    New-ConsoleField Target 'Target' -Type Select -Choices 'Daily', 'Weekly' -Default 'Daily' -Parameter Target
    New-ConsoleField Copies 'Copies' -Type Number -Integer -Default 1 -Parameter Copies
}
$menu = New-ConsoleMenu 'Backup helper' {
    New-ConsoleMenuPreset 'Quick' -Form $form -Values @{ Copies = 2 } -Preview 'quick preview'
    New-ConsoleMenuPreset 'Sunday' -Form $form -Values @{ Target = 'Weekly'; Copies = 4 } -Preview { New-ConsoleText 'sunday preview' }
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
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('r q')
$script:result = $null
$menu = New-ConsoleMenu 'Tool' {
    New-ConsoleMenuItem 'Run' -Hotkey r -Action {
        $clock = New-ConsoleClock -Speed 0
        $dashboard = New-ConsoleDashboard -Title 'Checks' { New-ConsoleSummaryBar -Total 2; New-ConsoleActivityList -Name running }
        $script:result = $MenuContext.Run($dashboard, {
            Update-ConsoleActivity -Name running -Id 1 -Activity 'first check'
            $clock.Wait(42)
            Update-ConsoleSummaryBar -Done 2 -Counter @{ passed = 2 }
            New-ConsoleRunResult 'Checks finished'
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
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('b q')
$menu = New-ConsoleMenu 'Tool' {
    New-ConsoleMenuItem 'Build' -Hotkey b -Action { $script:log.Add('built ' + $MenuContext.Item.Title) }
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
            $field = New-ConsoleField Checks 'Checks' -Type MultiSelect -Choices $script:Checks -Display Name -CommandValue Id -GroupBy Category -Badge { "~$($_.Seconds) s" } -Disabled Retired -Selected { $_.Id -eq 'CertChain' } -Parameter Check
            $choices = @($field.Choices)
            ($choices | ForEach-Object Title) -join ',' | Should -Be 'Time offset,Free space,Cert chain'
            ($choices | ForEach-Object { [string] $_.Value }) -join ',' | Should -Be 'TimeOffset,FreeSpace,CertChain'
            $choices[0].Value.Category | Should -Be 'Identity'
            $choices[0].Badge.PlainText | Should -Be '~0.8 s'
            $choices[0].Badge.ToMarkup() | Should -Match '(?i)muted'
            $choices[1].DisabledReason | Should -Be 'replaced by Volume health'
            $choices[0].DisabledReason | Should -BeNullOrEmpty
            @($choices | Where-Object Selected).Title | Should -Be 'Cert chain'
        }

        It 'marks loaded choices with property names: badge, disabled reason and selection' {
            $domain = New-ConsoleField Domain 'Domain' -Type Select -Choices 'corp.example.test' -Default 'corp.example.test' -Parameter Domain
            $servers = New-ConsoleField Servers 'Servers' -Type MultiSelect -DependsOn Domain -Parameter ComputerName -LoadChoices {
                param($Values)
                [pscustomobject] @{ Name = 'SRV01'; Role = 'Web'; Offline = $null; Online = $true; Domain = $Values.Domain }
                [pscustomobject] @{ Name = 'SRV02'; Role = 'File'; Offline = 'offline since 07:12'; Online = $false; Domain = $Values.Domain }
            } -Display Name -CommandValue Name -Badge Role -Disabled Offline -Selected Online
            $form = New-ConsoleForm 'Fleet' -Field $domain, $servers
            $loaded = @($servers.Loader.Invoke($form.GetValues(), [System.Threading.CancellationToken]::None).GetAwaiter().GetResult())
            $loaded.Count | Should -Be 2
            $loaded[0].Badge.PlainText | Should -Be 'Web'
            $loaded[0].Selected | Should -BeTrue
            $loaded[1].Selected | Should -BeFalse
            $loaded[1].DisabledReason | Should -Be 'offline since 07:12'
            [string] $loaded[1].Value | Should -Be 'SRV02'
            $loaded[1].Value.Domain | Should -Be 'corp.example.test'
        }

        It 'gives a form built-in presets from its scriptblock and -Preset, with values matched to its choices' {
            $extra = New-ConsoleFormPreset 'Storage only' -Values @{ Checks = 'Free space' } -Description 'Only the storage check.'
            $form = New-ConsoleForm 'Fleet' -Command 'Invoke-FleetCheck' -Preset $extra {
                New-ConsoleField Checks 'Checks' -Type MultiSelect -Choices $script:Checks -Display Name -CommandValue Id -Parameter Check
                New-ConsoleField Parallel 'Servers at a time' -Type Number -Integer -Default 4 -Parameter ThrottleLimit
                New-ConsoleFormPreset 'Nightly' -Values @{ Checks = 'TimeOffset', 'Cert chain'; Parallel = 8 } -Preview { 'two checks, eight at a time' }
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
            $form = New-ConsoleForm 'Fleet' -Command 'Invoke-FleetCheck' {
                New-ConsoleField Checks 'Checks' -Type MultiSelect -Choices $script:Checks -Display Name -CommandValue Id -Parameter Check
                New-ConsoleField Notify 'Email the report' -Type Toggle -Parameter Notify
            }
            Set-ConsoleFormValue -Form $form -Value @{ Checks = @('Free space'); Notify = $true; Unknown = 1 }
            $form.BuildCommand() | Should -Be 'Invoke-FleetCheck -Check FreeSpace -Notify'
            $earlier = $form.GetValues()
            Set-ConsoleFormValue $form @{ Checks = @(); Notify = $false }
            $form.BuildCommand() | Should -Be 'Invoke-FleetCheck'
            Set-ConsoleFormValue $form $earlier
            $form.BuildCommand() | Should -Be 'Invoke-FleetCheck -Check FreeSpace -Notify'
            { Set-ConsoleFormValue $form 'text' -ErrorAction Stop } | Should -Throw '*hashtable*'
        }

        It 'creates menu presets with form values, selections and a preview, and adds them to the menu' {
            $form = New-ConsoleForm 'Fleet' { New-ConsoleField Checks 'Checks' -Type MultiSelect -Choices $script:Checks -Display Name -CommandValue Id }
            $targets = New-ConsoleMenuItem 'Targets' -MultiSelect ([pscustomobject] @{ Name = 'SRV01' }), ([pscustomobject] @{ Name = 'SRV02' }) -Display Name
            $first = New-ConsoleMenuPreset 'First' -Form $form -Values @{ Checks = 'Time offset' } -Item $targets -Selection 'SRV02' -Description 'The first wave.' -Preview { New-ConsoleText 'preview' }
            $first.Description | Should -Be 'The first wave.'
            $first.Preview | Should -Not -BeNullOrEmpty
            $second = New-ConsoleMenuPreset 'Second' -Form $form -Values @{ Checks = 'FreeSpace' }
            $menu = New-ConsoleMenu 'Fleet' -Item $targets -Preset $first {
                $second
                New-ConsoleMenuItem 'Presets' -Presets
            }
            ($menu.Presets | ForEach-Object Name) -join ',' | Should -Be 'First,Second'
            $menu.Items.Count | Should -Be 2
            { New-ConsoleMenuPreset 'Broken' -Values @{ Checks = 'x' } -ErrorAction Stop } | Should -Throw '*-Form*'
            { New-ConsoleMenuPreset 'Broken' -Selection 'x' -ErrorAction Stop } | Should -Throw '*-Item*'
        }

        It 'preserves original object answers and defaults instead of choosing the first custom object' {
            $targets = @([pscustomobject] @{ Name = 'SRV01' }, [pscustomobject] @{ Name = 'SRV02' })
            $single = New-ConsoleField Target 'Target' -Type Select -Choices $targets -Display Name -Default $targets[1]
            $multiple = New-ConsoleField Targets 'Targets' -Type MultiSelect -Choices $targets -Display Name -Default @($targets[1])
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
$preset = New-ConsoleFormPreset 'Second' -Values @{ Target = $targets[1] }
$form = New-ConsoleForm 'Deployment' -Preset $preset { New-ConsoleField Target 'Target' -Type Select -Choices $targets -Display Name }
Set-ConsoleFormValue $form $form.Presets[0].Values
"form=$($form.GetValues()['Target'].Name)"
$item = New-ConsoleMenuItem 'Targets' -MultiSelect $targets -Display Name
$menuPreset = New-ConsoleMenuPreset 'Second' -Item $item -Selection $targets[1] -Form $form -Values @{ Target = $targets[1] }
$menu = New-ConsoleMenu 'Deployment' -Item $item -Preset $menuPreset {
    New-ConsoleMenuItem 'Presets' -Hotkey p -Presets
    New-ConsoleMenuItem 'Quit' -Hotkey q -Exit
}
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('p Enter q')
Show-ConsoleMenu $menu -Layout Inline
"menu=$(@($item.Selection | ForEach-Object Name) -join ',') formAfter=$($form.GetValues()['Target'].Name)"
'@
            $text | Should -Match 'form=SRV02'
            $text | Should -Match 'menu=SRV02 formAfter=SRV02'
        }

        It 'colours run result figures by status and keeps a given elapsed time' {
            $result = New-ConsoleRunResult 'Checks finished' -Status Warn -Figure ([ordered] @{ passed = 41; warnings = 3; servers = 11 }) -FigureStatus @{ passed = 'Success'; Warnings = 'Warn' } -Elapsed 86s
            $result.Elapsed | Should -Be ([timespan]::FromSeconds(86))
            ($result.Figures | ForEach-Object Key) -join ',' | Should -Be 'passed,warnings,servers'
            $result.FigureStatus['passed'] | Should -Be 'Pass'
            $result.FigureStatus['warnings'] | Should -Be 'Warn'
            $result.FigureStatus.ContainsKey('servers') | Should -BeFalse
            (New-ConsoleRunResult 'Real time').Elapsed | Should -Be ([timespan]::Zero)
            { New-ConsoleRunResult 'Bad' -Figure @{ failed = 1 } -FigureStatus @{ failed = 'Purple' } -ErrorAction Stop } | Should -Throw '*not a status*'
        }

        It 'moves a simulated clock without sleeping at speed 0, paces it at a speed, and converts it to session options' {
            $clock = New-ConsoleClock -Speed 0
            $watch = [System.Diagnostics.Stopwatch]::StartNew()
            $clock.Wait(90)
            $clock.Advance(1.5)
            $clock.WaitUntil(10)
            $clock.Hold(5)
            $watch.ElapsedMilliseconds | Should -BeLessThan 1000
            $clock.Elapsed | Should -Be ([timespan]::FromSeconds(91.5))
            "$clock" | Should -Be '00:01:31'
            $options = [ConsoleMonster.Live.LiveSessionOptions] $clock
            $options.Clock.Invoke() | Should -Be ([timespan]::FromSeconds(91.5))
            $paced = New-ConsoleClock -Speed 20
            $watch.Restart()
            $paced.Wait(4)
            $watch.ElapsedMilliseconds | Should -BeGreaterOrEqual 150
            $paced.Seconds | Should -Be 4
        }
    }
}
