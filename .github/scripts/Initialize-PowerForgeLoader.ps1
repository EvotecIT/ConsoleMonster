$ErrorActionPreference = 'Stop'

Import-Module Microsoft.PowerShell.PSResourceGet -ErrorAction Stop

$moduleRoot = Join-Path ([IO.Path]::GetTempPath()) 'consolemonster-powerforge'
$null = New-Item -ItemType Directory -Path $moduleRoot -Force
Save-PSResource -Name PSPublishModule -Version 3.0.192 -Repository PSGallery -Path $moduleRoot -TrustRepository -ErrorAction Stop

$loaderRoot = Join-Path $moduleRoot 'PSPublishModule/3.0.192/Loader'
$developmentRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../ConsoleMonster.PowerShell/bin/PowerForge.ModuleLoader'))
$null = New-Item -ItemType Directory -Path $developmentRoot -Force
Copy-Item -Path (Join-Path $loaderRoot '*') -Destination $developmentRoot -Recurse -Force -ErrorAction Stop
