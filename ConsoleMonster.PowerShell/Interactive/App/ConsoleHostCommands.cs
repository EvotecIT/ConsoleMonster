using System;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>C# access to the PowerShell host's isolated operation adapter. Plain .NET hosts construct AppCommand with a typed asynchronous action.</summary>
    public static class ConsoleHostCommands {
        /// <summary>Creates an in-app action without running its operation or capturing closure state.</summary>
        /// <param name="options">Stable identity, operation source and explicit runspace inputs.</param>
        /// <returns>The core action definition shared by buttons, grids and apps.</returns>
        public static AppCommand Create(ConsoleCommandOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (options.ScriptBlock == null) { throw new ArgumentException("ScriptBlock is required.", nameof(options)); }
            AppCommand command = PSCommandActions.Create(options.Key, options.Label ?? options.Key, options.ScriptBlock.ToString(), options.ArgumentList, options.ImportModule);
            command.ConfirmPrompt = options.ConfirmPrompt;
            command.ProgressDialog = options.ProgressDialog;
            if (!string.IsNullOrWhiteSpace(options.Shortcut)) { command.Shortcut = KeyChord.Parse(options.Shortcut!); }
            return command;
        }
    }
}
