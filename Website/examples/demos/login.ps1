# The login overview a Linux server shows over SSH, with invented data so it runs anywhere.
#region hidden
Write-Host '$ ssh deploy@app-web-03' -ForegroundColor Yellow
Start-Sleep -Milliseconds 900
#endregion
& ../../../Module/Examples/Example-LoginOverview.ps1 -Sample -Width 96
