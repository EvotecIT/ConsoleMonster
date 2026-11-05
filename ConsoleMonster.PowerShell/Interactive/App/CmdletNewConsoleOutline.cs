using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using ConsoleMonster.Interactive;

namespace ConsoleMonster.PowerShell {
    /// <summary>Creates an outline for Show-ConsoleApp: a list of headings to jump between, such as settings groups or the sections of a long page.</summary>
    /// <remarks>The current entry is marked. Up/Down, Home/End and the wheel move the selection; Enter or a click opens it, which makes it current (the final state holds its title). An entry with a Target leads to that control: opening it focuses the control, scrolling a surrounding scroll view to it, and unfolds it when it is a collapsible section. New-ConsoleBreadcrumb -Outline shows the path to the current entry.</remarks>
    /// <example><para>Jump between the sections of a settings page.</para><code>$proxy = New-ConsoleCollapsible -Key Proxy -Content { New-ConsoleTextInput -Key ProxyServer -Label 'Server' }
    /// $dns = New-ConsoleCollapsible -Key Dns -Label 'DNS' -Content { New-ConsoleTextInput -Key DnsServers -Label 'Servers' }
    /// Show-ConsoleApp -Content {
    ///     New-ConsoleSplitter -Ratio 0.3 -First {
    ///         New-ConsoleOutline -Key Sections -Item @{ Title = 'Proxy'; Target = $proxy }, @{ Title = 'DNS'; Target = $dns }
    ///     } -Second { $proxy; $dns }
    /// }</code></example>
    [Cmdlet(VerbsCommon.New, "ConsoleOutline")]
    [OutputType(typeof(Outline))]
    public sealed class CmdletNewConsoleOutline : NoPipelineInputCmdlet {
        /// <summary><para type="description">Unique key in the application's state; the value is the current entry's title.</para></summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Key { get; set; } = string.Empty;

        /// <summary><para type="description">The entries: titles, hashtables with Title, Level (1 for the top) and Target (a control to open), or OutlineItem objects.</para></summary>
        [Parameter(Mandatory = true, Position = 1)]
        [AllowEmptyCollection]
        public object[] Item { get; set; } = Array.Empty<object>();

        /// <summary><para type="description">The index of the entry that starts current; -1 (the default) for none.</para></summary>
        [Parameter]
        public int CurrentIndex { get; set; } = -1;

        /// <summary><para type="description">Disables the outline and removes it from keyboard focus traversal.</para></summary>
        [Parameter]
        public SwitchParameter Disabled { get; set; }

        /// <summary><para type="description">Keyboard focus order. Equal values preserve layout declaration order.</para></summary>
        [Parameter]
        public int TabIndex { get; set; }

        /// <summary><para type="description">Runs once each time an entry is opened, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.</para></summary>
        [Parameter]
        public ScriptBlock? OnChange { get; set; }

        /// <inheritdoc />
        protected override void EndProcessing() {
            OutlineInputOptions options = new OutlineInputOptions { Key = Key, CurrentIndex = CurrentIndex, Disabled = Disabled.IsPresent, TabIndex = TabIndex };
            foreach (object? entry in Item) { options.Items.Add(ToItem(PSText.Unwrap(entry))); }
            Outline outline = AppInput.Outline(options);
            PSAppCallbacks.Register(outline, OnChange);
            WriteObject(outline);
        }

        private static OutlineItem ToItem(object? entry) {
            switch (entry) {
                case OutlineItem item: return item;
                case string title: return new OutlineItem(title);
                case IDictionary map:
                    string? text = null;
                    int level = 1;
                    InputControl? target = null;
                    foreach (DictionaryEntry pair in map) {
                        string name = Convert.ToString(pair.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                        object? value = PSText.Unwrap(pair.Value);
                        if (string.Equals(name, "Title", StringComparison.OrdinalIgnoreCase)) { text = Convert.ToString(value, CultureInfo.InvariantCulture); }
                        else if (string.Equals(name, "Level", StringComparison.OrdinalIgnoreCase)) { level = Convert.ToInt32(value, CultureInfo.InvariantCulture); }
                        else if (string.Equals(name, "Target", StringComparison.OrdinalIgnoreCase)) { target = value as InputControl ?? throw new PSArgumentException("An outline entry's Target must be a control, such as New-ConsoleCollapsible output."); }
                        else { throw new PSArgumentException("Unknown outline entry key '" + name + "'. Use Title, Level and Target."); }
                    }
                    if (string.IsNullOrEmpty(text)) { throw new PSArgumentException("An outline entry needs a Title."); }
                    return new OutlineItem(text!, level) { Target = target };
                default:
                    throw new PSArgumentException("Outline entries are titles, hashtables with Title, Level and Target, or OutlineItem objects.");
            }
        }
    }
}