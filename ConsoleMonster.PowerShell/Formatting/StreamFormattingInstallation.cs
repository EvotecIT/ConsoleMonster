using System;
using System.Collections.Generic;
using System.Management.Automation;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Owns only native stream colour properties successfully changed in one runspace.</summary>
    internal sealed class StreamFormattingInstallation {
        private readonly List<SavedProperty> _saved = new List<SavedProperty>();

        internal void Apply(PSCmdlet cmdlet) {
            ITheme theme = Terminal.Theme;
            // PSStyle was introduced in 7.2. Adapt its properties without binding the module to a newer SMA assembly.
            PSObject? formatting = ReadObject(cmdlet.SessionState.PSVariable.GetValue("PSStyle"), "Formatting");
            if (formatting != null) {
                SetStyle(formatting, "Warning", theme, ThemeToken.Warn);
                SetStyle(formatting, "Verbose", theme, ThemeToken.Info);
                SetStyle(formatting, "Debug", theme, ThemeToken.Muted);
                SetStyle(formatting, "Error", theme, ThemeToken.Fail);
                return;
            }

            // Older PowerShell hosts expose these optionally. Never replace a missing property or a stream command.
            PSObject? data;
            try {
                data = cmdlet.Host?.PrivateData;
            } catch (Exception error) when (Unavailable(error)) {
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
            } catch (Exception error) when (Unavailable(error)) {
                // Headless and remoting hosts need not implement RawUI.
            }
            SetHostStyle(data, "Warning", context.Style(ThemeToken.Warn), colors, background);
            SetHostStyle(data, "Verbose", context.Style(ThemeToken.Info), colors, background);
            SetHostStyle(data, "Debug", context.Style(ThemeToken.Muted), colors, background);
        }

        /// <summary>Restores the original baseline even if a caller subsequently edited an owned property.</summary>
        internal void Restore() {
            foreach (SavedProperty saved in _saved) {
                try {
                    saved.Property.Value = saved.Value;
                } catch (Exception error) when (Unavailable(error)) {
                    // A removed or disconnected host can make a formerly writable property unavailable.
                }
            }
            _saved.Clear();
        }

        private void SetStyle(PSObject target, string name, ITheme theme, ThemeToken token) {
            // These are PowerShell's style preferences, not output. PowerShell retains control over ANSI suppression,
            // redirection and transcripts; the terminal profile must not erase the preferences in a captured pipeline.
            Set(target, name, SgrEncoder.Encode(theme.Resolve(token, ColorDepth.TrueColor), ColorDepth.TrueColor));
        }

        private void SetHostStyle(PSObject target, string name, Style style, ConsoleColorMap colors, ConsoleColor? background) {
            // Stream roles need a concrete palette colour. The foreground mapper intentionally returns null for
            // text-like colours, which can share an ANSI16 entry with a HighContrast warning or information tone.
            Set(target, name + "ForegroundColor", colors.Background(style.Foreground));
            Set(target, name + "BackgroundColor", colors.Background(style.Background) ?? background);
        }

        private void Set(PSObject target, string name, object? value) {
            if (value == null) {
                return;
            }
            try {
                PSPropertyInfo? property = target.Properties[name];
                if (property == null || !property.IsGettable || !property.IsSettable) {
                    return;
                }
                object? original = property.Value;
                property.Value = value;
                // Reapplying a theme must not replace the baseline with the previous theme (or a user's edit).
                object owner = target.BaseObject is PSCustomObject ? target : target.BaseObject;
                if (!_saved.Exists(saved => ReferenceEquals(saved.Owner, owner) && saved.Property.Name == name)) {
                    _saved.Add(new SavedProperty(owner, property, original));
                }
            } catch (Exception error) when (Unavailable(error)) {
                // PrivateData is a host contract, not a guarantee that every colour property is writable.
            }
        }

        private static PSObject? ReadObject(object? target, string name) {
            if (target == null) {
                return null;
            }
            try {
                object? value = PSObject.AsPSObject(target).Properties[name]?.Value;
                return value == null ? null : PSObject.AsPSObject(value);
            } catch (Exception error) when (Unavailable(error)) {
                return null;
            }
        }

        private static bool Unavailable(Exception error) => error is RuntimeException || error is NotImplementedException
            || error is NotSupportedException || error is InvalidOperationException || error is ArgumentException;

        private sealed class SavedProperty {
            internal SavedProperty(object owner, PSPropertyInfo property, object? value) {
                Owner = owner;
                Property = property;
                Value = value;
            }

            internal object Owner { get; }
            internal PSPropertyInfo Property { get; }
            internal object? Value { get; }
        }
    }
}
