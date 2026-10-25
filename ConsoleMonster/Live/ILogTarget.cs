namespace ConsoleMonster.Live {
    /// <summary>
    /// Marks a live target that can receive captured output (Console.Out/Error, PowerShell streams).
    /// Captured lines arrive as <see cref="LiveEvent"/>s with operation <c>write</c>, the line in
    /// <see cref="LiveEvent.Text"/> and the level (out, err, info, warn, verbose, debug, host) in <see cref="LiveEvent.Status"/>.
    /// </summary>
    public interface ILogTarget : ILiveTarget {
    }
}
