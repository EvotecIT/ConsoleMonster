"""Exercise the real app, its child PTY, input relay, resize and terminal restoration."""
import errno
import json
import fcntl
import os
import pty
import select
import shutil
import signal
import struct
import sys
import termios
import time


def run(dotnet, host, result, runtime=None):
    dotnet = shutil.which(dotnet) or dotnet
    master, slave = pty.openpty()
    before = termios.tcgetattr(slave)
    fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 24, 80, 0, 0))
    pid = os.fork()
    if pid == 0:
        os.close(master)
        os.setsid()
        fcntl.ioctl(slave, termios.TIOCSCTTY, 0)
        for descriptor in (0, 1, 2):
            os.dup2(slave, descriptor)
        if slave > 2:
            os.close(slave)
        env = dict(os.environ, TERM="xterm-256color", LANG="en_US.UTF-8")
        for key in ("CI", "GITHUB_ACTIONS", "CONSOLEMONSTER_INPUT", "CONSOLEMONSTER_SNAPSHOT"):
            env.pop(key, None)
        command = [dotnet, "exec", "--runtimeconfig", runtime, host] if runtime else [dotnet, host]
        os.execve(dotnet, command + ["handoff-probe", result], env)
    output = bytearray()
    status = None

    def wait_for(marker, seconds=25):
        deadline = time.monotonic() + seconds
        while marker not in output:
            if time.monotonic() >= deadline:
                raise AssertionError("missing %r in %r" % (marker, bytes(output[-2500:])))
            if select.select([master], [], [], 0.1)[0]:
                try:
                    chunk = os.read(master, 65536)
                except OSError as error:
                    if error.errno != errno.EIO:
                        raise
                    chunk = b""
                if not chunk:
                    raise AssertionError("app closed before %r: %r" % (marker, bytes(output[-2500:])))
                output.extend(chunk)

    try:
        wait_for(b"HANDOFF_BASELINE")
        before = termios.tcgetattr(slave)
        with open(result + ".start", "w", encoding="utf-8"):
            pass
        wait_for(b"TTY handoff")
        # Focus the second original object before handing off the screen.
        os.write(master, b"\x1b[B\x1b[15~")
        wait_for(b"CHILD_READY")
        fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 28, 100, 0, 0))
        os.kill(pid, signal.SIGWINCH)
        os.write(master, b"handoff-input\n")
        wait_for(b"RESUMED")
        with open(result, encoding="utf-8") as stream:
            proof = stream.read()
        assert "exit=7" in proof, proof
        assert "focus=second" in proof, proof
        assert "CHILD_ANSWER:handoff-input" in proof, proof
        assert "\x1b[32m" in proof, repr(proof)
        os.write(master, b"\x0f")
        wait_for(b"Last output")
        os.write(master, b" ")
        # Wait for app screen restoration before asking it to quit.
        previous = len(output)
        deadline = time.monotonic() + 5
        while len(output) == previous and time.monotonic() < deadline:
            if select.select([master], [], [], 0.1)[0]:
                output.extend(os.read(master, 65536))
        os.write(master, b"q")
        wait_for(b"HANDOFF_RESTORED")
        after = termios.tcgetattr(slave)
        with open(result + ".modes", "w", encoding="utf-8") as stream:
            json.dump({"before": repr(before), "after": repr(after)}, stream)
        assert after == before, "terminal attributes not restored"
        with open(result + ".finish", "w", encoding="utf-8"):
            pass
        deadline = time.monotonic() + 5
        while time.monotonic() < deadline:
            done, child_status = os.waitpid(pid, os.WNOHANG)
            if done:
                status = child_status
                break
            time.sleep(0.02)
        else:
            raise AssertionError("app did not exit")
        assert os.waitstatus_to_exitcode(status) == 0, bytes(output[-2500:])
        assert b"\x1b[?1049l" in output and b"\x1b[?1049h" in output, "alternate screen was not handed off"
        print("PTY_HANDOFF_OK: tty, colours, input, resize, exit7, focus, output view, modes restored")
    finally:
        if status is None:
            try:
                os.kill(pid, signal.SIGKILL)
                os.waitpid(pid, 0)
            except ProcessLookupError:
                pass
        os.close(master)
        os.close(slave)


if __name__ == "__main__":
    run(*sys.argv[1:])
