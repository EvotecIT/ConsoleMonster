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

        RunResult Run(MenuContext context, IWidget dashboard, Func<RunContext, RunResult?> work, LiveSessionOptions? options);

        void ShowResult(RunResult result);

        void Open(MenuItem item);

        void Exit();

        void Log(string text, StatusKind status);
    }
}
