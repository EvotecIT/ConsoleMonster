using System;
using System.Globalization;

namespace ConsoleMonster {
    /// <summary>Bounds automatic annotations from a single runner or error-output pipeline. Explicit annotations stay unrestricted.</summary>
    internal sealed class AnnotationBatch : IDisposable {
        private readonly Action<string> _write;
        private int _count;
        private bool _disposed;
        internal AnnotationBatch(Action<string> write) { _write = write; }

        internal void Error(string message, string? title, string? file, int line, int column = 0) {
            if (ContinuousIntegration.Provider == ContinuousIntegrationProvider.None || _disposed) { return; }
            if (++_count <= 10) { ContinuousIntegration.Annotate(AnnotationLevel.Error, message, title, file, line > 0 ? (int?)line : null, column > 0 ? (int?)column : null, _write); }
        }

        public void Dispose() {
            if (_disposed) { return; }
            _disposed = true;
            if (_count > 10) { ContinuousIntegration.Write((_count - 10).ToString(CultureInfo.InvariantCulture) + " additional errors omitted from CI annotations.", _write); }
        }
    }
}
