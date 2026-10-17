using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// What an action gets from the menu: the item, the submitted form values, prompts drawn over the app, toasts, a live
    /// dashboard run that returns to the menu with a results summary, and navigation.
    /// </summary>
    public sealed class MenuContext {
        private readonly IMenuHost _host;

        internal MenuContext(IMenuHost host, Menu menu, MenuItem? item, FormValues? values, CancellationToken cancellationToken) {
            _host = host;
            Menu = menu;
            Item = item;
            Values = values;
            CancellationToken = cancellationToken;
        }

        /// <summary>The root menu.</summary>
        public Menu Menu { get; }

        /// <summary>The item whose action runs, if any.</summary>
        public MenuItem? Item { get; }

        /// <summary>The values of the submitted form, if the action follows a form.</summary>
        public FormValues? Values { get; }

        /// <summary>The form result, if the action follows a form.</summary>
        public FormResult? FormResult { get; internal set; }

        /// <summary>Cancelled when the menu ends (or Esc cancels an asynchronous action).</summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>The menu layout in use.</summary>
        public MenuLayout Layout => _host.Layout;

        /// <summary>Shows a short notice in the footer.</summary>
        /// <param name="text">The text.</param>
        /// <param name="status">The status (mark and colour).</param>
        public void Toast(string text, StatusKind status = StatusKind.Pass) => _host.ShowToast(new Toast(text, status));

        /// <summary>Asks a prompt in front of the app and returns the answer.</summary>
        /// <typeparam name="T">The answer type.</typeparam>
        /// <param name="prompt">The prompt.</param>
        /// <returns>The answer.</returns>
        public T Ask<T>(Prompt<T> prompt) => _host.Ask(prompt, CancellationToken);

        /// <summary>Asks a yes/no question in front of the app.</summary>
        /// <param name="question">The question.</param>
        /// <param name="defaultValue">The default answer.</param>
        /// <returns>The answer.</returns>
        public bool Confirm(string question, bool defaultValue = false) => Ask(new ConfirmPrompt(question, defaultValue));

        /// <summary>
        /// Runs work with a live dashboard on the app's screen; when the work returns, the menu comes back and shows the
        /// result with its next actions. Prompts during the run pause the dashboard and appear in front of it.
        /// </summary>
        /// <param name="dashboard">The dashboard.</param>
        /// <param name="work">The work; returns the result to show (null shows a plain "done").</param>
        /// <param name="options">Live session options; the output is managed by the menu.</param>
        /// <returns>The result.</returns>
        public RunResult Run(IWidget dashboard, Func<RunContext, RunResult?> work, LiveSessionOptions? options = null) => _host.Run(this, dashboard, work, options);

        /// <summary>Shows a result page (summary and next actions) in the content area.</summary>
        /// <param name="result">The result.</param>
        public void ShowResult(RunResult result) => _host.ShowResult(result);

        /// <summary>Opens an item (its submenu, form, selection or page).</summary>
        /// <param name="item">The item.</param>
        public void Open(MenuItem item) => _host.Open(item);

        /// <summary>Leaves the menu.</summary>
        public void Exit() => _host.Exit();

        /// <summary>
        /// Reports a line of output from the action. While the app owns the screen the line is kept and shown when the
        /// action returns (a toast for one line, an output dialog for several) instead of being drawn over the app; in the
        /// inline layout it is written to the console.
        /// </summary>
        /// <param name="text">The line.</param>
        /// <param name="status">Info for plain output, Warn or Fail to colour it.</param>
        public void Log(string text, StatusKind status = StatusKind.Info) => _host.Log(text ?? string.Empty, status);
    }
}
