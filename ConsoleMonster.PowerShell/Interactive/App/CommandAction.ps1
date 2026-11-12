param($rowXml, $argumentsXml)
$Row = [System.Management.Automation.PSSerializer]::Deserialize($rowXml)
$arguments = [System.Management.Automation.PSSerializer]::Deserialize($argumentsXml)
# Source stays in this child block so its parameters bind to $Row and @arguments in the same scope.
& {
__CONSOLE_ACTION_SOURCE__
} $Row @arguments
