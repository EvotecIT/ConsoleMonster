<#
.SYNOPSIS
Runs the .NET integration project from PowerShell.
.DESCRIPTION
Create the project and save Program.cs from either C# tab first. The .NET host and logging provider own this lifecycle;
there is no separate PowerShell hosting implementation.
.PARAMETER Project
The project file or directory containing the saved C# program (default current directory).
#>
[CmdletBinding()]
param([string] $Project = '.')

& dotnet run --project $Project -c Release
if ($LASTEXITCODE -ne 0) { throw "The .NET example exited with code $LASTEXITCODE." }
