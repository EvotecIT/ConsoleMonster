function New-SpectreImage {
    <#
    .SYNOPSIS
    Displays a raster image in the terminal.

    .DESCRIPTION
    Loads a local image or downloads one from a URI and renders it on a Spectre canvas.
    Image decoding uses the managed raster support in OfficeIMO.Core.

    .PARAMETER FilePath
    Path to a local raster image.

    .PARAMETER Uri
    URI of the raster image to download and display.

    .PARAMETER MaxWidth
    Maximum width of the rendered image in pixels.

    .EXAMPLE
    New-SpectreImage -FilePath '.\photo.png' -MaxWidth 40
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ParameterSetName = 'File')][string] $FilePath,
        [Parameter(Mandatory, ParameterSetName = 'Url')][Uri] $Uri,
        [ValidateRange(1, [int]::MaxValue)][int] $MaxWidth
    )
    $DownloadedPath = $null
    try {
        if ($PSCmdlet.ParameterSetName -eq 'Url') {
            $DownloadedPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.IO.Path]::GetRandomFileName())
            $FilePath = $DownloadedPath
            $ProgressPreference = 'SilentlyContinue'
            try {
                Invoke-WebRequest -Uri $Uri -OutFile $FilePath -UseBasicParsing -ErrorAction Stop
            } catch {
                Write-Warning -Message "Failed to download image from $($Uri.AbsoluteUri)"
                return
            }
        }
        $ResolvedPath = (Get-Item -LiteralPath $FilePath -ErrorAction Stop).FullName
        if ($PSBoundParameters.ContainsKey('MaxWidth')) {
            $Image = [ConsoleMonster.SpectreImageRenderer]::Load($ResolvedPath, $MaxWidth)
        } else {
            $Image = [ConsoleMonster.SpectreImageRenderer]::Load($ResolvedPath)
        }
        [Spectre.Console.AnsiConsole]::Write($Image)
    } finally {
        if ($DownloadedPath -and (Test-Path -LiteralPath $DownloadedPath)) {
            Remove-Item -LiteralPath $DownloadedPath -ErrorAction Stop
        }
    }
}
