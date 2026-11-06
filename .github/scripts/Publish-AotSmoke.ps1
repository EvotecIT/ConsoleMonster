<#
.SYNOPSIS
Publishes the native AOT smoke executable, retrying only when NuGet could not download a package.

.DESCRIPTION
The AOT lanes are the only ones that download the large ILCompiler runtime packages, and a short nuget.org outage
outlasts NuGet's own few seconds of retries. A trim, AOT or compile error fails on the first attempt.
#>
param(
    [Parameter(Mandatory)][string] $Framework,
    [Parameter(Mandatory)][string] $Runtime,
    [int] $Attempts = 3
)

for ($attempt = 1; ; $attempt++) {
    & dotnet publish Tests/AotSmoke/AotSmoke.csproj -c Release -f $Framework -r $Runtime -o Artefacts/AotSmoke 2>&1 |
        Tee-Object -Variable lines | Out-Host
    if ($LASTEXITCODE -eq 0) { return }
    $download = @($lines | Where-Object { "$_" -match 'error NU1301|error : Failed to download package' })
    if ($attempt -ge $Attempts -or $download.Count -eq 0) { exit $LASTEXITCODE }
    Write-Warning "NuGet could not download a package (attempt $attempt/$Attempts); restoring again in $(30 * $attempt) seconds."
    Start-Sleep -Seconds (30 * $attempt)
}
