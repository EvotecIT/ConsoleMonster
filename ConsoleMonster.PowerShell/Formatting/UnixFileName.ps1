$name = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.Markup'))::Escape($_.Name)
$tone = if ($_.UnixMode -like 'l*') {
    'accent2'
} elseif ($_.PSIsContainer) {
    'accent'
} elseif ($_.UnixMode -match 'x') {
    'pass'
}
if ($tone) {
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.TableCell'))::new(('[' + $tone + ']' + $name + '[/]'))
} else {
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.TableCell'))::new($name)
}
