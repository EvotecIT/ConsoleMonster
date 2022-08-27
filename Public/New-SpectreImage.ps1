function New-SpectreImage {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string] $FilePath,
        [int] $MaxWidth
    )
    $Image = [Spectre.Console.CanvasImage]::new($FilePath)
    if ($MaxWidth) {
        $Image.MaxWidth = $MaxWidth
    }
    [Spectre.Console.AnsiConsole]::Write($Image)
}