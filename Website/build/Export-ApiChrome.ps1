# Lifts the site header and footer out of the pages built at the API reference routes, so the API docs steps wrap
# their pages in exactly the markup every other page uses. Run after the site build and before the API docs steps.
param(
    [string]$Site = './_site',
    [string]$Out = './_temp/chrome'
)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $Out | Out-Null
foreach ($route in @(@{ Path = 'api'; Name = 'api' }, @{ Path = 'powershell/commands'; Name = 'powershell-commands' })) {
    $page = Join-Path $Site (Join-Path $route.Path 'index.html')
    $html = [IO.File]::ReadAllText((Resolve-Path $page))
    $header = [regex]::Match($html, '(?s)<a class="cm-skip".*?</header>')
    $footer = [regex]::Match($html, '(?s)<footer class="cm-footer">.*?</footer>')
    if (-not $header.Success -or -not $footer.Success) { throw "No site header or footer in $page." }
    # The API docs step requires a navigation placeholder in custom fragments. The visible header is already complete,
    # so the placeholder sits in an inert template that browsers never render.
    $marker = "<template data-cm-api-nav>{{NAV_ACTIONS}}</template>"
    [IO.File]::WriteAllText((Join-Path $Out "$($route.Name)-header.html"), $header.Value + "`n" + $marker + "`n")
    [IO.File]::WriteAllText((Join-Path $Out "$($route.Name)-footer.html"), $footer.Value + "`n")
    # Reference pages share the site's identity. Keep route-specific WebPage and breadcrumb nodes out of this fragment:
    # their URLs describe the temporary chrome page, not each generated type or command page.
    $identity = @([regex]::Matches($html, '(?s)<script\b[^>]*type="application/ld\+json"[^>]*>(.*?)</script>') |
        Where-Object { ($_.Groups[1].Value | ConvertFrom-Json).'@type' -in @('WebSite', 'Organization') } |
        ForEach-Object { $_.Value })
    if ($identity.Count -ne 2) { throw "Expected website and publisher structured data in $page." }
    # The shared site script versions its runtime requests from this page metadata, including on reference pages.
    $runtime = [regex]::Match($html, '<meta\b[^>]*name="cm-runtime"[^>]*>')
    if (-not $runtime.Success) { throw "No runtime asset version in $page." }
    [IO.File]::WriteAllText((Join-Path $Out "$($route.Name)-head.html"), $runtime.Value + "`n" + ($identity -join "`n") + "`n")
}
Write-Output "API chrome written to $Out"
