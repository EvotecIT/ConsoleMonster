# Windows terminal integration only: read the owned child process's actual console cells after a direct-host command.
if (-not ('ConsoleMonsterTestConsoleFrame' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
public static class ConsoleMonsterTestConsoleFrame {
    [StructLayout(LayoutKind.Sequential)] private struct Coord { public short X; public short Y; }
    [DllImport("kernel32.dll")] private static extern IntPtr GetStdHandle(int id);
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    private static extern bool ReadConsoleOutputCharacterW(IntPtr handle, [Out] char[] buffer, uint length, Coord start, out uint read);
    public static string LastFrame(int lines, int width) {
        int start = Math.Max(0, Console.CursorTop - lines - 1);
        StringBuilder frame = new StringBuilder();
        for (int y = start; y < start + lines; y++) {
            char[] row = new char[width];
            uint read;
            if (!ReadConsoleOutputCharacterW(GetStdHandle(-11), row, (uint)width, new Coord { X=0, Y=(short)y }, out read)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            frame.Append(new string(row, 0, (int)read).TrimEnd()).Append('\n');
        }
        return frame.ToString();
    }
}
'@
}
