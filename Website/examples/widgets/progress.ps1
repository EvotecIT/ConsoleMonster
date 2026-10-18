New-ConsoleRows {
    New-ConsoleProgress 'Build' -Value 64 -Total 100
    New-ConsoleProgress 'Tests' -Value 412 -Total 530
    $docs = New-ConsoleProgress 'Docs' -Value 100 -Total 100
    $docs.Status = 'Pass'
    $docs
    New-ConsoleProgress 'Upload' -Value 3 -Total 8 -Shape Block
} | Write-ConsoleWidget
