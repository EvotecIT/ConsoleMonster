function New-SpectreImage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ParameterSetName = 'File')][string] $FilePath,
        [Parameter(Mandatory, ParameterSetName = 'Url')][alias()][Uri] $Uri,
        [int] $MaxWidth
    )
    if ($Uri) {
        $Extension = [IO.Path]::GetExtension($Uri.AbsolutePath)
        $FilePath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "$($([System.IO.Path]::GetRandomFileName()).Split('.')[0]).$Extension")
        $ProgressPreference = 'SilentlyContinue'
        Try {
            Invoke-WebRequest -Uri $Uri -OutFile $FilePath -ErrorAction Stop
        } Catch {
            Write-Warning -Message "Failed to download image from $($Uri.AbsoluteUri)"
            Return
        }

    }
    if ($FilePath -and (Test-Path -Path $FilePath)) {
        $Image = [Spectre.Console.CanvasImage]::new($FilePath)
    }
    if ($MaxWidth) {
        $Image.MaxWidth = $MaxWidth
    }
   # $Image = [Spectre.Console.CanvasImageExtensions]::BicubicResampler($Image)
   # $Image

    [Spectre.Console.AnsiConsole]::Write($Image)
}