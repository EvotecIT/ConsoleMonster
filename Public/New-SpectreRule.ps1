function New-SpectreRule {
    [cmdletBinding()]
    param(
        [Array] $Text,
        [Array] $Color,
        [Alias('Justification')][Spectre.Console.Justify] $Align,
        [validateSet('bold', 'dim', 'italic', 'underline', 'invert', 'conceal', 'slowblink', 'rapidblink', 'strikethrough')][string] $RuleStyle,
        [string] $RuleColor
    )
    $DefaultColor = $Color[0]
    $PreparedText = if ($Text.Count -ne 0) {
        for ($i = 0; $i -lt $Text.Length; $i++ ) {
            if ($Color[$i]) {
                "[$($Color[$i])]$($Text[$i])[/]"
            } elseif ($DefaultColor) {
                "[$($DefaultColor)]$($Text[$i])[/]"
            } else {
                "$($Text[$i])"
            }
        }
    }
    $rule = [Spectre.Console.Rule]::new($PreparedText)
    $rule.Justification = $Align
    if ($RuleColor -and $Style) {
        $rule.Style = "$RuleColor $style"
    } elseif ($RuleColor) {
        $Rule.Style = $RuleColor
    } elseif ($Style) {
        $Rule.Style = $Style
    }
    [Spectre.Console.AnsiConsole]::Render($rule)
}