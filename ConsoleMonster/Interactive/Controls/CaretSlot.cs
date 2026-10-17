namespace ConsoleMonster.Interactive {
    /// <summary>Collects where the focused text box wants the caret during a render (absolute frame coordinates).</summary>
    internal sealed class CaretSlot {
        public Caret? Caret { get; private set; }

        public void Clear() => Caret = null;

        public void Set(int x, int y) => Caret = new Caret(x, y);
    }
}
