Describe 'Application keyboard editing contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'returns every keyboard-only edit through Show-ConsoleApp' {
        $inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new('Ctrl+Home Ctrl+Delete*3 Down "x" Escape')
        $capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new(80, 24)
        $result = Show-ConsoleApp -Content {
            New-ConsoleTextInput -Key Notes -Value "one`ntwo`nthree" -Multiline
        } -InputSource $inputSource -Capture $capture -NoMouse -PassThru -ErrorAction Stop
        $result.Notes | Should -Be 'x'
        ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired | Should -BeFalse
    }

}
