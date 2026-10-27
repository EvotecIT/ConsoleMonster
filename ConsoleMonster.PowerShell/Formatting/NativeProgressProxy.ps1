process {
    # Native command metadata supplies the parameters on each PowerShell host.
    $percent = if ($PSBoundParameters.ContainsKey('PercentComplete')) { $PercentComplete } else { -1 }
    $remaining = if ($PSBoundParameters.ContainsKey('SecondsRemaining')) { $SecondsRemaining } else { -1 }
    $parent = if ($PSBoundParameters.ContainsKey('ParentId')) { $ParentId } else { -1 }
    [ConsoleMonster.PowerShell.NativeProgress]::Write(
        $PSCmdlet,
        $Id,
        $parent,
        $Activity,
        $Status,
        $CurrentOperation,
        $percent,
        $remaining,
        $Completed.IsPresent
    )
}
