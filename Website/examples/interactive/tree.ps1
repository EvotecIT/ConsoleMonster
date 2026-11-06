Import-Module ConsoleMonster

$estate = [pscustomobject] @{
    Warsaw = @(
        [pscustomobject] @{ Name = 'WEB01'; Role = 'Web'; Patched = '2026-10-28' }
        [pscustomobject] @{ Name = 'SQL01'; Role = 'Database'; Patched = '2026-10-12' }
        [pscustomobject] @{ Name = 'FS01'; Role = 'File'; Patched = '2026-09-30' }
    )
    Berlin = @(
        [pscustomobject] @{ Name = 'WEB11'; Role = 'Web'; Patched = '2026-10-28' }
        [pscustomobject] @{ Name = 'SQL11'; Role = 'Database'; Patched = '2026-08-19' }
    )
}

Show-ConsoleApp -Title Inventory -Content {
    $treeSelectParameters = @{
        Key = 'Tree'
        Label = 'Servers'
        InputObject = $estate
        RootPath = '$estate'
        Height = 10
    }
    New-ConsoleTreeSelect @treeSelectParameters
    New-ConsoleBinding -Content {
        param($State)
        $path = if ($State.Tree) { $State.Tree.Path } else { 'nothing' }
        New-ConsoleText -Text "Selected: $path"
    }
}
