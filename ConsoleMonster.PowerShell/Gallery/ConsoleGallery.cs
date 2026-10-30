using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleMonster.Canvas;
using ConsoleMonster.Rendering;

namespace ConsoleMonster.PowerShell {
    /// <summary>The embedded PowerShell gallery host service. Execution and metadata conversion use the caller's owning runspace.</summary>
    public static class ConsoleGallery {
        /// <summary>Lists embedded gallery metadata using native PowerShell wildcard and JSON semantics.</summary>
        /// <param name="slug">An exact slug or wildcard pattern.</param>
        /// <param name="invokeCommand">Command invocation from the owning runspace.</param>
        /// <returns>The matching metadata records.</returns>
        public static IReadOnlyList<PSObject> List(string slug, CommandInvocationIntrinsics invokeCommand) {
            if (invokeCommand == null) { throw new ArgumentNullException(nameof(invokeCommand)); }
            var pattern = new WildcardPattern(slug, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant);
            var results = new List<PSObject>();
            bool found = false;
            foreach (string candidate in PSDemoResources.Slugs()) {
                if (!pattern.IsMatch(candidate)) { continue; }
                found = true;
                foreach (PSObject metadata in invokeCommand.InvokeScript("param($json) Microsoft.PowerShell.Utility\\ConvertFrom-Json -InputObject $json -ErrorAction Stop",
                    true, System.Management.Automation.Runspaces.PipelineResultTypes.None, null, PSDemoResources.Metadata(candidate))) {
                    metadata.TypeNames.Insert(0, WidgetFormatData.DemoTypeName);
                    metadata.Properties.Add(new PSNoteProperty("Source", PSDemoResources.Script(candidate)));
                    results.Add(metadata);
                }
            }
            if (!found && !WildcardPattern.ContainsWildcardCharacters(slug)) { throw NotFound(slug); }
            return results;
        }

        /// <summary>Runs a demo in a child scope, or displays a deterministic state through the existing capture harness.</summary>
        /// <param name="slug">The exact demo slug.</param>
        /// <param name="invokeCommand">Invocation from the owning runspace, which already has the module loaded.</param>
        /// <param name="staticSnapshot">Whether to capture a deterministic state.</param>
        /// <param name="step">The capture state, starting at zero.</param>
        /// <returns>Ordinary demo output; snapshots write directly to the current terminal target.</returns>
        public static Collection<PSObject> Show(string slug, CommandInvocationIntrinsics invokeCommand, bool staticSnapshot = false, int step = 0) {
            if (invokeCommand == null) { throw new ArgumentNullException(nameof(invokeCommand)); }
            if (step < 0) { throw new ArgumentOutOfRangeException(nameof(step)); }
            if (!staticSnapshot && step != 0) { throw new ArgumentException("Use Step with StaticSnapshot.", nameof(step)); }
            string resolved = Resolve(slug);
            // Standalone exports need their import; the in-process runner already has the
            // caller's module and must not resolve a different installed version by name.
            string source = Regex.Replace(PSDemoResources.Script(resolved),
                @"(?im)^[ \t]*Import-Module[ \t]+(?:-Name[ \t]+)?ConsoleMonster[ \t]*(?=\r?$)", string.Empty);
            ScriptBlock demo = ScriptBlock.Create(source);
            if (staticSnapshot) {
                ScriptBlock capture = ScriptBlock.Create(PSDemoResources.CaptureScript());
                ScriptBlock invokeCapture = ScriptBlock.Create("param($capture, $demo, $json, $step) & $capture -Demo $demo -Metadata (Microsoft.PowerShell.Utility\\ConvertFrom-Json -InputObject $json) -Step $step");
                foreach (PSObject result in invokeCommand.InvokeScript(true, invokeCapture, null, capture, demo, PSDemoResources.Metadata(resolved), step)) {
                    if (result.BaseObject is CellBuffer frame) {
                        using (ITerminalOutput output = Terminal.CreateOutput(Terminal.GetCurrentProfile(), Terminal.Target.Writer)) { output.WriteBlock(frame); }
                    }
                }
                return new Collection<PSObject>();
            }
            return invokeCommand.InvokeScript(true, ScriptBlock.Create("param($demo) & $demo"), null, demo);
        }

        private static string Resolve(string slug) => PSDemoResources.Find(slug) ?? throw NotFound(slug);
        private static ItemNotFoundException NotFound(string slug) => new ItemNotFoundException("Unknown ConsoleMonster demo '" + slug + "'. Use Get-ConsoleDemo to list available slugs.");
    }
}
