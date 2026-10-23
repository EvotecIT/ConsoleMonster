using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Views;

namespace ConsoleMonster.PowerShell {
    /// <summary>How objects of a registered type are shown by Enable-ConsoleFormatting (Register-ConsoleView).</summary>
    public sealed class ConsoleViewRegistration {
        internal ConsoleViewRegistration(string typeName, ConsoleViewKind view, object[]? property, object? groupBy, string group, ScriptBlock? title = null, bool builtIn = false) {
            TypeName = typeName;
            View = view;
            Property = property;
            GroupBy = groupBy;
            Group = group;
            Title = title;
            IsBuiltIn = builtIn;
        }

        /// <summary>Whether this is one of ConsoleMonster's own views (files and folders, processes, services).</summary>
        public bool IsBuiltIn { get; }

        /// <summary>
        /// A script block that gives each object's title ($_ is the object): when all objects of a view share one, it is the
        /// view's title, otherwise rows are grouped by it. Null for no title.
        /// </summary>
        public ScriptBlock? Title { get; }

        /// <summary>The type name (as in PSObject.TypeNames).</summary>
        public string TypeName { get; }

        /// <summary>The view.</summary>
        public ConsoleViewKind View { get; }

        /// <summary>The properties, as in Format-ConsoleTable, or null for the default display set.</summary>
        public object[]? Property { get; }

        /// <summary>A property name or script block that groups rows under headings, or null.</summary>
        public object? GroupBy { get; }

        /// <summary>Types with the same group are shown together in one view (files and folders, for example).</summary>
        public string Group { get; }
    }

    /// <summary>What Enable-ConsoleFormatting turns on; Disable-ConsoleFormatting turns the same parts off.</summary>
    [Flags]
    public enum ConsoleFormattingFeature {
        /// <summary>Nothing.</summary>
        None = 0,

        /// <summary>Registered and built-in types (files, processes, services, dictionaries) shown as ConsoleMonster views (the default).</summary>
        Output = 1,

        /// <summary>Errors that reach the console shown with ConsoleMonster's error view (Format-ConsoleError), following $ErrorView.</summary>
        ErrorView = 2,

        /// <summary>Write-Progress drawn as ConsoleMonster's progress area (Write-Progress becomes an alias of Write-ConsoleProgress).</summary>
        Progress = 4,

        /// <summary>Explicitly opts Format-Table, Format-List and Format-Wide into host-only rendering. Excluded from All.</summary>
        FormatCommands = 8,

        /// <summary>Native warning, verbose, debug and (PowerShell 7.2+) error colours from the active theme; stream commands and records stay native.</summary>
        Streams = 16,

            foreach (ConsoleFormattingFeature feature in new[] { ConsoleFormattingFeature.Output, ConsoleFormattingFeature.ErrorView, ConsoleFormattingFeature.Progress, ConsoleFormattingFeature.FormatCommands, ConsoleFormattingFeature.Streams, ConsoleFormattingFeature.Help, ConsoleFormattingFeature.Errors }) {
