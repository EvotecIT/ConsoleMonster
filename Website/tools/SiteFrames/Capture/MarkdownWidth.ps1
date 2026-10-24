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
Show-ConsoleMarkdown -Content $content -Width $Width
'MARKDOWN_PAGER_DONE'
