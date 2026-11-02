using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Lets PowerShell fit its evaluated wide records, including its native autosize and ellipsis rules.</summary>
    internal static class NativeWideView {
        internal static bool TryBuild(PSCmdlet cmdlet, IReadOnlyList<PSObject> records, out IWidget? widget) {
            widget = null;
            bool wide = false;
            foreach (PSObject record in records) {
                if (record.BaseObject.GetType().Name != "FormatStartData") { continue; }
                object? shape = record.Properties["shapeInfo"]?.Value;
                if ((shape is PSObject wrapped ? wrapped.BaseObject : shape)?.GetType().Name != "WideViewHeaderInfo") { return false; }
                wide = true;
            }
            if (!wide) { return false; }
            // Native records already hold evaluated strings: this does not evaluate source properties again. Using the
            // owning engine avoids Table's padding, width redistribution and narrow-column hiding changing Wide's fit.
            Collection<PSObject> output = cmdlet.InvokeCommand.InvokeScript(
                "param($records, $width) $records | Microsoft.PowerShell.Utility\\Out-String -Width $width", true,
                System.Management.Automation.Runspaces.PipelineResultTypes.None, null, records, HostWidth.Resolve(cmdlet.Host, Terminal.GetCurrentProfile()));
            string text = output.Count == 0 ? string.Empty : Convert.ToString(output[0].BaseObject) ?? string.Empty;
            widget = TextBlock.Plain(text.Trim('\r', '\n'));
            return true;
        }
    }
}
