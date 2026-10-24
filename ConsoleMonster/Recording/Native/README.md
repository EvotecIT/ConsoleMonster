# macOS terminal launcher

macOS does not acquire a controlling terminal when a session leader opens a PTY
slave. The launcher calls `setsid()` and `ioctl(TIOCSCTTY)` before executing the
requested child. `posix_spawn` supplies its standard descriptors and working
directory. No managed code runs between fork and exec.

Descriptor 3 is a startup-status pipe. The launcher writes `errno` on failure;
successful exec closes it. The parent can report a missing executable or failed
terminal setup synchronously, then reap the failed child.

`macos-terminal-exec` is a universal arm64/x86_64 executable, embedded in the
assembly and extracted into a private temporary directory for each launch. It
uses only macOS system libraries. The parent removes it after startup. Ordinary
.NET builds, NuGet consumers and PowerShell imports need no compiler, helper
installation or additional package. Linux continues to use `posix_spawn` directly.

To regenerate it on macOS with Apple's command-line developer tools:

```sh
sh ConsoleMonster/Recording/Native/build-macos-terminal-exec.sh
```

Commit the C source and regenerated executable together. The linker supplies
the local ad-hoc signature needed for arm64 execution; no signing identity or
private key is used.
