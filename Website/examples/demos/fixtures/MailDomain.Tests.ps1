# Invented checks for the Pester demo: short waits stand in for DNS lookups.
Describe 'evotec.xyz' {
    Context 'MX' {
        It 'has two MX records' { Start-Sleep -Milliseconds 220; 2 | Should -Be 2 }
        It 'points at Exchange Online' {
            Start-Sleep -Milliseconds 180
            'evotec-xyz.mail.protection.outlook.com' | Should -BeLike '*.outlook.com'
        }
        It 'answers on port 25' { Start-Sleep -Milliseconds 260; $true | Should -BeTrue }
    }
    Context 'SPF' {
        It 'publishes one SPF record' { Start-Sleep -Milliseconds 150; 1 | Should -Be 1 }
        It 'stays under 10 DNS lookups' { Start-Sleep -Milliseconds 240; 9 | Should -BeLessThan 10 }
        It 'ends with -all' { Start-Sleep -Milliseconds 120; '-all' | Should -Be '-all' }
    }
    Context 'DMARC' {
        It 'publishes a DMARC record' {
            Start-Sleep -Milliseconds 200
            'v=DMARC1' | Should -Not -BeNullOrEmpty
        }
        It 'enforces a reject policy' {
            Start-Sleep -Milliseconds 230
            'none' | Should -Be 'reject'
        }
        It 'sends aggregate reports' {
            Start-Sleep -Milliseconds 140
            'mailto:dmarc@evotec.xyz' | Should -Match '^mailto:'
        }
    }
    Context 'DKIM' {
        It 'signs with selector1' {
            Start-Sleep -Milliseconds 170
            'selector1' | Should -Be 'selector1'
        }
        It 'uses 2048-bit keys' {
            Start-Sleep -Milliseconds 210
            1024 | Should -BeGreaterOrEqual 2048
        }
        It 'rotates keys yearly' -Skip { }
    }
    Context 'MTA-STS' {
        It 'serves the policy file' { Start-Sleep -Milliseconds 250; 200 | Should -Be 200 }
        It 'is in enforce mode' { Start-Sleep -Milliseconds 160; 'enforce' | Should -Be 'enforce' }
    }
}
