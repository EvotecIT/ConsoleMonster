# A complete installer: location, components, service, connection, review and progress.
Import-Module -Name ConsoleMonster

# Data: invented installation inputs. The bounded driver only delays and updates widgets.
# No files, services, launch entries, network connections or persistent presets are created.
$installFolder = './toolbox-agent'
$dataFolder = './toolbox-data'
$components = @(
    [pscustomobject] @{ Id = 'Agent'; Name = 'Agent service'; Group = 'Core'; Size = '18 MB'
        Description = 'Runs tasks and reports results.'; Selected = $true; Disabled = $null }
    [pscustomobject] @{ Id = 'Cli'; Name = 'Command-line tools'; Group = 'Core'; Size = '6 MB'
        Description = 'The toolbox command and PowerShell module.'; Selected = $true
        Disabled = $null }
    [pscustomobject] @{ Id = 'Html'; Name = 'HTML reports'; Group = 'Reporting'; Size = '4 MB'
        Description = 'Interactive reports with tables and charts.'; Selected = $true
        Disabled = $null }
    [pscustomobject] @{ Id = 'Pdf'; Name = 'PDF export'; Group = 'Reporting'; Size = '31 MB'
        Description = 'Prints reports to PDF.'; Selected = $false; Disabled = $null }
    [pscustomobject] @{ Id = 'Chat'; Name = 'Chat notifications'; Group = 'Integrations'
        Size = '1 MB'; Description = 'Posts run summaries to a channel.'; Selected = $false
        Disabled = $null }
    [pscustomobject] @{ Id = 'Mail'; Name = 'Email notifications'; Group = 'Integrations'
        Size = '1 MB'; Description = 'Sends reports by email.'; Selected = $false
        Disabled = $null }
    [pscustomobject] @{ Id = 'Syslog'; Name = 'Syslog forwarder'; Group = 'Integrations'
        Size = '1 MB'; Description = 'Forwards findings to a collector.'; Selected = $false
        Disabled = 'needs the Enterprise licence' }
)
$accounts = @(
    [pscustomobject] @{ Id = 'CurrentUser'; Name = 'Current user'
        Description = 'Runs with your signed-in identity.' }
    [pscustomobject] @{ Id = 'ServiceUser'; Name = 'Dedicated service identity'
        Description = "Uses the platform's managed service identity." }
    [pscustomobject] @{ Id = 'Custom'; Name = 'A specific account'
        Description = 'An account you name; password stays out of the command.' }
)
$regions = @(
    [pscustomobject] @{ Id = 'eu'; Name = 'Europe' }
    [pscustomobject] @{ Id = 'us'; Name = 'Americas' }
    [pscustomobject] @{ Id = 'ap'; Name = 'Asia Pacific' }
)
$endpoints = @(
    [pscustomobject] @{ Region = 'eu'; Host = 'eu-west.example.test'; City = 'Dublin'
        Latency = '12 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'eu'; Host = 'eu-central.example.test'; City = 'Frankfurt'
        Latency = '9 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'eu'; Host = 'eu-north.example.test'; City = 'Stockholm'
        Latency = '31 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'us'; Host = 'us-east.example.test'; City = 'East'
        Latency = '18 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'us'; Host = 'us-west.example.test'; City = 'West'
        Latency = '74 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'ap'; Host = 'ap-south.example.test'; City = 'South'
        Latency = '121 ms'; Disabled = $null }
    [pscustomobject] @{ Region = 'ap'; Host = 'ap-east.example.test'; City = 'East'; Latency = ''
        Disabled = 'maintenance until 18:00' }
)
$installSteps = @('Copy files', 'Register agent', 'Configure connection', 'Start agent')

