using System;

namespace ConsoleMonster.Sessions {
    /// <summary>A process with terminal handles. Only the creating recorder owns its lifetime.</summary>
    internal interface IPseudoTerminal : IDisposable {
        int Read(byte[] bytes);
        void Write(byte[] bytes);
        void Resize(int width, int height);
        bool HasExited { get; }
        int ExitCode { get; }
        void Stop();
    }
}
