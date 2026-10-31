Describe 'Inline images' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:ModulePath = (Resolve-Path (Join-Path $PSScriptRoot '../ConsoleMonster.psd1')).Path
        # 2x2 PNG: red, green / blue, transparent white.
        $script:Png = [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAGElEQVR4nGP4z8Dwn6GB4T8DA8N/EGAAAELVCHmF7Ec3AAAAAElFTkSuQmCC')
    }

    It 'prints a placeholder per piped file in redirected output and continues past an unreadable one' {
        $folder = Join-Path $TestDrive 'images'
        New-Item -ItemType Directory -Path $folder | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $folder 'a.png'), $script:Png)
        [IO.File]::WriteAllText((Join-Path $folder 'b.png'), 'not an image')
        [IO.File]::WriteAllBytes((Join-Path $folder 'c.png'), $script:Png)
        $module = $script:ModulePath.Replace("'", "''")
        $directory = $folder.Replace("'", "''")
        $output = @(Invoke-TestChild -Runtime $script:Runtime -TimeoutSeconds 60 -Script "Import-Module '$module'; Get-ChildItem -LiteralPath '$directory' -Filter *.png | Sort-Object Name | Show-ConsoleImage -Protocol Kitty -Width 4 -ErrorAction SilentlyContinue -ErrorVariable failures; 'ERRORS=' + (`$failures | ForEach-Object { `$_.FullyQualifiedErrorId.Split(',')[0] + ':' + (Split-Path -Leaf `$_.TargetObject) }); 'DONE'")
        $text = $output -join "`n"
        $output | Should -Contain '[image: a.png 2x2]'
        $output | Should -Contain '[image: c.png 2x2]'
        $output | Should -Contain 'DONE'
        $output | Should -Contain 'ERRORS=ConsoleImageInvalid:b.png'
        $text | Should -Not -Match ([regex]::Escape([string][char]27))
    }
}
