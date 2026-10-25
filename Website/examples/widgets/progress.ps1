Import-Module ConsoleMonster

New-ConsoleRows -Content {
    New-ConsoleProgress -Label 'Build' -Value 64 -Total 100
    New-ConsoleProgress -Label 'Tests' -Value 412 -Total 530
    New-ConsoleProgress -Label 'Docs' -Value 100 -Total 100 -Status Pass
    New-ConsoleProgress -Label 'Upload' -Value 3 -Total 8 -Shape Block
} | Show-Console
