Describe 'Inline images' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:ModulePath = (Resolve-Path (Join-Path $PSScriptRoot '../ConsoleMonster.psd1')).Path
        # 2x2 PNG: red, green / blue, transparent white.
        $script:Png = [Convert]::FromBase64String('iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAGElEQVR4nGP4z8Dwn6GB4T8DA8N/EGAAAELVCHmF7Ec3AAAAAElFTkSuQmCC')
        Import-Module $script:ModulePath -Force
    }

    It 'New-ConsoleImage accepts byte arrays, numeric arrays and open streams through Bytes' {
        $bytes = New-ConsoleImage -Bytes $script:Png -Label dot.png -Width 4
        $numbers = New-ConsoleImage -Bytes ([int[]] $script:Png) -Label dot.png -Width 4
        $stream = [IO.MemoryStream]::new()
        try {
            $stream.WriteByte(42)
            $stream.Write($script:Png, 0, $script:Png.Length)
            $stream.Position = 1
            $image = New-ConsoleImage -Bytes $stream -Label dot.png -Width 4
            $image.Placeholder | Should -Be $bytes.Placeholder
            $numbers.Placeholder | Should -Be $bytes.Placeholder
            $image.Width | Should -Be 4
            $stream.Position | Should -Be $stream.Length
            $stream.CanRead | Should -BeTrue
        } finally { $stream.Dispose() }
    }

    It 'Show-ConsoleImage reads an open stream in redirected output' {
        $module = $script:ModulePath.Replace("'", "''")
        $png = [Convert]::ToBase64String($script:Png)
        $output = @(Invoke-TestChild -Runtime $script:Runtime -TimeoutSeconds 60 -Script "Import-Module '$module'; `$stream = [IO.MemoryStream]::new([Convert]::FromBase64String('$png')); try { Show-ConsoleImage -Bytes `$stream -Label dot.png -Protocol Sixel; 'OPEN=' + `$stream.CanRead; 'POSITION=' + `$stream.Position } finally { `$stream.Dispose() }")
        $output | Should -Contain '[image: dot.png 2x2]'
        $output | Should -Contain 'OPEN=True'
        $output | Should -Contain "POSITION=$($script:Png.Length)"
    }

    It '<Command> reports the stream byte limit without closing it' -ForEach @(
        @{ Command = 'Show-ConsoleImage' }
        @{ Command = 'New-ConsoleImage' }
    ) {
        $path = Join-Path $TestDrive "$Command.png"
        $stream = [IO.File]::Open($path, [IO.FileMode]::Create)
        try {
            $stream.SetLength(64MB + 1)
            $errors = @()
            & $Command -Bytes $stream -ErrorAction SilentlyContinue -ErrorVariable errors
            $errors.Count | Should -Be 1
            $errors[0].FullyQualifiedErrorId | Should -BeLike 'ConsoleImageInvalid,*'
            $errors[0].Exception.Message | Should -BeLike '*byte limit*'
            $stream.CanRead | Should -BeTrue
        } finally { $stream.Dispose() }
    }

    It '<Command> continues past damaged and oversized files in redirected output' -ForEach @(
        @{ Command = 'Show-ConsoleImage'; Protocol = '-Protocol Kitty' }
        @{ Command = 'New-ConsoleImage'; Protocol = '' }
    ) {
        $folder = Join-Path $TestDrive $Command
        New-Item -ItemType Directory -Path $folder | Out-Null
        [IO.File]::WriteAllBytes((Join-Path $folder 'a.png'), $script:Png)
        [IO.File]::WriteAllText((Join-Path $folder 'b.png'), 'not an image')
        [IO.File]::WriteAllBytes((Join-Path $folder 'c.png'), $script:Png)
        $oversized = [IO.File]::OpenWrite((Join-Path $folder 'd.png'))
        try { $oversized.SetLength(64MB + 1) } finally { $oversized.Dispose() }
        [IO.File]::WriteAllBytes((Join-Path $folder 'e.png'), $script:Png)
        $module = $script:ModulePath.Replace("'", "''")
        $directory = $folder.Replace("'", "''")
        $output = @(Invoke-TestChild -Runtime $script:Runtime -TimeoutSeconds 60 -Script "Import-Module '$module'; Get-ChildItem -LiteralPath '$directory' -Filter *.png | Sort-Object Name | $Command $Protocol -Width 4 -ErrorAction SilentlyContinue -ErrorVariable failures | ForEach-Object { if (`$_.PSObject.Properties['Placeholder']) { `$_.Placeholder } else { `$_ } }; 'ERRORS=' + @(`$failures).Count; `$failures | ForEach-Object { 'ERROR=' + `$_.FullyQualifiedErrorId.Split(',')[0] + ':' + (Split-Path -Leaf `$_.TargetObject) }; $Command $Protocol -Path '$directory/missing.png' -ErrorAction SilentlyContinue -ErrorVariable unreadable; 'READERROR=' + `$unreadable[0].FullyQualifiedErrorId.Split(',')[0]; 'DONE'")
        $text = $output -join "`n"
        $output | Should -Contain '[image: a.png 2x2]'
        $output | Should -Contain '[image: c.png 2x2]'
        $output | Should -Contain '[image: e.png 2x2]'
        $output | Should -Contain 'DONE'
        $output | Should -Contain 'ERRORS=2'
        $output | Should -Contain 'READERROR=ConsoleImageUnreadable'
        $output | Should -Contain 'ERROR=ConsoleImageInvalid:b.png'
        $output | Should -Contain 'ERROR=ConsoleImageInvalid:d.png'
        $text | Should -Not -Match ([regex]::Escape([string][char]27))
    }
}
