#!/usr/bin/env python3
"""Drive Start-ConsoleRecording through an owned native PTY, without desktop input."""
import errno
import fcntl
import json
import os
from pathlib import Path
import pty
import re
import select
import shutil
import signal
import struct
import sys
import termios
import time


def main():
    module, directory = Path(sys.argv[1]).resolve(), Path(sys.argv[2]).resolve()
    directory.mkdir()
    cast = directory / "session.cast"
    script = directory / "outer.ps1"
    # Never read the runner's bashrc: it can replace PS1 and add title writes.
    shell = directory / "recording-shell"
    shell.write_text("#!/bin/sh\nexport PS1='CM-SHELL> '\nexport BASH_SILENCE_DEPRECATION_WARNING=1\nexec /bin/bash --noprofile --norc -i\n")
    shell.chmod(0o700)
    quote = lambda value: "'" + str(value).replace("'", "''") + "'"
    script.write_text("\n".join([
        "$ErrorActionPreference = 'Stop'",
        "Import-Module " + quote(module),
        "$Host.UI.RawUI.WindowTitle = 'CM-parent'",
        "[Console]::WriteLine('CM-BASELINE')",
        "[Console]::ReadLine() > $null",
        "$before = & stty -g",
        "Start-ConsoleRecording -Shell " + quote(shell) + " -Path " + quote(cast) + " -Name PTY-check -TimeoutSeconds 30 | Out-Null",
        "$after = & stty -g",
        "@{before=$before; after=$after} | ConvertTo-Json | Set-Content " + quote(directory / "mode.json"),
        "[Console]::WriteLine('CM-RETURNED')",
        "while (-not [IO.File]::Exists(" + quote(directory / "finish") + ")) { Start-Sleep -Milliseconds 20 }",
    ]))
    master, slave = pty.openpty()
    fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 30, 100, 0, 0))
    pid = os.fork()
    if pid == 0:
        os.setsid()
        fcntl.ioctl(slave, termios.TIOCSCTTY, 0)
        for fd in range(3):
            os.dup2(slave, fd)
        os.close(master)
        os.close(slave)
        os.environ.update(TERM="xterm-256color", LANG="en_US.UTF-8", LC_ALL="en_US.UTF-8", PS1="CM-SHELL> ")
        os.execv(shutil.which("pwsh"), ["pwsh", "-NoLogo", "-NoProfile", "-File", str(script)])
    data = bytearray()
    cursor = 0
    status = None
    started = time.monotonic()

    def send(value):
        payload = value.encode() if isinstance(value, str) else value
        os.write(master, payload)

    def until(token, timeout=8):
        nonlocal cursor
        target = token.encode()
        end = time.monotonic() + timeout
        while target not in data[cursor:]:
            if time.monotonic() > end:
                raise AssertionError("Missing " + token + ": " + data[cursor:].decode(errors="replace"))
            if select.select([master], [], [], 0.05)[0]:
                try:
                    chunk = os.read(master, 65536)
                except OSError as error:
                    if error.errno != errno.EIO:
                        raise
                    chunk = b""
                if not chunk:
                    raise AssertionError("Terminal closed before " + token)
                data.extend(chunk)
                # PowerShell may query the cursor position during native console initialization.
                for _ in re.finditer(rb"\x1b\[6n", chunk):
                    send(b"\x1b[1;1R")
        cursor = data.index(target, cursor) + len(target)

    def command(value):
        send(value + "\n")

    try:
        until("CM-BASELINE")
        send("\n")
        # Wait for the recording shell, then require a marker assembled only during execution.
        until("CM-SHELL> ")
        command("exec 9<>/dev/tty; test $(ps -o tpgid= -p $$) -gt 0 && printf 'CM-%s\\n' TTY_OK")
        until("CM-TTY_OK")
        command("printf 'CM-%s\\n' SLEEP_READY; /bin/sleep 20")
        until("CM-SLEEP_READY")
        time.sleep(0.2)
        send(b"\x03")
        command("printf 'CM-%s\\n' INTERRUPTED")
        until("CM-INTERRUPTED", 5)
        command("trap 'printf CM-WINCH; stty size' WINCH; printf 'CM-%s\\n' RESIZE_READY")
        until("CM-RESIZE_READY")
        fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 40, 132, 0, 0))
        send("\n")
        until("CM-WINCH")
        until("40 132")
        command("pwsh -NoLogo -NoProfile -Command '[Console]::WriteLine((\"CM-WIDTH-\" + [Console]::WindowWidth)); [Console]::ReadLine() > $null; [Console]::WriteLine((\"CM-WIDTH-\" + [Console]::WindowWidth))'")
        until("CM-WIDTH-132")
        fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 42, 140, 0, 0))
        time.sleep(0.2)
        send("\n")
        until("CM-WIDTH-140")
        command("stty -echo; printf 'CM-%s\\n' SECRET_READY; IFS= read -r value </dev/tty; stty echo; test \"$value\" = synthetic-secret && printf 'CM-%s\\n' SECRET_ACCEPTED")
        until("CM-SECRET_READY")
        send("synthetic-secret\n")
        until("CM-SECRET_ACCEPTED")
        # The value occurs in command echo above; check only the actual no-echo input interval in the cast.
        command("exit")
        until("CM-RETURNED")
        mode = json.loads((directory / "mode.json").read_text(encoding="utf-8-sig"))
        assert mode["before"] == mode["after"], "Parent terminal mode was not restored"
        events = [json.loads(line) for line in cast.read_text().splitlines()]
        assert any(isinstance(event, list) and event[1:3] == ["r", "132x40"] for event in events), "Missing resize event"
        # pwsh itself writes empty titles during startup. Count only this command's set/restore.
        session_data = data[data.index(b"CM-BASELINE"):]
        titles = re.findall(rb"\x1b\]0;[^\x07]*\x07", session_data)
        rec_titles = [title for title in titles if b"REC PTY-check" in title]
        assert len(rec_titles) == 1, "Recording title written " + str(len(rec_titles)) + " times"
        assert titles[-1] == b"\x1b]0;CM-parent\x07", "Parent title was not restored"
        visible = "".join(event[2] for event in events if isinstance(event, list) and event[1] == "o")
        # Events can split anywhere. Inspect the reconstructed output between executed markers,
        # after the command echo and before echo was re-enabled.
        secret_start = visible.index("CM-SECRET_READY") + len("CM-SECRET_READY")
        secret_end = visible.index("CM-SECRET_ACCEPTED", secret_start)
        assert "synthetic-secret" not in visible[secret_start:secret_end], "No-echo input was recorded"
        (directory / "finish").touch()
        end = time.monotonic() + 5
        while time.monotonic() < end:
            waited, status_value = os.waitpid(pid, os.WNOHANG)
            if waited:
                status = status_value
                break
            time.sleep(0.02)
        assert status is not None and os.waitstatus_to_exitcode(status) == 0, "Outer PowerShell failed"
        print(json.dumps({"controlling_tty": True, "ctrl_c": True, "sigwinch": True,
                          "console_width": [132, 140], "no_echo": True, "restored": True,
                          "recording_title_writes": len(rec_titles), "parent_title_restored": True,
                          "seconds": round(time.monotonic() - started, 2)}))
    finally:
        if status is None:
            os.kill(pid, signal.SIGKILL)
            # Darwin can keep the exiting child's terminal output open until the master hangs up.
            os.close(master)
            master = None
            os.waitpid(pid, 0)
        if master is not None:
            os.close(master)
        os.close(slave)
        (directory / "terminal.bin").write_bytes(data)


if __name__ == "__main__":
    main()
