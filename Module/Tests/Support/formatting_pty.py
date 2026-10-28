"""Run PowerShell on a real Unix terminal; --submissions uses automatic host output."""
import errno
import fcntl
import os
import pty
import select
import signal
import struct
import sys
import termios
import time

runtime, script, output = sys.argv[1:4]
timeout_seconds = float(sys.argv[4]) if len(sys.argv) > 4 else 30
width = int(sys.argv[5]) if len(sys.argv) > 5 else 120
submissions = "--submissions" in sys.argv[6:]
input_file = os.open(script, os.O_RDONLY) if submissions else None
pid, master = pty.fork()
if pid == 0:
    os.environ.update(TERM="xterm-256color", COLORTERM="truecolor", COLUMNS=str(width), LINES="40")
    for name in ("CI", "TF_BUILD", "GITHUB_ACTIONS", "GITLAB_CI", "CODEX_CI", "NO_COLOR", "CONSOLEMONSTER_MODE"):
        os.environ.pop(name, None)
    if submissions:
        os.dup2(input_file, 0)
        os.close(input_file)
    os.execv(runtime, [runtime, "-NoLogo", "-NoProfile", "-Command", "-"] if submissions
             else [runtime, "-NoLogo", "-NoProfile", "-File", script])
fcntl.ioctl(master, termios.TIOCSWINSZ, struct.pack("HHHH", 40, width, 0, 0))
if submissions:
    # Separate host submissions without PSReadLine or command echo; stdout/stderr stay on the tty.
    os.close(input_file)
data = bytearray()
status = None
try:
    deadline = time.monotonic() + timeout_seconds
    while time.monotonic() < deadline:
        if select.select([master], [], [], 0.05)[0]:
            try:
                chunk = os.read(master, 65536)
            except OSError as error:
                if error.errno == errno.EIO:
                    break
                raise
            if not chunk:
                break
            data.extend(chunk)
        done, observed = os.waitpid(pid, os.WNOHANG)
        if done:
            status = observed
            # Drain output written just before exit.
            while select.select([master], [], [], 0)[0]:
                try:
                    chunk = os.read(master, 65536)
                except OSError:
                    break
                if not chunk:
                    break
                data.extend(chunk)
            break
    else:
        raise TimeoutError(f"PowerShell formatting did not complete within {timeout_seconds:g} seconds")
finally:
    if status is None:
        done, observed = os.waitpid(pid, os.WNOHANG)
        if not done:
            os.kill(pid, signal.SIGKILL)
            _, observed = os.waitpid(pid, 0)
        status = observed
    os.close(master)
    with open(output, "wb") as capture:
        capture.write(data)
sys.exit(os.waitstatus_to_exitcode(status))
