using System;
using System.Collections.Generic;
using System.Globalization;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>What the settings picker ended with.</summary>
    internal enum SettingsPickerAction {
        /// <summary>Escape or Cancel: nothing changes.</summary>
        Cancel,
        /// <summary>Use the choices in this session only.</summary>
        Apply,
        /// <summary>Use the choices and save them to the settings file.</summary>
        Save
    }

    /// <summary>
    /// The settings picker: a choice for each setting on the left and a preview on the right that redraws as the
    /// highlight moves, so a theme or glyph set can be seen before it is chosen. Settings an environment variable sets are
    /// shown locked with the variable's name.
    /// </summary>
    internal static class SettingsPicker {
        private const string ActionKey = "settings.action";

        internal sealed class Result {
            internal Result(SettingsPickerAction action, IReadOnlyDictionary<string, string> values) { Action = action; Values = values; }
            internal SettingsPickerAction Action { get; }
            // The chosen value of every setting that is not locked.
            internal IReadOnlyDictionary<string, string> Values { get; }
        }

        internal static Result Run(ConsoleAppOptions? options = null, System.Threading.CancellationToken cancellationToken = default) {
            IReadOnlyList<ConsoleSetting> settings = ConsoleSettings.Current;
            AppState state = new AppState();
            Dictionary<string, Select> selects = new Dictionary<string, Select>(StringComparer.Ordinal);
            Rows choices = new Rows();
            int labelWidth = 0;
            foreach (ConsoleSetting setting in settings) { labelWidth = Math.Max(labelWidth, setting.Name.Length + 2); }
            foreach (ConsoleSetting setting in settings) {
                List<Choice> list = new List<Choice>();
                foreach (string value in Values(setting)) {
                    list.Add(new Choice(value, Label(setting, value)));
                }
                // One row per setting: its name, then a dropdown without a frame of its own.
                Select select = new Select(setting.Name, list) { Label = setting.Name, Dropdown = true, Border = Glyphs.BoxStyle.None, LinkKey = "settings.link." + setting.Name };
                select.Value = setting.Value;
                bool locked = setting.Source == ConsoleSettingSource.Environment;
                if (locked) { select.Disabled = true; }
                selects[setting.Name] = select;
                TextBlock label = new TextBlock((locked ? "[muted]" : "[bold]") + Markup.Escape(setting.Name) + "[/]");
                choices.Add(new Columns().Add(label, LayoutSize.Fixed(labelWidth)).Add(select, LayoutSize.Fraction()));
            }
            Button save = new Button("settings.save", "Save", false);
            Button apply = new Button("settings.apply", "Use now", false);
            Button cancel = new Button("settings.cancel", "Cancel", false);
            save.Clicked += (_, __) => { state[ActionKey] = SettingsPickerAction.Save; state.RequestExit(); };
            apply.Clicked += (_, __) => { state[ActionKey] = SettingsPickerAction.Apply; state.RequestExit(); };
            cancel.Clicked += (_, __) => { state[ActionKey] = SettingsPickerAction.Cancel; state.RequestExit(); };
            Columns buttons = new Columns().Add(save, LayoutSize.Fixed(10)).Add(apply, LayoutSize.Fixed(13)).Add(cancel, LayoutSize.Fixed(12));
            Rows left = new Rows(new Panel(choices, "Settings"), buttons, new AppBinding(current => Notes(settings, selects))) { Gap = 1 };
            AppBinding preview = new AppBinding(current => Preview(Chosen(settings, selects, current)), "settings.preview");
            Columns body = new Columns().Add(left, LayoutSize.Fraction(2, 30)).Add(new Panel(preview, "Preview"), LayoutSize.Fraction(3, 40));
            Rows layout = new Rows(
                new TextBlock("[bold]ConsoleMonster settings[/] [muted]· the preview follows your choices · Save keeps them for every session[/]"),
                body) { Gap = 1 };
            ConsoleAppOptions run = options ?? new ConsoleAppOptions();
            // Each setting is a label beside its dropdown; keep that row side by side down to a narrow terminal.
            run.ReflowBelow = Math.Min(run.ReflowBelow, 24);
            ConsoleApp.Run(layout, state, run, cancellationToken);

            Dictionary<string, string> chosen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ConsoleSetting setting in settings) {
                if (setting.Source != ConsoleSettingSource.Environment && selects[setting.Name].Value is string value) { chosen[setting.Name] = value; }
            }
            SettingsPickerAction action = state.Get(ActionKey, SettingsPickerAction.Cancel);
            return new Result(action, chosen);
        }

        // Applies a result: each value that differs from what is in effect is set (and saved); a choice that means detection
        // removes the stored value instead. Literal choices (including Comfortable) remain explicit.
        internal static IReadOnlyList<string> Apply(Result result) {
            List<string> changed = new List<string>();
            if (result.Action == SettingsPickerAction.Cancel) { return changed; }
            bool save = result.Action == SettingsPickerAction.Save;
            foreach (KeyValuePair<string, string> pair in result.Values) {
                ConsoleSetting current = ConsoleSettings.Get(pair.Key);
                bool differs = !string.Equals(current.Value, pair.Value, StringComparison.OrdinalIgnoreCase);
                if (string.Equals(pair.Value, current.DefaultValue, StringComparison.OrdinalIgnoreCase) && ConsoleSettings.UsesDetection(pair.Key, pair.Value)) {
                    if (differs || (save && current.Source == ConsoleSettingSource.File) || current.Source == ConsoleSettingSource.Session) {
                        ConsoleSettings.Reset(pair.Key, save);
                        if (differs) { changed.Add(pair.Key); }
                    }
                } else if (differs || current.Source == ConsoleSettingSource.Default || (save && current.Source != ConsoleSettingSource.File)) {
                    ConsoleSettings.Set(pair.Key, pair.Value, save);
                    if (differs) { changed.Add(pair.Key); }
                }
            }
            return changed;
        }

        private static IEnumerable<string> Values(ConsoleSetting setting) {
            bool listed = false;
            foreach (string value in setting.Values) {
                if (string.Equals(value, setting.Value, StringComparison.OrdinalIgnoreCase)) { listed = true; }
                yield return value;
            }
            // A theme file, a culture or a variable's own spelling that is not one of the listed values.
            if (!listed) { yield return setting.Value; }
        }

        private static string Label(ConsoleSetting setting, string value) {
            if (setting.Name == "Theme" && (value.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase))) {
                return System.IO.Path.GetFileName(value) + " (file)";
            }
            if (string.Equals(value, setting.DefaultValue, StringComparison.OrdinalIgnoreCase)) { return value + " (default)"; }
            if ((setting.Name == "Language" || setting.Name == "Culture") && value != "Invariant") {
                try { return value + " · " + CultureInfo.GetCultureInfo(value).NativeName; } catch (CultureNotFoundException) { }
            }
            return value;
        }

        // What the focused setting does, and where to change a locked one.
        private static IWidget Notes(IReadOnlyList<ConsoleSetting> settings, Dictionary<string, Select> selects) {
            List<string> lines = new List<string>();
            foreach (ConsoleSetting setting in settings) {
                if (selects[setting.Name].Focused) { lines.Add(Markup.Escape(setting.Description)); }
            }
            if (lines.Count == 0) {
                lines.Add("Save keeps the choices for every session; Use now applies them to this one. Escape leaves everything as it was.");
            }
            foreach (ConsoleSetting setting in settings) {
                if (setting.Source == ConsoleSettingSource.Environment) {
                    lines.Add(Markup.Escape(setting.Name) + " is set by " + Markup.Escape(setting.Variable) + "; clear it to choose here.");
                }
            }
            return new TextBlock("[muted]" + string.Join("\n", lines) + "[/]");
        }

        // The committed values, except that the focused setting shows its highlighted choice, so browsing an open list
        // previews each entry before it is chosen.
        private static Dictionary<string, string> Chosen(IReadOnlyList<ConsoleSetting> settings, Dictionary<string, Select> selects, AppState state) {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (ConsoleSetting setting in settings) {
                Select select = selects[setting.Name];
                string value = select.Value as string ?? setting.Value;
                if (select.Focused && state.Get<int?>("settings.link." + setting.Name, null) is int index) {
                    List<string> listed = new List<string>(Values(setting));
                    if (index >= 0 && index < listed.Count) { value = listed[index]; }
                }
                values[setting.Name] = value;
            }
            return values;
        }

        // Fixed sample content that shows every status token, a panel, a progress bar, a chart and an error, drawn with
        // the chosen theme, colours, glyphs, culture and language.
        internal static IWidget Preview(IReadOnlyDictionary<string, string> values) {
            Table table = new Table();
            table.AddColumn("Check").AddColumn("State")
                .AddColumn(new TableColumn("Size") { Presenter = Presenters.CellPresenters.Bytes, Alignment = HorizontalAlignment.Right })
                .AddColumn(new TableColumn("Changed") { Presenter = Presenters.CellPresenters.Date, Priority = 1 });
            DateTime when = new DateTime(2026, 11, 9, 14, 30, 0);
            table.AddRow("Backups", TableCell.ForStatus(StatusKind.Pass, "passed"), TableCell.FromValue(52_428_800L), TableCell.FromValue(when));
            table.AddRow("Certificates", TableCell.ForStatus(StatusKind.Warn, "expiring"), TableCell.FromValue(4_096L), TableCell.FromValue(when.AddDays(-3)));
            table.AddRow("Replication", TableCell.ForStatus(StatusKind.Fail, "failed"), TableCell.FromValue(1_288_490_189L), TableCell.FromValue(when.AddHours(-5)));
            StatusList status = new StatusList()
                .Add("Build", StatusKind.Pass, "1 m 12 s")
                .Add("Tests", StatusKind.Warn, "2 skipped")
                .Add("Deploy", StatusKind.Fail, "timed out")
                .Add("Notify", StatusKind.Running);
            ProgressBar progress = new ProgressBar("Copying") { Value = 64, Total = 100 };
            BarChart chart = new BarChart();
            chart.Add("web", 42).Add("sql", 87).Add("files", 23);
            IWidget content = new Rows(
                table,
                new Columns(status, new Rows(progress, chart)),
                new TextBlock("[fail]Error:[/] [muted]Cannot reach[/] [accent]sql01.contoso.test[/] [muted](port 1433)[/] [link=https://consolemonster.dev/]docs[/]")) { Gap = 1 };
            return new PreviewScope(content, values);
        }

        // Draws a widget as the chosen settings would, whatever is in effect now.
        private sealed class PreviewScope : IContainer {
            private readonly IWidget _child;
            private readonly IReadOnlyDictionary<string, string> _values;

            internal PreviewScope(IWidget child, IReadOnlyDictionary<string, string> values) { _child = child; _values = values; }

            public IReadOnlyList<IWidget> Children => new[] { _child };

            public Size Measure(Constraints constraints, RenderContext context) => _child.Measure(constraints, Scoped(context));

            public void Render(Region region, RenderContext context) => _child.Render(region, Scoped(context));

            private RenderContext Scoped(RenderContext context) {
                TerminalProfile profile = context.Profile;
                switch (Value("Color")) {
                    case "None": profile = profile.WithColorDepth(ColorDepth.None); break;
                    case "16": profile = profile.WithColorDepth(ColorDepth.Ansi16); break;
                    case "256": profile = profile.WithColorDepth(ColorDepth.Ansi256); break;
                    case "TrueColor": profile = profile.WithColorDepth(ColorDepth.TrueColor); break;
                }
                switch (Value("Glyphs")) {
                    case "Full": profile = profile.WithGlyphs(GlyphTier.Full); break;
                    case "Basic": profile = profile.WithGlyphs(GlyphTier.Basic); break;
                    case "Ascii": profile = profile.WithGlyphs(GlyphTier.Ascii); break;
                }
                // Auto leaves the frame and markers to the previewed theme.
                string border = Value("Border");
                profile = profile.WithBorder(border == "Auto" || border.Length == 0 ? (BoxStyle?)null : (BoxStyle)Enum.Parse(typeof(BoxStyle), border));
                string markers = Value("StatusMarkers");
                profile = profile.WithStatusMarkers(markers == "Auto" || markers.Length == 0 ? (StatusMarkerStyle?)null : (StatusMarkerStyle)Enum.Parse(typeof(StatusMarkerStyle), markers));
                string density = Value("Density");
                profile = profile.WithDensity(density.Length == 0 ? (ThemeDensity?)null : (ThemeDensity)Enum.Parse(typeof(ThemeDensity), density));
                string culture = Value("Culture");
                if (culture == "Invariant") { profile = profile.WithCulture(CultureInfo.InvariantCulture); }
                else if (culture != "Current" && culture.Length > 0) { profile = profile.WithCulture(Culture(culture) ?? profile.Culture); }
                string language = Value("Language");
                if (language != "Auto" && language.Length > 0) { profile = profile.WithLanguage(Culture(language) ?? profile.Language); }
                IGlyphSet glyphs = ReferenceEquals(profile, context.Profile) || profile.Glyphs == context.Profile.Glyphs ? context.Glyphs : GlyphSet.For(profile.Glyphs);
                return new RenderContext(profile, ThemeFor(Value("Theme"), context), glyphs, context.Tick, context.Elapsed);
            }

            private string Value(string name) => _values.TryGetValue(name, out string? value) ? value : string.Empty;

            private static CultureInfo? Culture(string name) {
                try { return CultureInfo.GetCultureInfo(name); } catch (CultureNotFoundException) { return null; }
            }

            private static ITheme ThemeFor(string name, RenderContext context) {
                switch (name) {
                    case "Auto": return Terminal.ResolveTheme(Terminal.Profile.WithThemePreference(ThemePreference.Auto));
                    case "ColorBlindSafe": return Terminal.ResolveTheme(Terminal.Profile.WithThemePreference(ThemePreference.ColorBlind));
                    case "Dark": case "Light": case "HighContrast": case "ColorBlindSafeLight": case "ColorBlindSafeDark":
                        return Theme.FromName(name);
                    default:
                        // A theme file: the one in effect when it is the current setting, otherwise the active theme.
                        return ConsoleSettings.ThemeFromPath() ?? context.Theme;
                }
            }
        }
    }
}
