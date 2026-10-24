using System;

namespace ConsoleMonster.Demos {
    /// <summary>The presentation action assigned to a key.</summary>
    public enum DemoPresenterAction {
        /// <summary>Ignore the key.</summary>
        None,
        /// <summary>Type the command and execute it.</summary>
        Run,
        /// <summary>Type the command without executing it.</summary>
        Type,
        /// <summary>Move to the previous command; executed side effects are not undone.</summary>
        Back,
        /// <summary>Skip the current command.</summary>
        Skip,
        /// <summary>Stop the demo and its owned child.</summary>
        Quit
    }
    /// <summary>Presenter controls independent of terminal key encodings.</summary>
    public static class DemoPresenter {
        /// <summary>Maps Enter, Space, b, s and q to presentation actions.</summary>
        public static DemoPresenterAction Action(ConsoleKeyInfo key) {
            if (key.Key == ConsoleKey.Enter) { return DemoPresenterAction.Run; }
            if (key.Key == ConsoleKey.Spacebar) { return DemoPresenterAction.Type; }
            switch (char.ToLowerInvariant(key.KeyChar)) {
                case 'b': return DemoPresenterAction.Back;
                case 's': return DemoPresenterAction.Skip;
                case 'q': return DemoPresenterAction.Quit;
                default: return DemoPresenterAction.None;
            }
        }
    }
}
