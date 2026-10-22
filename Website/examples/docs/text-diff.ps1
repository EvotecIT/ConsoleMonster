Import-Module ConsoleMonster

# Compare configuration text under Before and After headings with one line of context.
$before = @(
    '# Web service'
    'host=web-01'
    'port=80'
    'protocol=http'
    'workers=4'
    'timeout=30'
    'retries=3'
    'log=information'
    'health=/status'
    'enabled=true'
    ''
) -join "`n"
$after = @(
    '# Web service'
    'host=web-01'
    'port=443'
    'protocol=https'
    'workers=4'
    'timeout=30'
    'retries=3'
    'log=information'
    'health=/status'
    'enabled=true'
    ''
) -join "`n"
Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1
