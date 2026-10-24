using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Interactive {
    /// <summary>The menu runner's services for <see cref="MenuContext"/>.</summary>
    internal interface IMenuHost {
        MenuLayout Layout { get; }

        void ShowToast(Toast toast);

        T Ask<T>(Prompt<T> prompt, CancellationToken cancellationToken);

        MenuRunResult Run(MenuContext context, IWidget dashboard, Func<RunContext, MenuRunResult?> work, LiveSessionOptions? options);

        void ShowResult(MenuRunResult result);

        void Open(MenuItem item);

        void Exit();

        void Log(string text, StatusKind status);
    }
}
