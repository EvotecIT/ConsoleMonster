# A complete installer: location, components, service, connection, review and progress.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 6)] [int] $Step = 0
)
Import-Module ConsoleMonster

# Data: invented installation inputs. The bounded driver only delays and updates widgets.
# No files, services, launch entries, network connections or persistent presets are created.
$installFolder = './toolbox-agent'
$dataFolder = './toolbox-data'
$components = @(
    [pscustomobject] @{ Id = 'Agent'; Name = 'Agent service'; Group = 'Core'; Size = '18 MB'; Description = 'Runs tasks and reports results.'; Selected = $true; Disabled = $null }
    [pscustomobject] @{ Id = 'Cli'; Name = 'Command-line tools'; Group = 'Core'; Size = '6 MB'; Description = 'The toolbox command and PowerShell module.'; Selected = $true; Disabled = $null }
    [pscustomobject] @{ Id = 'Html'; Name = 'HTML reports'; Group = 'Reporting'; Size = '4 MB'; Description = 'Interactive reports with tables and charts.'; Selected = $true; Disabled = $null }
    [pscustomobject] @{ Id = 'Pdf'; Name = 'PDF export'; Group = 'Reporting'; Size = '31 MB'; Description = 'Prints reports to PDF.'; Selected = $false; Disabled = $null }
    [pscustomobject] @{ Id = 'Chat'; Name = 'Chat notifications'; Group = 'Integrations'; Size = '1 MB'; Description = 'Posts run summaries to a channel.'; Selected = $false; Disabled = $null }
    [pscustomobject] @{ Id = 'Mail'; Name = 'Email notifications'; Group = 'Integrations'; Size = '1 MB'; Description = 'Sends reports by email.'; Selected = $false; Disabled = $null }
    [pscustomobject] @{ Id = 'Syslog'; Name = 'Syslog forwarder'; Group = 'Integrations'; Size = '1 MB'; Description = 'Forwards findings to a collector.'; Selected = $false; Disabled = 'needs the Enterprise licence' }
)
$accounts = @(
    [pscustomobject] @{ Id = 'CurrentUser'; Name = 'Current user'; Description = 'Runs with your signed-in identity.' }
    [pscustomobject] @{ Id = 'ServiceUser'; Name = 'Dedicated service identity'; Description = "Uses the platform's managed service identity." }
    [pscustomobject] @{ Id = 'Custom'; Name = 'A specific account'; Description = 'An account you name; password stays out of the command.' }
)
$regions = @(
    [pscustomobject] @{ Id = 'eu'; Name = 'Europe' }
    [pscustomobject] @{ Id = 'us'; Name = 'Americas' }
    [pscustomobject] @{ Id = 'ap'; Name = 'Asia Pacific' }
)
$endpoints = @(
    [pscustomobject] @{ Region = 'eu'; Host = 'eu-west.example.test'; City = 'Dublin'; Latency = '12 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'eu'; Host = 'eu-central.example.test'; City = 'Frankfurt'; Latency = '9 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'eu'; Host = 'eu-north.example.test'; City = 'Stockholm'; Latency = '31 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'us'; Host = 'us-east.example.test'; City = 'East'; Latency = '18 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'us'; Host = 'us-west.example.test'; City = 'West'; Latency = '74 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'ap'; Host = 'ap-south.example.test'; City = 'South'; Latency = '121 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'ap'; Host = 'ap-east.example.test'; City = 'East'; Latency = ''; Disabled = 'maintenance until 18:00' }
)
$pageNames = @('Location', 'Components', 'Service', 'Connection', 'Review', 'Install', 'Result')
$installSteps = @('Copy files', 'Register agent', 'Configure connection', 'Start agent')

