[CmdletBinding()]
param(
    [Parameter(Mandatory, ParameterSetName = 'Prepare')]
    [string] $ModuleDirectory,
    [Parameter(Mandatory, ParameterSetName = 'Run')]
    [string] $ProfileDirectory,
    [Parameter(ParameterSetName = 'Run')]
    [string] $Channel,
    [Parameter(ParameterSetName = 'Run')]
    [switch] $Visible
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$version = '3.0.10'

if ($PSCmdlet.ParameterSetName -eq 'Prepare') {
    New-Item -ItemType Directory -Path $ModuleDirectory -Force | Out-Null
    Save-PSResource -Name PSParseHTML -Version $version -Repository PSGallery -Path $ModuleDirectory -TrustRepository
    return
}

Import-Module -FullyQualifiedName @{ ModuleName = 'PSParseHTML'; RequiredVersion = $version }
$launch = @{
    Url = 'about:blank'
    Browser = 'Chromium'
    UserDataDirectory = $ProfileDirectory
    BrowserArgument = @('--remote-debugging-port=0', '--remote-debugging-address=127.0.0.1', '--force-renderer-accessibility')
    LoadState = 'DomContentLoaded'
    Visible = $Visible
    NoDefault = $true
}
if ($Channel) {
    $launch.BrowserChannel = $Channel
}

$session = Start-HtmlBrowserSession @launch
try {
    $port = [int](Get-Content -LiteralPath (Join-Path $ProfileDirectory 'DevToolsActivePort') -TotalCount 1)
    if ($port -lt 1 -or $port -gt 65535) {
        throw 'HtmlTinkerX Chromium did not provide a valid loopback port.'
    }
    [IO.File]::WriteAllText((Join-Path $ProfileDirectory 'managed-browser-port'), [string]$port)
    # The Node parent holds stdin open for the lifetime of the measurement.
    [void][Console]::ReadLine()
} finally {
    Close-HtmlBrowserSession -Session $session
}
