param(
    [Parameter(Position = 0, ValueFromPipeline = $true, ValueFromRemainingArguments = $true)][object[]] $Object,
    [switch] $NoNewline,
    [object] $Separator = ' ',
    [System.ConsoleColor] $ForegroundColor,
    [System.ConsoleColor] $BackgroundColor
)
$__cmProgressBridge.Host((@($Object | ForEach-Object {
    if ($_ -is [System.Collections.IEnumerable] -and $_ -isnot [string]) {
        $_ -join $Separator
    } else {
        "$_"
    }
}) -join $Separator), $NoNewline.IsPresent, $PSBoundParameters['ForegroundColor'], $PSBoundParameters['BackgroundColor'])
