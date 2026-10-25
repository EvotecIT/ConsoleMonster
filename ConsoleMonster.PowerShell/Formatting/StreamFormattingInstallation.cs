using System;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Owns native stream and progress colour properties successfully changed in one runspace.</summary>
    internal sealed class StreamFormattingInstallation {
        private readonly NativeFormattingProperties _properties = new NativeFormattingProperties();

        internal void Apply(PSCmdlet cmdlet) {
            ITheme theme = Terminal.Theme;
            // PSStyle was introduced in 7.2. Adapt its properties without binding the module to a newer SMA assembly.
            PSObject? formatting = NativeFormattingProperties.ReadObject(cmdlet.SessionState.PSVariable.GetValue("PSStyle"), "Formatting");
            if (formatting != null) {
                SetStyle(formatting, "Warning", theme, ThemeToken.Warn);
                SetStyle(formatting, "Verbose", theme, ThemeToken.Info);
                SetStyle(formatting, "Debug", theme, ThemeToken.Muted);
                SetStyle(formatting, "Error", theme, ThemeToken.Fail);
                PSObject? progress = NativeFormattingProperties.ReadObject(cmdlet.SessionState.PSVariable.GetValue("PSStyle"), "Progress");
                if (progress != null) { SetStyle(progress, "Style", theme, ThemeToken.Running); }
                return;
            }

            // Older PowerShell hosts expose these optionally. Never replace a missing property or a stream command.
            PSObject? data;
            try {
                data = cmdlet.Host?.PrivateData;
            } catch (Exception error) when (NativeFormattingProperties.Unavailable(error)) {
                return;
            }
            if (data == null) {
                return;
            }
            RenderContext context = new RenderContext(Terminal.GetCurrentProfile().WithColorDepth(ColorDepth.Ansi16), theme);
            ConsoleColorMap colors = new ConsoleColorMap(context);
            ConsoleColor? background = null;
            try {
                background = cmdlet.Host?.UI?.RawUI?.BackgroundColor;
            } catch (Exception error) when (NativeFormattingProperties.Unavailable(error)) {
                // Headless and remoting hosts need not implement RawUI.
            }
            SetHostStyle(data, "Warning", context.Style(ThemeToken.Warn), colors, background);
            SetHostStyle(data, "Verbose", context.Style(ThemeToken.Info), colors, background);
            SetHostStyle(data, "Debug", context.Style(ThemeToken.Muted), colors, background);
            SetHostStyle(data, "Progress", context.Style(ThemeToken.Running), colors, background);
        }

        internal void Restore() => _properties.Restore();

        private void SetStyle(PSObject target, string name, ITheme theme, ThemeToken token) {
            // These are PowerShell's style preferences, not output. PowerShell retains control over ANSI suppression,
            // redirection and transcripts; the terminal profile must not erase the preferences in a captured pipeline.
            _properties.Set(target, name, SgrEncoder.Encode(theme.Resolve(token, ColorDepth.TrueColor), ColorDepth.TrueColor));
        }

        private void SetHostStyle(PSObject target, string name, Style style, ConsoleColorMap colors, ConsoleColor? background) {
            // Stream roles need a concrete palette colour. The foreground mapper intentionally returns null for
            // text-like colours, which can share an ANSI16 entry with a HighContrast warning or information tone.
            _properties.Set(target, name + "ForegroundColor", colors.Background(style.Color));
            _properties.Set(target, name + "BackgroundColor", colors.Background(style.BackgroundColor) ?? background);
        }

    }
}
