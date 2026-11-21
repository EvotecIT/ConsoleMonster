using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates a named region whose content can be replaced while a dashboard runs.</summary>
    /// <remarks><para>Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Update-Console -WidgetId with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.</para></remarks>
    /// <example>
    ///   <para>Build a panel with an indeterminate spinner as its initial content. WidgetId main identifies the region for a later Update-Console -Content call. WidgetId spinner identifies the progress widget currently inside it.</para>
    ///   <code>$panelOptions = @{
    ///     Title = 'Result'
    ///     Content = {
    ///         $regionOptions = @{
    ///             WidgetId = 'main'
    ///             Content = {
    ///                 $progressOptions = @{
    ///                     Indeterminate = $true
    ///                     Label = 'Querying'
    ///                     WidgetId = 'spinner'
    ///                 }
    ///                 New-ConsoleProgress @progressOptions
    ///             }
    ///         }
    ///         New-ConsoleRegion @regionOptions
    ///     }
    /// }
    /// New-ConsolePanel @panelOptions</code>
    /// </example>
    /// <example>
    /// <para>Center Ready and leave one cell of padding on each side. Padding is an integer array. The one-entry form uses that entry for all four sides. With two entries the order is horizontal then vertical. With four entries it is left, top, right, bottom.</para>
    /// <code>$regionOptions = @{
    ///     Horizontal = 'Center'
    ///     Padding = @(1)
    ///     Content = {
    ///         $textOptions = @{
    ///             Text = @('Ready')
    ///         }
    ///         New-ConsoleText @textOptions
    ///     }
    /// }
    /// New-ConsoleRegion @regionOptions | Show-Console</code>
    /// </example>
    /// <example>
    /// <para>Read any of these gallery scripts to see New-ConsoleRegion in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.</para>
    /// <code>Get-ConsoleDemo -Slug 'build-steps' | Select-Object -ExpandProperty Source
    /// Get-ConsoleDemo -Slug 'log-tail' | Select-Object -ExpandProperty Source</code>
    /// </example>
    [Cmdlet(VerbsCommon.New, "ConsoleRegion", HelpUri = "https://consolemonster.dev/powershell/commands/new-consoleregion/")]
    [OutputType(typeof(IWidget))]
    public sealed class CmdletNewConsoleRegion : ContainerCmdlet {
        /// <summary><para type="description">The region name used by Update-Console.</para></summary>
        [Parameter(Position = 0)]
        public string WidgetId { get; set; } = string.Empty;

        /// <summary><para type="description">A script block whose widgets are the first content.</para></summary>
        [Parameter(Position = 1, ValueFromPipeline = true)]
        [AllowNull, AllowEmptyCollection]
        public override object? Content { get; set; }

        /// <summary><para type="description">The share of the remaining space in a row or column layout.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "0")]
        public double Weight { get; set; }

        /// <summary><para type="description">A fixed height in rows.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "0")]
        public int Height { get; set; }

        /// <summary><para type="description">Takes the remaining height.</para></summary>
        [Parameter]
        [PSDefaultValue(Help = "$false")]
        public SwitchParameter Fill { get; set; }

        /// <summary><para type="description">Horizontal placement within the available space.</para></summary>
        [Parameter] [PSDefaultValue(Help = "Left")]
        public HorizontalAlignment Horizontal { get; set; } = HorizontalAlignment.Left;
        /// <summary><para type="description">Vertical placement within the available space.</para></summary>
        [Parameter] [PSDefaultValue(Help = "Top")]
        public VerticalAlignment Vertical { get; set; } = VerticalAlignment.Top;
        /// <summary><para type="description">Padding as one value for all sides, two for horizontal and vertical, or four for left, top, right and bottom.</para></summary>
        [Parameter, ValidateCount(1, 4), ValidateRange(0, int.MaxValue)]
        public int[]? Padding { get; set; }

        /// <inheritdoc />
        protected override IWidget Build(List<IWidget> children) {
            IWidget? content = children.Count == 0 ? null : children.Count == 1 ? children[0] : new Rows(children.ToArray());
            if (content != null && (Horizontal != HorizontalAlignment.Left || Vertical != VerticalAlignment.Top)) { content = new Align(content, Horizontal, Vertical); }
            if (Padding != null) {
                if (Padding.Length == 3) { throw new PSArgumentException("Padding takes one, two or four values."); }
                int left = Padding[0], top = Padding.Length == 1 ? left : Padding[1];
                int right = Padding.Length < 4 ? left : Padding[2], bottom = Padding.Length < 4 ? top : Padding[3];
                content = new ConsoleMonster.Layout.Padding(content ?? new Rows(), left, top, right, bottom);
            }
            IWidget region = string.IsNullOrEmpty(WidgetId) ? content ?? new Rows() : new RegionWidget(WidgetId, content);
            return PSWidgets.WithSize(region, Weight, Height, Fill.IsPresent);
        }
    }
}
