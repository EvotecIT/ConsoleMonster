$shapeCases = foreach ($shape in 'splat-command', 'duplicate-inner', 'distinct-cause', 'embedded-stack', 'detached-record', 'mixed-output') {
    foreach ($width in 120, 60) {
        foreach ($theme in 'light', 'dark') { @{ Shape = $shape; Width = $width; Theme = $theme } }
    }
}
Describe 'Synthetic compact error shapes' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/New-ErrorShapeFixture.ps1')
        . (Join-Path $PSScriptRoot 'Support/Get-ErrorShapeRecords.ps1')
        $script:ShapeInvocation = New-ErrorShapeFixture -Directory $TestDrive
        $script:ShapeMixedInvocation = New-ErrorShapeFixture -Directory $TestDrive -MixedOutput
        $script:SavedProfile = [ConsoleMonster.Terminal]::Profile
        $script:SavedErrorView = $ErrorView
        $script:RuntimeName = if ($PSVersionTable.PSVersion.Major -ge 7) { 'ps7' } else { 'ps5' }
        $script:NativeErrorView = if ($PSVersionTable.PSVersion.Major -ge 7) { 'ConciseView' } else { 'NormalView' }
        $script:English = [Globalization.CultureInfo]::GetCultureInfo('en-US')
        [ConsoleMonster.Terminal]::Profile = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile'))::Fixed(120, 80).WithLanguage($script:English).WithCulture($script:English)
    }
    AfterAll { [ConsoleMonster.Terminal]::Profile = $script:SavedProfile; $global:ErrorView = $script:SavedErrorView; Disable-ConsoleFormatting }

    It 'uses the fallback header for a script-level throw without inventing a command name' {
        $record = try { & { throw 'Synthetic script-level failure.' } } catch { $_ }
        $widget = $record | Show-Console -ErrorLayout Compact -AsWidget -View Error
        $text = [ConsoleMonster.Terminal]::RenderText($widget, [ConsoleMonster.Terminal]::Profile, [ConsoleMonster.Styling.Theme]::Dark)
        $text | Should -Match '^Error: Synthetic script-level failure\.'
        $text | Should -Not -Match 'NotSpecified'
    }

    It 'preserves useful <Shape> details at <Width> columns in <Theme>' -ForEach $shapeCases {
        $invocation = if ($Shape -eq 'mixed-output') { $script:ShapeMixedInvocation } else { $script:ShapeInvocation }
        $records = @(Get-ErrorShapeRecords -Shape $Shape -Invocation $invocation)
        $profile = [ConsoleMonster.Terminal]::Profile.WithSize($Width, 100)
        $themeObject = if ($Theme -eq 'light') { [ConsoleMonster.Styling.Theme]::Light } else { [ConsoleMonster.Styling.Theme]::Dark }
        $widgets = [Collections.Generic.List[ConsoleMonster.Layout.IWidget]]::new()
        $ErrorView = $script:NativeErrorView
        foreach ($record in $records) {
            if ($record -is [Management.Automation.ErrorRecord]) {
                $native = $record | Out-String -Width $Width
                Enable-ConsoleFormatting -Include Errors -Exclude Output, FormatCommands, Streams, ReadLine -ErrorLayout Compact
                try { ($record | Out-String -Width $Width) | Should -BeExactly $native }
                finally { Disable-ConsoleFormatting -Feature Errors }
                $widgets.Add(($record | Show-Console -ErrorLayout Compact -AsWidget -View Error))
            } else { $widgets.Add(([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.TextBlock'))::Plain([string] $record)) }
        }
        $view = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Layout.Rows'))::new($widgets.ToArray())
        $frame = [ConsoleMonster.Terminal]::Render($view, $profile, $themeObject)
        $text = $frame.ToPlainText().Replace("`r", '').TrimEnd()
        @($text -split "`n" | Where-Object { ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.DisplayWidth'))::Measure($_) -gt $Width }).Count | Should -Be 0
        switch ($Shape) {
            'splat-command' {
                $records[0].InvocationInfo.MyCommand.CommandType | Should -Be ([Management.Automation.CommandTypes]::Cmdlet)
                $text | Should -Match 'Invoke-SyntheticInventory.ps1:4:8'
                $lines = $text -split "`n"
                $sourceIndex = [array]::IndexOf($lines, '$x = @(& $command @arguments)')
                $sourceIndex | Should -BeGreaterOrEqual 0
                $lines[$sourceIndex + 1] | Should -BeExactly ('       ' + ('^' * 21))
                $text | Should -Match '^Test-SyntheticErrorShape: '
                $prefix = 'Test-SyntheticErrorShape: '
                $message = @($lines | Select-Object -First ($sourceIndex - 1))
                ($message | Select-Object -Skip 1 | Where-Object { -not $_.StartsWith((' ' * $prefix.Length)) }).Count | Should -Be 0
            }
            'duplicate-inner' {
                $records[0].Exception | Should -BeOfType ([Management.Automation.CmdletInvocationException])
                $records[0].Exception.InnerException | Should -BeOfType ([NullReferenceException])
                $joined = ($text -split "`n" | ForEach-Object { $_.Trim() }) -join ' '
                ([regex]::Matches($joined, 'Object reference not set to an instance of an object')).Count | Should -Be 1
                $text | Should -Match '\+1 inner exception \(NullReferenceException\)'
            }
            'distinct-cause' { $text | Should -Match 'cause: The synthetic inventory cache is locked'; ($text -replace '\s+', ' ') | Should -Match 'by another writer\.' }
            'embedded-stack' { $text | Should -Match '20 stack lines hidden'; $text | Should -Not -Match 'System\.Synthetic'; ($text -split "`n").Count | Should -BeLessThan 12 }
            'detached-record' {
                $text | Should -Match '^Error: '
                $records[0].InvocationInfo | Should -BeNullOrEmpty
                $text | Should -Not -Match 'command line|\^|:\d+:\d+'
                @($text -split "`n" | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count | Should -Be 0
                $text | Should -Match 'detached worker'
                $written = @(& { Write-Error -Exception $records[0].Exception -ErrorAction Continue } 2>&1)
                $written[0] | Should -BeOfType ([Management.Automation.ErrorRecord])
                $writtenWidget = $written[0] | Show-Console -ErrorLayout Compact -AsWidget -View Error
                $writtenText = [ConsoleMonster.Terminal]::RenderText($writtenWidget, $profile, $themeObject)
                $writtenText | Should -Match 'detached worker'
                $writtenText | Should -Not -Match '(?m)^\s*:\d+'
            }
            'mixed-output' {
                $records[1].InvocationInfo.Line.Trim() | Should -BeExactly 'Test-SyntheticErrorShape -Shape mixed-output'
                $records[1].InvocationInfo.MyCommand.Name | Should -BeExactly 'Test-SyntheticErrorShape'
                $text | Should -Match '(?m)^Test-SyntheticErrorShape: First synthetic failure\.'
                $text | Should -Match '(?m)^Test-SyntheticErrorShape -Shape mixed-output$'
                $text | Should -Not -Match 'Out-ConsoleDefault|TopLevel|2>&1|Shim'
                $records.Count | Should -Be 5
                $order = @('before error', 'First synthetic failure.', 'between errors', 'Second synthetic failure.', 'after errors')
                $position = -1
                foreach ($value in $order) { $next = $text.IndexOf($value, [StringComparison]::Ordinal); $next | Should -BeGreaterThan $position; $position = $next }
            }
        }
        $text | Should -Not -Match 'NotSpecified'
        if ($Shape -in @('distinct-cause', 'embedded-stack')) {
            $text | Should -Match '^Test-SyntheticErrorShape: '
            if ($Shape -eq 'distinct-cause') { $text | Should -Match 'System.InvalidOperationException' }
        }
        if ($Shape -eq 'duplicate-inner') {
            # PowerShell deliberately exposes no command metadata for this wrapped CmdletInvocationException.
            $records[0].InvocationInfo.MyCommand | Should -BeNullOrEmpty
            $text | Should -Match '^Error: '
        }
        $path = Join-Path $PSScriptRoot "Golden/errors/$Shape-$script:RuntimeName-$Width-$Theme.txt"
        if ($env:CONSOLEMONSTER_UPDATE_GOLDEN -eq '1') {
            $null = New-Item -ItemType Directory -Path (Split-Path $path) -Force
            [IO.File]::WriteAllText($path, $text + "`n", [Text.UTF8Encoding]::new($false))
        }
        Test-Path -LiteralPath $path | Should -BeTrue
        $text | Should -BeExactly ([IO.File]::ReadAllText($path).Replace("`r", '').TrimEnd())
    }

    It 'preserves live compiled WriteError ordering at <Width> columns' -Skip:($env:OS -ne 'Windows_NT') -ForEach @(@{Width=120}, @{Width=60}) {
        $runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        $probe = Join-Path $PSScriptRoot 'Support/Measure-ErrorShapeOrder.ps1'
        $module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $receipt = Join-Path $TestDrive "order-$Width.json"
        $info = [Diagnostics.ProcessStartInfo]::new($runtime)
        $info.Arguments = "-NoLogo -NoProfile -NonInteractive -File `"$probe`" -ModulePath `"$module`" -ReceiptPath `"$receipt`" -Width $Width"
        $info.UseShellExecute = $true
        $info.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
        $process = [Diagnostics.Process]::Start($info)
        try {
            if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'Live error-order regression timed out.' }
            $result = Get-Content -LiteralPath $receipt -Raw | ConvertFrom-Json
            if ($process.ExitCode -ne 0 -or -not $result.Passed) { throw $result.Error }
        } finally { $process.Dispose() }
    }
}
