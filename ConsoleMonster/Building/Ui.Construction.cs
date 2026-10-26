using System;
using ConsoleMonster.Interactive;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Export;
using ConsoleMonster.Output;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

namespace ConsoleMonster {
    /// <summary>Configuration endings for factories whose identity or representation is fixed at construction.</summary>
    public static class UiConstruction {
        /// <summary>Creates the selected chart through the shared validated factory.</summary><param name="options">The configured definition.</param><returns>The chart.</returns>
        public static IWidget Build(this ChartOptions options) => Chart.Create(options);
        /// <summary>Creates determinate or indeterminate progress through the shared validated factory.</summary><param name="options">The configured definition.</param><returns>The indicator.</returns>
        public static IWidget Build(this ProgressOptions options) => Progress.Create(options);
        /// <summary>Materializes a numeric chart and verifies the concrete handle type.</summary><typeparam name="T">The expected renderer.</typeparam><param name="options">The definition.</param><returns>The typed renderer.</returns>
        public static T Build<T>(this ChartOptions options) where T : class, IWidget => Chart.Create(options) as T ?? throw new ArgumentException("The chart Type does not produce " + typeof(T).Name + ".", nameof(options));
        /// <summary>Materializes progress and verifies the concrete handle type.</summary><typeparam name="T">The expected renderer.</typeparam><param name="options">The definition.</param><returns>The typed renderer.</returns>
        public static T Build<T>(this ProgressOptions options) where T : class, IWidget => Progress.Create(options) as T ?? throw new ArgumentException("Indeterminate does not produce " + typeof(T).Name + ".", nameof(options));
        /// <summary>Creates single or multiple selection through the shared input factory.</summary><param name="options">The configured definition.</param><returns>The control.</returns>
        public static InputControl Build(this SelectInputOptions options) => AppInput.Select(options);
        /// <summary>Creates single-line or multiline text through the shared input factory.</summary><param name="options">The configured definition.</param><returns>The control.</returns>
        public static InputControl Build(this TextInputOptions options) => AppInput.Text(options);
        /// <summary>Creates a button through the shared input factory.</summary><param name="options">The configured definition.</param><returns>The button.</returns>
        public static Button Build(this ButtonInputOptions options) => AppInput.Button(options);
        /// <summary>Creates a toggle through the shared input factory.</summary><param name="options">The configured definition.</param><returns>The toggle.</returns>
        public static Toggle Build(this ToggleInputOptions options) => AppInput.Toggle(options);
        /// <summary>Creates a state binding after its callback and identity have been configured.</summary><param name="options">The configured definition.</param><returns>The binding.</returns>
        public static AppBinding Build(this BindingOptions options) => new AppBinding(options.Content ?? throw new ArgumentException("A binding needs Content.", nameof(options)), options.WidgetId);
        /// <summary>Creates an asynchronous action after its immutable identity and callback have been configured.</summary><param name="options">The configured definition.</param><returns>The command.</returns>
        public static AppCommand Build(this CommandOptions options) => new AppCommand(options.Key, options.Label ?? options.Key, options.Action ?? throw new ArgumentException("A command needs Action.", nameof(options))) { Shortcut = options.Shortcut, ConfirmPrompt = options.ConfirmPrompt };
        /// <summary>Creates an owned live-session lifetime over a configured widget; dispose it after running.</summary><param name="widget">The session root.</param><param name="options">The session options.</param><returns>The new session.</returns>
        public static LiveSession CreateSession(this IWidget widget, LiveSessionOptions? options = null) => new LiveSession(widget, options);
        /// <summary>Displays the configured definition through the shared widget pipeline.</summary><param name="definition">The configured definition.</param><param name="profile">The terminal profile.</param><param name="writer">The output writer.</param>
        public static void Show(this ChartOptions definition, TerminalProfile? profile = null, ITerminalWriter? writer = null) => definition.Build().Show(profile, writer);
        /// <summary>Exports the configured definition through the shared core/add-on dispatcher.</summary><param name="definition">The configured definition.</param><param name="path">The destination.</param><param name="format">The format.</param><param name="options">The export options.</param><param name="provider">An optional add-on provider.</param>
        public static void Export(this ChartOptions definition, string path, string? format = null, ExportOptions? options = null, IFrameExportProvider? provider = null) => definition.Build().Export(path, format, options, provider);
        /// <summary>Displays the configured definition through the shared widget pipeline.</summary><param name="definition">The configured definition.</param><param name="profile">The terminal profile.</param><param name="writer">The output writer.</param>
        public static void Show(this ProgressOptions definition, TerminalProfile? profile = null, ITerminalWriter? writer = null) => definition.Build().Show(profile, writer);
        /// <summary>Exports the configured definition through the shared core/add-on dispatcher.</summary><param name="definition">The configured definition.</param><param name="path">The destination.</param><param name="format">The format.</param><param name="options">The export options.</param><param name="provider">An optional add-on provider.</param>
        public static void Export(this ProgressOptions definition, string path, string? format = null, ExportOptions? options = null, IFrameExportProvider? provider = null) => definition.Build().Export(path, format, options, provider);
    }
}