# Layout: the native form owns validation, dependent choices, editors and review.
$formOptions = @{
    Title = 'Install Toolbox Agent'
    Command = 'Install-ToolboxAgent'
    SubmitText = 'Install'
    Description = 'Sets up the agent that runs toolbox tasks on this machine.'
    Content = {
        $fieldOptions = @{
            Key = 'Path'
            Label = 'Install folder'
            Type = 'Path'
            Kind = 'Folder'
            Default = $installFolder
            Required = $true
            Page = 'Location'
            ParameterName = 'Path'
            Help = 'Created during installation. Tab completes folder names.'
        }
        New-ConsoleField @fieldOptions
        $fieldOptions = @{
            Key = 'Data'
            Label = 'Data folder'
            Type = 'Path'
            Kind = 'Folder'
            Default = $dataFolder
            Page = 'Location'
            ParameterName = 'DataPath'
            Help = 'Logs, reports and the local queue.'
        }
        New-ConsoleField @fieldOptions
        $fieldParameters = @{
            Key = 'Launcher'
            Label = 'Launcher entry'
            Type = 'Toggle'
            Default = $true
            Page = 'Location'
            ParameterName = 'Launcher'
        }
        New-ConsoleField @fieldParameters
        $fieldOptions = @{
            Key = 'Components'
            Label = 'Components'
            Type = 'Select'
            Multiple = $true
            Choices = $components
            Display = 'Name'
            CommandValue = 'Id'
            GroupBy = 'Group'
            Badge = 'Size'
            Describe = 'Description'
            Selected = 'Selected'
            DisabledReason = 'Disabled'
            Minimum = 1
            Page = 'Components'
            ParameterName = 'Component'
            Help = 'Space toggles, Ctrl+A selects all, Enter closes the list.'
        }
        New-ConsoleField @fieldOptions
        $fieldOptions = @{
            Key = 'Account'
            Label = 'Service account'
            Type = 'Select'
            Choices = $accounts
            Display = 'Name'
            CommandValue = 'Id'
            Describe = 'Description'
            Default = 'ServiceUser'
            Page = 'Service'
            ParameterName = 'Account'
        }
        New-ConsoleField @fieldOptions
        $fieldOptions = @{
            Key = 'UserName'
            Label = 'User name'
            VisibleWhen = { $Values.Account -eq 'Custom' }
            Required = $true
            Placeholder = 'toolbox-agent'
            Page = 'Service'
            ParameterName = 'UserName'
        }
        New-ConsoleField @fieldOptions
        $fieldOptions = @{
            Key = 'Password'
            Label = 'Password'
            Type = 'Text'
            AsSecret = $true
            VisibleWhen = { $Values.Account -eq 'Custom' }
            Page = 'Service'
            ParameterName = 'Password'
            CommandPlaceholder = '(Read-Host -AsSecureString)'
        }
        New-ConsoleField @fieldOptions
        $fieldParameters = @{
            Key = 'AutoStart'
            Label = 'Start automatically'
            Type = 'Toggle'
            Default = $true
            Page = 'Service'
            ParameterName = 'AutoStart'
        }
        New-ConsoleField @fieldParameters
        $fieldOptions = @{
            Key = 'Region'
            Label = 'Region'
            Type = 'Select'
            Choices = $regions
            Display = 'Name'
            CommandValue = 'Id'
            Default = 'eu'
            Required = $true
            Page = 'Connection'
            ParameterName = 'Region'
            Help = 'Choosing a region loads its servers.'
        }
        New-ConsoleField @fieldOptions
        $fieldParameters = @{
            Key = 'Server'
            Label = 'Server'
            Type = 'Select'
            DependsOn = 'Region'
            Required = $true
            Page = 'Connection'
            ParameterName = 'Server'
            ArgumentList = (, $endpoints)
            Display = 'Host'
            CommandValue = 'Host'
            Badge = 'Latency'
            Describe = 'City'
            DisabledReason = 'Disabled'
            Help = 'Loaded for the chosen region; Esc cancels loading.'
        }
        New-ConsoleField @fieldParameters -LoadChoices {
            param($Values, $Endpoints)
            Start-Sleep -Milliseconds 300
            $Endpoints | Where-Object -Property Region -EQ -Value $Values.Region
        }
        $fieldParameters = @{
            Key = 'Port'
            Label = 'Port'
            Type = 'Number'
            Integer = $true
            Minimum = 1
            Maximum = 65535
            Default = 443
            Page = 'Connection'
            ParameterName = 'Port'
        }
        New-ConsoleField @fieldParameters
        $fieldParameters = @{
            Key = 'UseProxy'
            Label = 'Use a proxy'
            Type = 'Toggle'
            Page = 'Connection'
            ParameterName = 'UseProxy'
        }
        New-ConsoleField @fieldParameters
        $fieldOptions = @{
            Key = 'Proxy'
            Label = 'Proxy address'
            VisibleWhen = { $Values.UseProxy }
            Required = $true
            Placeholder = 'http://proxy.example.test:8080'
            Page = 'Connection'
            ParameterName = 'Proxy'
        }
        New-ConsoleField @fieldOptions
    }
}
$form = New-ConsoleForm @formOptions

# Behaviour: edit every page, review, then run a bounded in-memory installation.
$answers = Read-ConsoleForm -Form $form
if (-not $answers) {
    return
}
$dashboardParameters = @{
    Title = 'Installing Toolbox Agent'
    Subtitle = $answers.Path
}
$dashboard = New-ConsoleDashboard @dashboardParameters -Content {
    New-ConsolePanel -Title Steps -Content {
        New-ConsoleStatusList -WidgetId steps -Item $installSteps
    }
    New-ConsolePadding -Padding 1, 0 -Content { New-ConsoleProgress -Label Install -Total 40 }
}
Invoke-ConsoleDashboard -Dashboard $dashboard -ScriptBlock {
    $Steps = $using:installSteps
    foreach ($label in $Steps) {
        Update-Console -WidgetId steps -Key $label -Status Running
        Update-Console -Label $label -WidgetId 'progress'
        for ($i = 0; $i -lt 10; $i++) {
            Update-Console -Increment 1 -WidgetId 'progress'
            Wait-ConsoleDashboard -Seconds 0.09
        }
        Update-Console -WidgetId steps -Key $label -Status Pass
    }
    Update-Console -Status Pass -WidgetId 'progress'
}
New-ConsolePanel -Title 'Toolbox Agent installed' -Content {
    New-ConsoleKeyValue -Content {
        [pscustomobject]@{ Key = 'Install folder'; Value = $answers.Path }
        [pscustomobject]@{ Key = 'Components'; Value = ($answers.Components -join ', ') }
        [pscustomobject]@{ Key = 'Server'; Value = $answers.Server }
        [pscustomobject]@{ Key = 'Steps done'; Value = '4'; Status = 'Pass' }
        [pscustomobject]@{ Key = 'Launch'; Value = 'toolbox' }
    }
} | Show-Console
