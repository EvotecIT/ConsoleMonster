using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a menu from items (New-ConsoleMenuItem) for Show-ConsoleMenu.</summary>
    /// <remarks><para>The scriptblock runs once and its New-ConsoleMenuItem output becomes the items, in order. A menu can be shown as an app (Shell: title bar with the breadcrumb, sidebar, content pane, key footer), a centred menu (Classic) or a list in the scrollback (Inline); the layout can be set here or on Show-ConsoleMenu. Presets the app comes with (New-ConsoleMenu -Preset, emitted in the scriptblock or given with -Preset) are listed first by a New-ConsoleMenuItem -Presets item, marked as built in.</para></remarks>
    /// <example>
    /// <para>Build a small script menu</para>
    /// <code>
    /// $formOptions = @{
    ///     Title = 'Settings'
    ///     Content = {
    ///         $fieldOptions2 = @{
    ///             Key = 'Target'
    ///             Label = 'Target'
    ///             Default = 'Weekly'
    ///         }
    ///         New-ConsoleField @fieldOptions2
    ///         $fieldOptions = @{
    ///             Key = 'Compress'
    ///             Label = 'Compress'
    ///             Kind = 'Toggle'
    ///             Default = $true
    ///         }
    ///         New-ConsoleField @fieldOptions
    ///     }
    /// }
    /// $settings = New-ConsoleForm @formOptions
    /// $menuOptions2 = @{
    ///     Title = 'Backup helper'
    ///     Content = {
    ///         $menuItemOptions2 = @{
    ///             Label = 'Preview backup'
    ///             Hotkey = 'b'
    ///             Action = {
    ///                 $dashboardOptions = @{
    ///                     Title = 'Backup preview'
    ///                     Content = {
    ///                         $statusListOptions = @{
    ///                             WidgetId = 'steps'
    ///                             Content = { 'Inspect plan' }
    ///                         }
    ///                         New-ConsoleStatusList @statusListOptions
    ///                     }
    ///                 }
    ///                 $dashboard = New-ConsoleDashboard @dashboardOptions
    ///                 $null = $MenuContext.Run($dashboard, {
    ///                         $updateConsoleOptions = @{
    ///                             WidgetId = 'steps'
    ///                             Key = 'Inspect plan'
    ///                             Status = 'Pass'
    ///                         }
    ///                         Update-Console @updateConsoleOptions
    ///                         @{ Title = 'Backup preview'; Status = 'Pass'; Summary = 'No files were copied.' }
    ///                     })
    ///             }
    ///         }
    ///         New-ConsoleMenuItem @menuItemOptions2
    ///         $menuItemOptions = @{
    ///             Label = 'Settings'
    ///             Hotkey = 's'
    ///             Form = $settings
    ///         }
    ///         New-ConsoleMenuItem @menuItemOptions
    ///         New-ConsoleMenuItem -Exit
    ///     }
    /// }
    /// $menu = New-ConsoleMenu @menuOptions2
    /// $menuOptions = @{
    ///     Menu = $menu
    ///     Layout = 'Classic'
    /// }
    /// Show-ConsoleMenu @menuOptions</code>
    /// </example>
    /// <example>
    /// <para>Offer a preset for a menu form</para>
    /// <code>
    /// $formOptions = @{
    ///     Title = 'Settings'
    ///     Content = {
    ///         $fieldOptions2 = @{
    ///             Key = 'Target'
    ///             Label = 'Target'
    ///             Default = 'Weekly'
    ///         }
    ///         New-ConsoleField @fieldOptions2
    ///         $fieldOptions = @{
    ///             Key = 'Compress'
    ///             Label = 'Compress'
    ///             Kind = 'Toggle'
    ///             Default = $true
    ///         }
    ///         New-ConsoleField @fieldOptions
    ///     }
    /// }
    /// $settings = New-ConsoleForm @formOptions
    /// $menuOptions2 = @{
    ///     Title = 'Backup helper'
    ///     Content = {
    ///         [pscustomobject]@{ Name = 'Weekly'; Form = $settings; Values = @{ Target = 'Weekly'; Compress = $true }; Description = 'The Sunday backup.' }
    ///         $menuItemOptions2 = @{
    ///             Presets = $true
    ///             Label = 'Presets'
    ///             Hotkey = 'p'
    ///         }
    ///         New-ConsoleMenuItem @menuItemOptions2
    ///         $menuItemOptions = @{
    ///             Label = 'Settings'
    ///             Hotkey = 's'
    ///             Form = $settings
    ///         }
    ///         New-ConsoleMenuItem @menuItemOptions
    ///         New-ConsoleMenuItem -Exit
    ///     }
    /// }
    /// $menu = New-ConsoleMenu @menuOptions2
    /// $menuOptions = @{
    ///     Menu = $menu
    ///     Layout = 'Shell'
    /// }
    /// Show-ConsoleMenu @menuOptions</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see New-ConsoleMenu in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>
    /// Get-ConsoleDemo -Slug 'toolbox' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'fleet' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleMenu", HelpUri = "https://consolemonster.dev/powershell/commands/new-consolemenu/")]
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
        [PSDefaultValue(Help = "Auto")]
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
