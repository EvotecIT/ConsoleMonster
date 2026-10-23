using System;
using System.Runtime.InteropServices;

// Used only inside a fresh, isolated PowerShell console; never changes the test runner's native mode.
public static class ConsoleMonsterTailModeConsole {
    [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int id);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetConsoleMode(IntPtr handle, out uint mode);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleMode(IntPtr handle, uint mode);

    /// <summary>Reads the complete stdout console mode, failing if the fixture is accidentally redirected.</summary>
    public static uint Read() {
        uint mode;
        if (!GetConsoleMode(GetStdHandle(-11), out mode)) { throw new InvalidOperationException("A native stdout console is required."); }
        return mode;
    }

    /// <summary>Sets a known entry mode in the isolated child.</summary>
    public static void Set(uint mode) {
        if (!SetConsoleMode(GetStdHandle(-11), mode)) { throw new InvalidOperationException("Cannot set the native stdout mode."); }
    }
}
