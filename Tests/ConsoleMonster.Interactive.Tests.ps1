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

        It 'cancels with a PromptCancelled error on Esc' {
            $text = Invoke-PromptChild -Script @'
[ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = [ConsoleMonster.Input.ScriptedInput]::new('Escape')
try { Read-ConsoleSelection 'Pick' -Choices 'a', 'b' -ErrorAction Stop } catch { "id=$($_.FullyQualifiedErrorId.Split(',')[0])" }
'@
            $text | Should -Match 'id=PromptCancelled'
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
}
