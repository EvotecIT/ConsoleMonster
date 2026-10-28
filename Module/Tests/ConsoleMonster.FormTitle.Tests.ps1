Describe 'Read-ConsoleForm prompt title' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force -ErrorAction Stop
    }

    It 'accepts the prompt alias without changing the reusable form' {
        $form = New-ConsoleForm 'Original' -Content { New-ConsoleField Name 'Name' -Default 'sample' }
        $answer = Read-ConsoleForm $form -Prompt 'Temporary' -Mode None
        $answer.Name | Should -Be 'sample'
        $form.Title | Should -Be 'Original'
        (Get-Command Read-ConsoleForm).Parameters.Title.Aliases | Should -Contain 'Prompt'
    }
}
