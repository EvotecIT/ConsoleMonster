using ConsoleMonster.Live;

namespace ConsoleMonster.Tests.Live {
    internal sealed class FakeLogTarget : FakeTarget, ILogTarget {
        internal FakeLogTarget(string name = "log") : base(name) { }
    }
}
