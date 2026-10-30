using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.</para>
    /// <para type="description">The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenu -Preset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.</para>
    /// </summary>
    /// <example>
    /// <para>Build a small script menu</para>
    /// <code>$settings = New-ConsoleForm 'Settings' -Content {
    ///     New-ConsoleField Target 'Target' -Default Weekly
    ///     New-ConsoleField Compress 'Compress' -Type Toggle -Default $true
    /// }
    /// $menu = New-ConsoleMenu 'Backup helper' -Content {
    ///     New-ConsoleMenuItem 'Preview backup' -Hotkey b -Action {
    ///         $dashboard = New-ConsoleDashboard -Title 'Backup preview' -Content { New-ConsoleStatusList -WidgetId steps -Content { 'Inspect plan' } }
    ///         $null = $MenuContext.Run($dashboard, {
    ///             Update-Console steps -Key 'Inspect plan' -Status Pass
    ///             @{ Title = 'Backup preview'; Status = 'Pass'; Summary = 'No files were copied.' }
    ///         })
    ///     }
    ///     New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings
    ///     New-ConsoleMenuItem -Exit
    /// }
    /// Show-ConsoleMenu $menu -Layout Classic</code>
    /// </example>
    /// <example>
    /// <para>Offer a preset for a menu form</para>
    /// <code>$settings = New-ConsoleForm 'Settings' -Content {
    ///     New-ConsoleField Target 'Target' -Default Weekly
    ///     New-ConsoleField Compress 'Compress' -Type Toggle -Default $true
    /// }
    /// $menu = New-ConsoleMenu 'Backup helper' -Content {
    ///     [pscustomobject]@{ Name = 'Weekly'; Form = $settings; Values = @{ Target = 'Weekly'; Compress = $true }; Description = 'The Sunday backup.' }
    ///     New-ConsoleMenuItem 'Presets' -Hotkey p -Presets
    ///     New-ConsoleMenuItem 'Settings' -Hotkey s -Form $settings
    ///     New-ConsoleMenuItem -Exit
    /// }
    /// Show-ConsoleMenu $menu -Layout Shell</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleMenu")]
    [OutputType(typeof(Menu))]
    public sealed class CmdletNewConsoleMenu : PSCmdlet {
        private readonly List<PSObject> _piped = new List<PSObject>();
        /// <summary><para type="description">Menu items or preset records from the pipeline.</para></summary>
        [Parameter(ValueFromPipeline = true)] public PSObject? InputObject { get; set; }
        /// <inheritdoc />
        protected override void ProcessRecord() { if (InputObject != null) { _piped.Add(InputObject); } }
        /// <summary><para type="description">The title (the app name for the top menu).</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Title { get; set; } = string.Empty;

        /// <summary><para type="description">A scriptblock whose New-ConsoleMenuItem output becomes the items (and New-ConsoleMenu -Preset output the built-in presets).</para></summary>
        [Parameter(Position = 1)]
        public ScriptBlock? Content { get; set; }

        /// <summary><para type="description">Presets the app comes with (New-ConsoleMenu -Preset), listed before the saved ones by a -Presets item.</para></summary>
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
            List<MenuPreset> presets = new List<MenuPreset>();
            if (Preset != null) {
                foreach (object preset in Preset) { presets.Add(PSPresets.Menu(preset, this)); }
            }
            foreach (MenuItem item in PSMenus.CollectItems(Content, presets, this)) {
                menu.Add(item);
            }
            foreach (MenuItem item in PSMenus.CollectItems(_piped, presets, this)) { menu.Add(item); }
            foreach (MenuPreset preset in presets) {
                menu.Presets.Add(preset);
            }
            WriteObject(menu);
        }
    }
}
