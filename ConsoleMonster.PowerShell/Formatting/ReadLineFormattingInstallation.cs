using System.IO;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Colours the already loaded PSReadLine without importing it or changing editing and prediction behaviour.</summary>
    internal sealed class ReadLineFormattingInstallation {
        private readonly NativeFormattingProperties _properties = new NativeFormattingProperties();

        internal void Apply(PSCmdlet cmdlet) {
            TerminalProfile profile = Terminal.GetCurrentProfile();
            if (profile.ColorDepth == ColorDepth.None || string.Equals(
                NativeFormattingProperties.ReadObject(cmdlet.SessionState.PSVariable.GetValue("PSStyle"), "OutputRendering")?.BaseObject.ToString(),
                "PlainText", System.StringComparison.OrdinalIgnoreCase)) { return; }
            string script;
            using (Stream stream = typeof(ReadLineFormattingInstallation).Assembly.GetManifestResourceStream(
                "ConsoleMonster.PowerShell.Formatting.ReadLineOptions.ps1")!)
            using (StreamReader reader = new StreamReader(stream)) { script = reader.ReadToEnd(); }
            foreach (PSObject options in cmdlet.InvokeCommand.InvokeScript(script)) {
                RenderContext context = new RenderContext(profile, Terminal.Theme);
                Set(options, "Command", HighlightStyles.For(HighlightRole.Identifier, context));
                Set(options, "Parameter", HighlightStyles.For(HighlightRole.Key, context));
                Set(options, "String", HighlightStyles.For(HighlightRole.String, context));
                Set(options, "Variable", HighlightStyles.For(HighlightRole.Variable, context));
                Set(options, "Number", HighlightStyles.For(HighlightRole.Number, context));
                Set(options, "Operator", HighlightStyles.For(HighlightRole.Keyword, context));
                Set(options, "Comment", HighlightStyles.For(HighlightRole.Comment, context));
                Set(options, "Keyword", HighlightStyles.For(HighlightRole.Keyword, context));
                Set(options, "Type", HighlightStyles.For(HighlightRole.Type, context));
                Set(options, "Error", context.Style(ThemeToken.Fail));
                Set(options, "Selection", context.Style(ThemeToken.Focus));
                Set(options, "InlinePrediction", context.Style(ThemeToken.Muted));
            }
        }

        internal void Restore() => _properties.Restore();

        private void Set(PSObject options, string name, Style style) =>
            _properties.Set(options, name + "Color", SgrEncoder.Encode(style, Terminal.GetCurrentProfile().ColorDepth));
    }
}
