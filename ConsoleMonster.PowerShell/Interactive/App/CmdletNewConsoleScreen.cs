using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a screen for Show-ConsoleApp: its content, its breadcrumb title and the keys that work on it.</para>
    /// <para type="description">Put New-ConsoleScreen commands next to each other in the app content: together they are one screen stack, and the first is where the app starts. To open a screen, assign its key to $State.Screen in a key binding or callback; assigning a screen that is already open goes back to it. Escape or Backspace returns to the previous screen when the focused input does not use them. Inputs inside a screen keep their values while other screens show, and each screen remembers its focused input. The app header (Show-ConsoleApp -Title) shows the open screens as breadcrumbs, and the footer lists the keys that work on the current screen.</para>
    /// </summary>
    /// <example>
    /// <para>Open a server from a numbered list, and go back with Escape</para>
    /// <code>$servers = @(
    ///     [pscustomobject]@{ Name = 'web01'; Role = 'Web'; Status = 'Pass' }
    ///     [pscustomobject]@{ Name = 'sql01'; Role = 'Database'; Status = 'Fail' }
    /// )
    /// Show-ConsoleApp -Title Servers -Content {
    ///     New-ConsoleScreen -Key list -Title Servers -KeyBinding ([ordered]@{
    ///         '1-2' = 'open', { param($Number, $State) $State.Server = $Number; $State.Screen = 'server' }
    ///     }) -Content {
    ///         'Type a number to open a server.'
    ///         $servers | New-ConsoleTable -Property Name, Role, Status -StatusProperty Status
    ///     }
    ///     New-ConsoleScreen -Key server -Title { $servers[$State.Server - 1].Name } -Content {
    ///         New-ConsoleBinding -Content { $servers[$State.Server - 1] | New-ConsoleKeyValue }
    ///     }
    /// }</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleScreen")]
    [OutputType(typeof(AppScreen))]
    public sealed class CmdletNewConsoleScreen : NoPipelineInputCmdlet {
        /// <summary><para type="description">The key that opens the screen, unique within the app. The current key is $State.Screen.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">Inputs and widgets on the screen, built once when the app starts. Use New-ConsoleBinding inside it for parts that change with state.</para></summary>
        [Parameter(Mandatory = true, Position = 1)]
        public ScriptBlock Content { get; set; } = null!;

        /// <summary><para type="description">The breadcrumb title: text, or a script block that builds it from $State (the item a screen shows). Defaults to the key.</para></summary>
        [Parameter] public object? Title { get; set; }

        /// <summary><para type="description">Keys that work on this screen, as an ordered dictionary of key to 'label', { action }. The action receives $State; '1-12' opens numbered items and passes the number first: param($Number, $State).</para></summary>
        [Parameter] public object? KeyBinding { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            List<IWidget> content = PSWidgets.Collect(Content);
            ScriptBlock? titleScript = PSText.Unwrap(Title) as ScriptBlock;
            string title = titleScript != null || Title == null ? Key : PSWidgets.ToText(Title);
            AppScreen screen = new AppScreen(Key, title, content.Count == 1 ? content[0] : new Rows(content.ToArray()));
            if (titleScript != null) { PSScreens.SetTitle(screen, titleScript); }
            foreach (KeyBinding binding in PSKeyBindings.Read(KeyBinding)) { screen.Bindings.Add(binding); }
            WriteObject(screen);
        }
    }
}
