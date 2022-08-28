# Get library name, from the PSM1 file name
$Library = $myInvocation.MyCommand.Name.Replace(".psm1", '.dll')
$Class = 'ConsoleMonster.SpectreImage'

# This is special way of importing DLL if multiple frameworks are in use
$FoundErrors = @(
    # this is to prevent import library over and over again

    try {
        $ImportModule = Get-Command -Name Import-Module -Module Microsoft.PowerShell.Core
        $Framework = if ($PSVersionTable.PSVersion.Major -eq 5) {
            'Default'
        } else {
            'Core'
        }
        if (-not $Class -as [type]) {
            & $ImportModule ([IO.Path]::Combine($PSScriptRoot, 'Lib', $Framework, $Library)) -ErrorAction Stop
        } else {
            $Type = "$Class" -as [Type]
            &$importModule -Force -Assembly ($Type.Assembly)
        }
    } catch {
        Write-Warning -Message "Importing module $Library failed. Fix errors before continuing. Error: $($_.Exception.Message)"
        $true
    }

    #Get public and private function definition files.
    $Public = @( Get-ChildItem -Path $PSScriptRoot\Public\*.ps1 -ErrorAction SilentlyContinue -Recurse )
    $Private = @( Get-ChildItem -Path $PSScriptRoot\Private\*.ps1 -ErrorAction SilentlyContinue -Recurse )
    $Classes = @( Get-ChildItem -Path $PSScriptRoot\Classes\*.ps1 -ErrorAction SilentlyContinue -Recurse )
    $Enums = @( Get-ChildItem -Path $PSScriptRoot\Enums\*.ps1 -ErrorAction SilentlyContinue -Recurse )

    #Dot source the files
    Foreach ($Import in @($Private + $Classes + $Enums + $Public)) {
        Try {
            . $Import.Fullname
        } Catch {
            Write-Warning -Message "Failed to import functions from $($import.Fullname).Error: $($_.Exception.Message)"
            $true
        }
    }
)

if ($FoundErrors.Count -gt 0) {
    $ModuleName = (Get-ChildItem $PSScriptRoot\*.psd1).BaseName
    Write-Warning "Importing module $ModuleName failed. Fix errors before continuing."
    break
}

Export-ModuleMember -Function '*' -Alias '*' -Cmdlet '*'