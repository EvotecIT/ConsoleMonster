"""Exercise startup OSC response, timeout, typed keys and exact termios restoration."""
import fcntl
import os
from pathlib import Path
import pty
import select
import signal
import struct
import sys
import termios
import time

host, runtime, root = sys.argv[1:]
for scenario in ("light", "dark", "timeout", "partial", "abandoned", "host"):
    receipt = Path(root) / scenario
    master, slave = pty.openpty()
    if sys.platform == "darwin":
        fcntl.ioctl(slave, termios.FIONREAD, struct.pack("I", 0))
    before = termios.tcgetattr(slave)
    pid = os.fork()
    if pid == 0:
        os.close(master)
        os.setsid()
        fcntl.ioctl(slave, termios.TIOCSCTTY, 0)
        for fd in (0, 1, 2):
            os.dup2(slave, fd)
        if slave > 2:
            os.close(slave)
        os.environ.update(TERM="xterm-256color", COLORFGBG="0;15")
        for name in ("CI", "GITHUB_ACTIONS", "TF_BUILD", "CONSOLEMONSTER_THEME"):
            os.environ.pop(name, None)
        os.execvp("dotnet", ["dotnet", "exec", "--runtimeconfig", runtime, host, "background", str(receipt), scenario])
    data = bytearray()
    replied = False
    done = False
    partial_at = None
    try:
        deadline = time.monotonic() + 15
        while time.monotonic() < deadline:
            if select.select([master], [], [], .01)[0]:
                chunk = os.read(master, 4096)
                data.extend(chunk)
            if scenario == "host" and not replied and b"HOST_READY" in data:
                os.write(master, b"abc\r")
                replied = True
            if scenario != "host" and not replied and b"\x1b]11;?\x07" in data:
                rgb = b"ffff/ffff/ffff" if scenario == "light" else b"0000/0000/0000"
                # Text arriving with the reply must survive the startup read.
                if scenario in ("partial", "abandoned"):
                    os.write(master, b"a\x1b]11;rgb:ffff/")
                    partial_at = time.monotonic() + (.2 if scenario == "partial" else .6)
                else:
                    os.write(master, b"a" + (b"\x1b]11;rgb:" + rgb + b"\x1b\\" if scenario != "timeout" else b"") + b"bc\r")
                replied = True
            if partial_at is not None and time.monotonic() >= partial_at:
                os.write(master, b"ffff/ffff\x07bc\r" if scenario == "partial" else b"bc\r")
                partial_at = None
            if receipt.with_suffix(".restored").exists():
                if sys.platform == "darwin":
                    fcntl.ioctl(slave, termios.FIONREAD, struct.pack("I", 0))
                # Console.ReadLine owns its own mode until process exit; leased input must restore while alive.
                if scenario != "host":
                    assert termios.tcgetattr(slave) == before, (scenario, "termios was not restored", before, termios.tcgetattr(slave))
                actual = receipt.with_suffix(".value").read_text()
                expected = "native|abc" if scenario == "host" else ("COLORFGBG|True" if scenario in ("timeout", "partial", "abandoned") else "OSC 11|" + str(scenario == "light")) + "|abc"
                assert data.count(b"\x1b]11;?\x07") == (0 if scenario == "host" else 1), (scenario, "query crossed the input ownership boundary")
                assert actual == expected, (scenario, actual, expected, bytes(data))
                receipt.with_suffix(".ack").write_text("yes")
                _, status = os.waitpid(pid, 0)
                assert os.waitstatus_to_exitcode(status) == 0
                done = True
                break
        assert done, (scenario, "deadline", bytes(data))
    finally:
        if not done:
            os.kill(pid, signal.SIGKILL)
            os.waitpid(pid, 0)
        os.close(master)
        os.close(slave)
print("OSC_STARTUP_OK")
