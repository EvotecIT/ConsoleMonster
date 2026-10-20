New-ConsoleRows {
    New-ConsoleProgress 'Build' -Value 64 -Total 100
    New-ConsoleProgress 'Tests' -Value 412 -Total 530
    New-ConsoleProgress 'Docs' -Value 100 -Total 100 -Status Pass
    New-ConsoleProgress 'Upload' -Value 3 -Total 8 -Shape Block
} | Write-ConsoleWidget
