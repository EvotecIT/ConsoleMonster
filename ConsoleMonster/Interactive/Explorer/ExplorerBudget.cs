using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Caps cached nodes across every branch of one explorer, including branches later collapsed.</summary>
    internal sealed class ExplorerBudget {
        public ExplorerBudget(int maxNodes) { Remaining = Math.Max(1, maxNodes) - 1; }
        public int Remaining { get; private set; }
        public bool Take() {
            if (Remaining == 0) { return false; }
            Remaining--;
            return true;
        }
    }
}
