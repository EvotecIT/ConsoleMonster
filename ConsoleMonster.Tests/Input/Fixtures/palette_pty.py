"""Exercise the real OSC 4 exchange, all-or-nothing timeout, input preservation and exported RGB."""
import fcntl
import json
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
for scenario in ("complete", "incomplete", "timeout", "late", "light-timeout"):
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
        os.environ.update(TERM="xterm-256color")
        os.environ.pop("CONSOLEMONSTER_THEME", None)
        for name in ("CI", "GITHUB_ACTIONS", "TF_BUILD"):
            os.environ.pop(name, None)
        os.execvp("dotnet", ["dotnet", "exec", "--runtimeconfig", runtime, host, "palette", str(receipt)])
    data = bytearray()
    background = False
    palette = False
    done = False
    query_at = None
    late_at = None
    try:
        deadline = time.monotonic() + 15
        while time.monotonic() < deadline:
            if select.select([master], [], [], .01)[0]:
                data.extend(os.read(master, 8192))
            if not background and b"\x1b]11;?\x07" in data:
                os.write(master, b"\x1b]11;rgb:" + (b"ffff/ffff/ffff" if scenario == "light-timeout" else b"1111/2222/3333") + b"\x07")
                background = True
            if not palette and b"\x1b]4;15;?\x07" in data:
                query_at = time.monotonic()
                response = b"a"
                if scenario not in ("timeout", "light-timeout", "late"):
                    for index in range(16 if scenario == "complete" else 15):
                        # Each channel uses 16-bit precision; entries have distinct expected values.
                        channel = f"{index * 0x1111:04x}".encode()
                        response += b"\x1b]4;" + str(index).encode() + b";rgb:" + channel + b"/3333/7777\x1b\\"
                    response += b"\x1b]10;rgb:eeee/eeee/eeee\x07\x1b]11;rgb:1111/2222/3333\x07"
                if scenario == "late":
                    os.write(master, b"a\x1b]4;2;rgb:1234/")
                    late_at = time.monotonic() + .15
                else:
                    os.write(master, response + b"bc\r")
                palette = True
            if late_at is not None and time.monotonic() >= late_at:
                os.write(master, b"5678/abcd\x07\x1b]10;rgb:eeee/eeee/eeee\x07bc\r")
                late_at = None
            if receipt.with_suffix(".restored").exists():
                if sys.platform == "darwin":
                    fcntl.ioctl(slave, termios.FIONREAD, struct.pack("I", 0))
                assert termios.tcgetattr(slave) == before, (scenario, before, termios.tcgetattr(slave))
                value = receipt.with_suffix(".value").read_text()
                assert value.endswith("|abc"), (scenario, value)
                assert data.count(b"\x1b]4;0;?\x07") == 1
                if scenario == "complete":
                    assert value.startswith("#eeeeee|#112233|#003377,#113377,#223377,"), value
                    assert "#223377" in receipt.with_suffix(".svg").read_text()
                    assert "#223377" in receipt.with_suffix(".recording.svg").read_text()
                    assert json.loads(receipt.with_suffix(".cast").read_text().splitlines()[0])["theme"]["palette"].split(":")[2] == "#223377"
                    assert "Unknown" not in receipt.with_suffix(".check").read_text()
                    assert "OSC 4/10/11" in receipt.with_suffix(".decision").read_text()
                    document = json.loads(receipt.with_suffix(".theme.json").read_text())
                    assert document["BaseScheme"]["Scheme"]["Palette"][2] == "#223377"
                else:
                    assert value == "unknown|abc", value
                    assert "fallback" in receipt.with_suffix(".theme.json").read_text()
                    assert "exports use the background variant defaults" in receipt.with_suffix(".decision").read_text()
                    if scenario == "light-timeout":
                        document = json.loads(receipt.with_suffix(".theme.json").read_text())
                        assert document["BaseScheme"]["Scheme"]["Background"] == "#ffffff"
                assert query_at is not None and time.monotonic() - query_at < 3, "exchange did not time out"
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
print("OSC_PALETTE_OK")
