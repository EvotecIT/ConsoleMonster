function ConvertTo-TestEverydayText {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)] [object] $InputObject,
        [int] $Width = 96
    )
    begin { $items = [Collections.Generic.List[object]]::new() }
    process { $items.Add($InputObject) }
    end {
        # Exercise the real default-output service against a public recording target. Export is a separate job.
        $assembly = [ConsoleMonster.Terminal].Assembly
        $profileType = $assembly.GetType('ConsoleMonster.Capabilities.TerminalProfile')
        $targetType = $assembly.GetType('ConsoleMonster.Output.RecordingTarget')
        $profile = $profileType::Fixed($Width, 200).WithCulture([cultureinfo]'en-US').WithLanguage([cultureinfo]'en-US')
        $target = $targetType.GetConstructors()[0].Invoke(@($profile, $null))
        $previous = [ConsoleMonster.Terminal]::Profile
        $scope = [ConsoleMonster.Terminal]::Use($target)
        try {
            [ConsoleMonster.Terminal]::Profile = $profile
            $items | Out-ConsoleDefault -Force
            $target.Transcript().ToPlainText().TrimEnd()
        } finally {
            $scope.Dispose()
            [ConsoleMonster.Terminal]::Profile = $previous
        }
    }
}
