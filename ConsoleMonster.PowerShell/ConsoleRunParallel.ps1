param($__cmItem, $__cmUsing)
foreach ($__cmPair in $__cmUsing.GetEnumerator()) {
    Set-Variable -Name $__cmPair.Key -Value $__cmPair.Value
}
# The body is inserted as a script block in this scope, including its own param() block and rewritten $using: references.
. { , $__cmItem | ForEach-Object -Process __CONSOLE_RUN_BODY__ } *>&1
