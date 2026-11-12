$ErrorActionPreference = 'Stop'
# The module path is a quoted literal: the child shell receives only an encoded command, not script arguments.
Import-Module __CONSOLE_RECORDING_MODULE_PATH__
# Source stays in the original child block so its variables and param() binding retain their scope.
& {
__CONSOLE_RECORDING_SOURCE__
}
