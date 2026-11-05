namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Who the keys reach: the focused input, the shown screen and the open dialog. A context menu and a press on a footer
    /// hint belong to the scope that listed them; once another input, screen or dialog takes the keys, the same chord and
    /// label may name another binding, so they no longer run. Redraws keep an equal scope.
    /// </summary>
    internal sealed class KeyScope {
        private readonly object? _focused, _screen, _modal;
        internal KeyScope(object? focused, object? screen, object? modal) { _focused = focused; _screen = screen; _modal = modal; }
        public override bool Equals(object? obj) => obj is KeyScope other
            && ReferenceEquals(_focused, other._focused) && ReferenceEquals(_screen, other._screen) && ReferenceEquals(_modal, other._modal);
        public override int GetHashCode() => (_focused?.GetHashCode() ?? 0) ^ (_screen?.GetHashCode() ?? 0) * 31 ^ (_modal?.GetHashCode() ?? 0) * 17;
    }
}
