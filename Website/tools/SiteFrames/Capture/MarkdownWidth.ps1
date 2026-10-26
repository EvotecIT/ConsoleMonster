<#
.SYNOPSIS
Exercises the public Markdown pager on a physical terminal wider than its document.
.PARAMETER ModulePath
The development module manifest.
.PARAMETER Width
The requested document width, independent of the physical console.
#>
param([string]$ModulePath, [int]$Width)
$ErrorActionPreference = 'Stop'
Import-Module $ModulePath -Force
$content = ('alpha beta gamma delta epsilon zeta ' * 40)
Show-Console -Content $content -Width $Width -View Markdown
'MARKDOWN_PAGER_DONE'