# Layout: the native form owns validation, dependent choices, editors and review.
$form = New-ConsoleForm 'Install Toolbox Agent' -Command Install-ToolboxAgent -SubmitText Install -Description 'Sets up the agent that runs toolbox tasks on this machine.' -Content {
    New-ConsoleField Path 'Install folder' -Type Path -Kind Folder -Default $installFolder -Required -Page Location -ParameterName Path -Help 'Created during installation. Tab completes folder names.'
    New-ConsoleField Data 'Data folder' -Type Path -Kind Folder -Default $dataFolder -Page Location -ParameterName DataPath -Help 'Logs, reports and the local queue.'
    New-ConsoleField Launcher 'Launcher entry' -Type Toggle -Default $true -Page Location -ParameterName Launcher
    New-ConsoleField Components Components -Type MultiSelect -Choices $components -Display Name -CommandValue Id -GroupBy Group -Badge Size -Describe Description -Selected Selected -DisabledReason Disabled -Minimum 1 -Page Components -ParameterName Component -Help 'Space toggles, Ctrl+A selects all, Enter closes the list.'
    New-ConsoleField Account 'Service account' -Type Select -Choices $accounts -Display Name -CommandValue Id -Describe Description -Default ServiceUser -Page Service -ParameterName Account
    New-ConsoleField UserName 'User name' -VisibleWhen { $Values.Account -eq 'Custom' } -Required -Placeholder toolbox-agent -Page Service -ParameterName UserName
    New-ConsoleField Password Password -Type Secret -VisibleWhen { $Values.Account -eq 'Custom' } -Page Service -ParameterName Password -CommandPlaceholder '(Read-Host -AsSecureString)'
    New-ConsoleField AutoStart 'Start automatically' -Type Toggle -Default $true -Page Service -ParameterName AutoStart
    New-ConsoleField Region Region -Type Select -Choices $regions -Display Name -CommandValue Id -Default eu -Required -Page Connection -ParameterName Region -Help 'Choosing a region loads its servers.'
    New-ConsoleField Server Server -Type Select -DependsOn Region -Required -Page Connection -ParameterName Server -LoadChoices {
        param($Values, $Endpoints)
        Start-Sleep -Milliseconds 300
        $Endpoints | Where-Object Region -EQ $Values.Region
    } -ArgumentList (,$endpoints) -Display Host -CommandValue Host -Badge Latency -Describe City -DisabledReason Disabled -Help 'Loaded for the chosen region; Esc cancels loading.'
    New-ConsoleField Port Port -Type Number -Integer -Minimum 1 -Maximum 65535 -Default 443 -Page Connection -ParameterName Port
    New-ConsoleField UseProxy 'Use a proxy' -Type Toggle -Page Connection -ParameterName UseProxy
    New-ConsoleField Proxy 'Proxy address' -VisibleWhen { $Values.UseProxy } -Required -Placeholder 'http://proxy.example.test:8080' -Page Connection -ParameterName Proxy
}

