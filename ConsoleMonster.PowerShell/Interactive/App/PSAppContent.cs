using System;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Marks the thread while Show-ConsoleApp collects its content, so commands that build a widget there can build the
    /// app's form of it: New-ConsoleLogRegion returns a retained, searchable LogViewport around the same region.
    /// </summary>
    internal static class PSAppContent {
        [ThreadStatic] private static int _depth;

        /// <summary>Whether the current thread is collecting app content.</summary>
        internal static bool Active => _depth > 0;

        /// <summary>Enters the scope until the returned value is disposed.</summary>
        internal static IDisposable Enter() { _depth++; return new Scope(); }

        private sealed class Scope : IDisposable {
            private bool _disposed;
            public void Dispose() { if (!_disposed) { _disposed = true; _depth--; } }
        }
    }
}