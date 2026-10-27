[CmdletBinding()]
param([Parameter(Mandatory = $true)] [string] $ResultsPath)
$ErrorActionPreference = 'Stop'
$script:StreamReceipts = New-Object 'System.Collections.Generic.List[object]'
$script:Route = ''
function Receive-OrderingProbe {
    [CmdletBinding()]
    param([Parameter(ValueFromPipeline = $true)] [psobject] $InputObject)
    process {
        $baseObject = $InputObject.PSObject.BaseObject
        $type = $baseObject.GetType().FullName
        $text = if ($baseObject -is [string]) { [string]$baseObject } elseif ($baseObject -is [Management.Automation.ErrorRecord]) { $baseObject.Exception.Message } else { [string]$baseObject.Message }
        $script:StreamReceipts.Add([pscustomobject]@{Route=$script:Route;Type=$type;Text=$text})
        [Console]::Out.WriteLine('SINK ' + $script:Route + ' ' + $type + ' ' + $text)
    }
}
$script:Route = 'merged'
& { 'before'; Write-Error 'error' -ErrorAction Continue; Write-Warning 'warning'; Write-Verbose 'verbose' -Verbose; 'after' } 2>&1 3>&1 4>&1 | Receive-OrderingProbe
$script:Route = 'unmerged'
& { 'before'; Write-Error 'error' -ErrorAction Continue; Write-Warning 'warning'; Write-Verbose 'verbose' -Verbose; 'after' } | Receive-OrderingProbe
$merged = @($script:StreamReceipts | Where-Object Route -eq merged)
$unmerged = @($script:StreamReceipts | Where-Object Route -eq unmerged)
if (($merged.Text -join ',') -cne 'before,error,warning,verbose,after') { throw 'Merged stream order changed.' }
if (($unmerged.Text -join ',') -cne 'before,after') { throw 'Unexpected unmerged stream record reached the sink.' }
if ($merged[1].Type -cne 'System.Management.Automation.ErrorRecord' -or $merged[2].Type -cne 'System.Management.Automation.WarningRecord' -or $merged[3].Type -cne 'System.Management.Automation.VerboseRecord') { throw 'Merged streams lost their native record identity.' }
$flags = [Reflection.BindingFlags]'Instance,Public,NonPublic'
$property = $Host.UI.GetType().GetProperty('IsTranscribing', $flags)
if (-not $property -or $property.PropertyType -ne [bool]) { throw 'No readable transcript state property.' }
$before = [bool]$property.GetValue($Host.UI, $null)
$transcript = [IO.Path]::ChangeExtension($ResultsPath, 'transcript.txt')
try {
    Start-Transcript -LiteralPath $transcript | Out-Null
    $during = [bool]$property.GetValue($Host.UI, $null)
} finally { Stop-Transcript | Out-Null }
$after = [bool]$property.GetValue($Host.UI, $null)
if ($before -or -not $during -or $after) { throw 'Transcript state did not follow actual recording lifecycle.' }
[pscustomobject]@{
    Runtime=$PSVersionTable.PSVersion.ToString(); Receipts=$script:StreamReceipts.ToArray();
    Transcript=[pscustomobject]@{Before=$before;During=$during;After=$after};
    Conclusion='Merged error/warning/verbose records reach the sink in producer order. Unmerged records bypass it. Automatic Collect remains native.'
} | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $ResultsPath -Encoding UTF8
