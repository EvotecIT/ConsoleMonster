using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.</para>
    /// <para type="description">The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenuPreset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.</para>
    /// </summary>
    /// <example>
    ///   <para>A small script menu</para>
    ///   <code>
    /// $menu = New-ConsoleMenu 'Backup helper' -Content {
    ///     New-ConsoleMenuItem 'Back up now' -Hotkey b -Action { Start-Backup }
    ///     New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings
    ///     New-ConsoleMenuItem -Exit
    /// }
    /// Show-ConsoleMenu $menu -Layout Classic</code>
    /// </example>
    /// <example>
    ///   <para>An app with built-in presets for its form</para>
    ///   <code>
    /// $menu = New-ConsoleMenu 'Backup helper' -Content {
    ///     New-ConsoleMenuPreset 'Weekly' -Form $settings -Values @{ Target = 'Weekly'; Compress = $true } -Description 'The Sunday backup.'
    ///     New-ConsoleMenuItem 'Presets' -Hotkey p -Presets
    ///     New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings
    ///     New-ConsoleMenuItem -Exit
    /// }
    /// Show-ConsoleMenu $menu -Layout Shell</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleMenu")]
    [OutputType(typeof(Menu))]
    public sealed class CmdletNewConsoleMenu : PSCmdlet {
        /// <summary><para type="description">The title (the app name for the top menu).</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Title { get; set; } = string.Empty;

        /// <summary><para type="description">A scriptblock whose New-ConsoleMenuItem output becomes the items (and New-ConsoleMenuPreset output the built-in presets).</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Items created beforehand.</para></summary>
        [Parameter]
        public MenuItem[]? Item { get; set; }

        /// <summary><para type="description">Presets the app comes with (New-ConsoleMenuPreset), listed before the saved ones by a -Presets item.</para></summary>
        [Parameter]
        public object[]? Preset { get; set; }

        /// <summary><para type="description">A sentence shown above the items.</para></summary>
        [Parameter]
        public string? Description { get; set; }

        /// <summary><para type="description">Muted text at the right of the footer (a version, an environment).</para></summary>
        [Parameter]
        public string? Subtitle { get; set; }

        /// <summary><para type="description">The default layout for Show-ConsoleMenu: Auto, Classic, Shell or Inline.</para></summary>
        [Parameter]
        public MenuLayout Layout { get; set; } = MenuLayout.Auto;

        /// <inheritdoc />
        protected override void EndProcessing() {
            Menu menu = new Menu(Title) { Description = Description, Subtitle = Subtitle, Layout = Layout };
            if (Item != null) {
                foreach (MenuItem item in Item) {
                    menu.Add(item);
                }
            }
            List<MenuPreset> presets = new List<MenuPreset>();
            if (Preset != null) {
                foreach (object preset in Preset) { presets.Add(PSPresets.Menu(preset, this)); }
            }
            foreach (MenuItem item in PSMenus.CollectItems(Content, presets, this)) {
                menu.Add(item);
            }
            foreach (MenuPreset preset in presets) {
                menu.Presets.Add(preset);
            }
            WriteObject(menu);
        }
    }
}