# The snapshot uses native app controls on each wizard page, without asking for input.
function New-SetupSnapshot([int] $Page) {
    New-ConsoleRows -Gap 1 -Content {
        New-ConsoleRule '[bold]Install Toolbox Agent[/]'
        New-ConsolePhases -Content {
            for ($i = 0; $i -lt $pageNames.Count; $i++) {
                $status = if ($Page -eq 6 -or $i -lt $Page) { 'Pass' } elseif ($i -eq $Page) { 'Running' } else { 'Pending' }
                New-ConsoleStatusItem $pageNames[$i] -Status $status
            }
        }
        New-ConsolePanel -Title "$($Page + 1) / 7 · $($pageNames[$Page])" -Content {
            New-ConsoleRows -Gap 1 -Content {
                switch ($Page) {
                    0 {
                        'Where the agent and its data go.'
                        New-ConsoleTextInput -Key Path -Label 'Install folder' -Value $installFolder
                        New-ConsoleTextInput -Key Data -Label 'Data folder' -Value $dataFolder
                        New-ConsoleToggle -Key Launcher -Label 'Launcher entry' -Value $true
                        '[muted]Logs, reports and the local queue use the data folder.[/]'
                    }
                    1 {
                        $choices = $form.Pages[1].Fields[0].Choices
                        New-ConsoleMultiSelect -Key Components -Label Components -Choices $choices -Value @($choices | Where-Object Selected | ForEach-Object Value)
                        $components | ForEach-Object {
                            [pscustomobject] @{ Component = $_.Name; Group = $_.Group; Size = $_.Size; Description = $_.Description }
                        } | New-ConsoleTable -Property Component, Group, Size, Description -Expand
                        '[muted]Syslog forwarder needs the Enterprise licence. At least one component is required.[/]'
                    }
                    2 {
                        $choices = $form.Pages[2].Fields[0].Choices
                        New-ConsoleSelect -Key Account -Label 'Service account' -Choices $choices -Value $choices[1].Value
                        New-ConsoleToggle -Key AutoStart -Label 'Start automatically' -Value $true
                        'Choose a specific account to reveal user name and password fields.'
                        '[muted]Secrets are omitted from the equivalent command.[/]'
                    }
                    3 {
                        $choices = $form.Pages[3].Fields[0].Choices
                        New-ConsoleSelect -Key Region -Label Region -Choices $choices -Value $choices[0].Value
                        $serverField = New-ConsoleField Server -Type Select -Choices @($endpoints | Where-Object Region -EQ eu) -Display Host -CommandValue Host -Badge Latency -Describe City
                        New-ConsoleSelect -Key Server -Label Server -Choices $serverField.Choices -Value $serverField.Choices[1].Value
                        New-ConsoleTextInput -Key Port -Label 'Port (1–65535)' -Value 443
                        New-ConsoleToggle -Key UseProxy -Label 'Use a proxy'
                        '[muted]Changing the region reloads servers. Enabling a proxy reveals its address.[/]'
                    }
                    4 {
                        New-ConsoleKeyValue -InputObject ([ordered] @{
                            'Install folder' = $installFolder; 'Data folder' = $dataFolder; Components = 'Agent, Cli, Html'
                            'Service account' = 'Dedicated service identity'; Server = 'eu-central.example.test:443'; 'Launcher / auto start' = 'Yes / Yes'
                        })
                        New-ConsolePanel -Title 'Equivalent command' -Content {
                            "Install-ToolboxAgent -Path './toolbox-agent' -DataPath './toolbox-data' -Component Agent,Cli,Html -Account ServiceUser -Region eu -Server eu-central.example.test -Port 443 -Launcher -AutoStart"
                        }
                        New-ConsoleButton -Key Install -Label Install
                        New-ConsoleButton -Key Back -Label Back
                    }
                    default {
                        New-ConsoleStatusList -Content {
                            for ($i = 0; $i -lt $installSteps.Count; $i++) {
                                $status = if ($Page -eq 6 -or $i -lt 2) { 'Pass' } elseif ($i -eq 2) { 'Running' } else { 'Pending' }
                                New-ConsoleStatusItem $installSteps[$i] -Status $status
                            }
                        }
                        New-ConsoleProgress -Label Install -Total 40 -Value $(if ($Page -eq 6) { 40 } else { 24 }) -Status $(if ($Page -eq 6) { 'Pass' } else { 'Running' })
                        if ($Page -eq 6) { '[pass]Toolbox Agent installed[/] · 4 steps done · launch with the toolbox command' }
                        else { 'Configuring eu-central.example.test:443' }
                    }
                }
            }
        }
    }
}

# Behaviour: edit every page, review, then run a bounded in-memory installation.
if ($StaticSnapshot -or [Console]::IsInputRedirected) {
    Show-ConsoleApp -Content { New-SetupSnapshot $Step } -StaticSnapshot
    return
}
$answers = Read-ConsoleForm $form
if (-not $answers) { return }
$dashboard = New-ConsoleDashboard -Title 'Installing Toolbox Agent' -Subtitle $answers.Path -Content {
    New-ConsolePanel -Title Steps -Content { New-ConsoleStatusList -WidgetId steps -Item $installSteps }
    New-ConsolePadding -Padding 1, 0 -Content { New-ConsoleProgress -Label Install -Total 40 }
}
Invoke-ConsoleDashboard $dashboard -ArgumentList (,$installSteps) {
    param($Steps)
    foreach ($label in $Steps) {
        Update-ConsoleStatusItem -WidgetId steps -Key $label -Status Running
        Update-ConsoleProgress -Label $label
        1..10 | ForEach-Object { Start-Sleep -Milliseconds 90; Update-ConsoleProgress -Increment 1 }
        Update-ConsoleStatusItem -WidgetId steps -Key $label -Status Pass
    }
    Update-ConsoleProgress -Status Pass
}
New-ConsolePanel -Title 'Toolbox Agent installed' -Content {
    New-ConsoleKeyValue -Content {
        New-ConsoleKeyValueItem 'Install folder' $answers.Path
        New-ConsoleKeyValueItem Components ($answers.Components -join ', ')
        New-ConsoleKeyValueItem Server $answers.Server
        New-ConsoleKeyValueItem 'Steps done' '4' -Status Pass
        New-ConsoleKeyValueItem Launch toolbox
    }
} | Show-Console
