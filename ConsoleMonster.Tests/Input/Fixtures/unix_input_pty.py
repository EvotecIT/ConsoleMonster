#!/usr/bin/env python3
"""One owned Unix PTY, real ConsoleMonster console input, JSON evidence."""
import argparse
import errno
import fcntl
import hashlib
import json
import os
from pathlib import Path
import pty
import selectors
import shutil
import signal
import struct
import sys
import termios
import time

CASES = {
    "unicode": "aŻ中".encode(),
    "arrows": b"\x1b[A\x1b[B\x1b[C\x1b[D",
    "tab": b"\t", "shift_tab": b"\x1b[Z", "escape": b"\x1b",
    "sgr_origin": b"\x1b[<0;1;1M\x1b[<0;1;1mz",
    "sgr_all_modifiers": b"\x1b[<28;120;40M\x1b[<28;120;40mz",
    "sgr_middle_alt": b"\x1b[<9;43;17M\x1b[<9;43;17mz",
    "sgr_right_control": b"\x1b[<18;7;9M\x1b[<18;7;9mz",
    "idle_mouse": b"\x1b[<0;12;",
    "idle_overlong": b"\x1b[" + b"1" * 80,
    "idle_paste": b"\x1b[200~p",
}
TAILS = {"idle_mouse": b"6Mz", "idle_overlong": b"Mz", "idle_paste": b"\x1b[201~z"}


def attrs(fd):
    if sys.platform == "darwin":
        # Darwin sets PENDIN on a return to canonical mode. Observing the pending input count
        # lets the kernel process that state; compare every attribute afterwards, without masking flags.
        fcntl.ioctl(fd, termios.FIONREAD, struct.pack("I", 0))
    value = termios.tcgetattr(fd)
    return [*value[:6], [x if isinstance(x, int) else x[0] for x in value[6]]]


def record_exit_state(receipt, fd, before, exit_code):
    """Inspect a reaped child's PTY; only Darwin may revoke it after proven live restoration."""
    try:
        after = attrs(fd)
    except (OSError, termios.error) as ex:
        if (sys.platform != "darwin" or ex.args[0] != errno.ENOTTY or exit_code != 0
                or receipt.get("restored_while_child_alive") is not True
                or receipt.get("lease_released") is not True
                or receipt.get("finished_observed") is not True):
            raise
        receipt["pty_revoked_after_exit"] = True
        receipt["termios_after_exit"] = None
        return
    receipt["termios_after_exit"] = after
    receipt["restored_after_exit"] = after == before
    if after != before:
        receipt["errors"].append("Terminal state changed after child exit.")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("dll", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--runtimeconfig", type=Path, required=True)
    parser.add_argument("--watchdog", type=float, default=90)
    options = parser.parse_args()
    options.output.mkdir(exist_ok=False)
    master, slave = pty.openpty()
    fcntl.ioctl(slave, termios.TIOCSWINSZ, struct.pack("HHHH", 40, 120, 0, 0))
    before_exec = attrs(slave)
    before = None
    report_r, report_w = os.pipe()
    control_r, control_w = os.pipe()
    os.set_inheritable(control_r, True)
    command = [shutil.which("dotnet"), "exec", "--runtimeconfig", str(options.runtimeconfig),
               str(options.dll.resolve()), "unix-input-probe", str(control_r)]
    receipt = {"command": command, "harness_pid": os.getpid(), "pty": os.ttyname(slave),
               "term": "xterm-256color", "watchdog_seconds": options.watchdog,
               "termios_before_exec": before_exec, "sent": [], "passed": [], "errors": [], "phases": [],
               "assembly_sha256": hashlib.sha256(options.dll.read_bytes()).hexdigest(),
               "core_assembly_sha256": hashlib.sha256(
                   (options.dll.parent / "ConsoleMonster.dll").read_bytes()).hexdigest()}
    started = time.monotonic()
    pid = os.fork()
    if pid == 0:
        try:
            os.setsid()
            fcntl.ioctl(slave, termios.TIOCSCTTY, 0)
            os.dup2(slave, 0)
            os.dup2(slave, 1)
            os.dup2(report_w, 2)
            for fd in (master, slave, report_r, report_w, control_w):
                if fd > 2:
                    os.close(fd)
            os.environ.update(TERM="xterm-256color", LANG="C.UTF-8", LC_ALL="C.UTF-8")
            os.execv(command[0], command)
        except BaseException:
            os._exit(127)
    receipt["child_pid"] = pid
    os.close(report_w)
    os.close(control_r)
    selector = selectors.DefaultSelector()
    selector.register(master, selectors.EVENT_READ, "terminal")
    selector.register(report_r, selectors.EVENT_READ, "report")
    raw_terminal = bytearray()
    report_bytes = bytearray()
    pending = bytearray()
    status = None
    disposed = False
    finished = False
    signals_sent = []
    try:
        while status is None or selector.get_map():
            if time.monotonic() - started > options.watchdog:
                raise TimeoutError("Generous whole-run watchdog expired; no latency assertion is made.")
            for key, _ in selector.select(0.1):
                try:
                    data = os.read(key.fd, 65536)
                except OSError as ex:
                    if ex.errno != errno.EIO:
                        raise
                    data = b""
                if not data:
                    selector.unregister(key.fd)
                    continue
                if key.data == "terminal":
                    raw_terminal.extend(data)
                    continue
                report_bytes.extend(data)
                (options.output / "probe.jsonl").write_bytes(report_bytes)
                pending.extend(data)
                while b"\n" in pending:
                    line, _, pending = pending.partition(b"\n")
                    event = json.loads(line)
                    phase = event["phase"]
                    receipt["phases"].append(phase)
                    if phase == "opened":
                        # The .NET runtime has initialized the console, but cannot acquire its lease until this ACK.
                        if before is not None:
                            raise RuntimeError("Duplicate prelease handshake")
                        before = attrs(slave)
                        receipt["termios_before"] = before
                        receipt["prelease_baseline_acknowledged"] = True
                        os.write(control_w, b"\x03")
                    elif phase == "ready":
                        name = event["name"]
                        payload = CASES[name]
                        written = os.write(master, payload)
                        if written != len(payload):
                            raise RuntimeError("Partial PTY write")
                        receipt["sent"].append({"name": name, "hex": payload.hex()})
                        if name.startswith("idle_"):
                            os.write(control_w, b"\x02")
                    elif phase == "pass":
                        receipt["passed"].append(event["name"])
                    elif phase == "ready_tail":
                        os.write(master, TAILS[event["name"]])
                    elif phase == "menu_ready":
                        os.write(master, b"\r")
                    elif phase == "live_ready":
                        # ShowKeys is set before the first read activates raw mode. Wait for the
                        # actual mode, then prove SIGINT is preserved during raw key polling.
                        end = time.monotonic() + 5
                        while attrs(slave)[3] & termios.ICANON:
                            if time.monotonic() > end:
                                raise TimeoutError("Live input did not enter raw mode")
                            time.sleep(0.01)
                        receipt["live_signals_enabled"] = bool(attrs(slave)[3] & termios.ISIG)
                        os.write(master, b"\x03")
                    elif phase == "live_pass":
                        receipt["live_keyboard_cancelled_work"] = True
                    elif phase == "relay_invalid_pass":
                        receipt["relay_invalid_descriptor_error"] = True
                    elif phase == "signal_ready":
                        suspended = attrs(slave)
                        receipt["termios_while_suspended"] = suspended
                        receipt["restored_while_suspended"] = suspended == before
                        receipt["signals_enabled_during_work"] = bool(suspended[3] & termios.ISIG)
                        if suspended != before:
                            receipt["errors"].append("Suspending input did not restore the complete prelease terminal state.")
                        os.write(master, b"\x03")
                    elif phase == "menu_resumed":
                        receipt["keyboard_cancelled_work"] = event["cancelled"]
                        receipt["raw_mode_resumed"] = not bool(attrs(slave)[3] & termios.ICANON)
                        # The menu's global Ctrl+Q binding proves that raw-key navigation resumed after live work.
                        os.write(master, b"\x11")
                    elif phase == "menu_pass":
                        receipt["menu_navigation_after_work"] = True
                    elif phase == "failure":
                        receipt["errors"].append(event["error"])
                    elif phase == "disposed":
                        disposed = True
                        after_dispose = attrs(slave)
                        receipt["termios_after_dispose"] = after_dispose
                        receipt["restored_while_child_alive"] = after_dispose == before
                        receipt["lease_released"] = event["acquired"] is False
                        receipt["terminal_flags"] = {
                            name: {"before": bool(before[3] & getattr(termios, name)),
                                   "after_dispose": bool(after_dispose[3] & getattr(termios, name))}
                            for name in ("ICANON", "ECHO", "ISIG", "IEXTEN")}
                        if not receipt["restored_while_child_alive"]:
                            receipt["errors"].append("Terminal state was not restored by lease disposal while child remained alive.")
                        os.write(control_w, b"\x01")
                    elif phase == "finished":
                        finished = True
            if status is None:
                waited, child_status = os.waitpid(pid, os.WNOHANG)
                if waited:
                    status = child_status
                    # The parent keeps the slave open for restoration evidence;
                    # unregister master once the child has exited to avoid false EOF waits.
                    if master in selector.get_map():
                        selector.unregister(master)
        receipt["finished_observed"] = finished
        record_exit_state(receipt, slave, before, os.waitstatus_to_exitcode(status))
    except BaseException as ex:
        receipt["errors"].append(type(ex).__name__ + ": " + str(ex))
    finally:
        # Only the exact child created above, with its unreaped PID, is signalled.
        if status is None:
            waited, child_status = os.waitpid(pid, os.WNOHANG)
            if waited:
                status = child_status
            else:
                os.kill(pid, signal.SIGTERM)
                signals_sent.append("SIGTERM")
                end = time.monotonic() + 3
                while time.monotonic() < end:
                    waited, child_status = os.waitpid(pid, os.WNOHANG)
                    if waited:
                        status = child_status
                        break
                    time.sleep(0.05)
                if status is None:
                    os.kill(pid, signal.SIGKILL)
                    signals_sent.append("SIGKILL")
                    # Darwin can wait for terminal output to drain while a process exits. On
                    # failure the reader loop has stopped, so hang up the master before reaping.
                    if master in selector.get_map():
                        selector.unregister(master)
                    os.close(master)
                    master = None
                    _, status = os.waitpid(pid, 0)
        receipt.update(exit_code=os.waitstatus_to_exitcode(status), disposed_observed=disposed,
                       finished_observed=finished, elapsed_seconds=round(time.monotonic() - started, 3),
                       cleanup={"owned_child_pid": pid, "reaped": True, "signals": signals_sent})
        # Child reaped before closing the owned PTY and IPC descriptors.
        selector.close()
        for fd in (master, slave, report_r, control_w):
            if fd is not None:
                os.close(fd)
        receipt["cleanup"]["pty_and_pipes_closed"] = True
        receipt["success"] = (receipt["exit_code"] == 0 and not receipt["errors"]
                              and receipt["passed"] == list(CASES)
                              and receipt.get("prelease_baseline_acknowledged") is True
                              and receipt.get("restored_while_suspended") is True
                              and receipt.get("restored_while_child_alive") is True
                              and receipt.get("lease_released") is True and finished)
        receipt["success"] = (receipt["success"]
                              and receipt.get("signals_enabled_during_work") is True
                              and receipt.get("keyboard_cancelled_work") is True
                              and receipt.get("raw_mode_resumed") is True
                              and receipt.get("menu_navigation_after_work") is True
                              and receipt.get("live_signals_enabled") is True
                              and receipt.get("live_keyboard_cancelled_work") is True
                              and receipt.get("relay_invalid_descriptor_error") is True
                              and (receipt.get("restored_after_exit") is True
                                   or receipt.get("pty_revoked_after_exit") is True))
        (options.output / "probe.jsonl").write_bytes(report_bytes)
        (options.output / "terminal.bin").write_bytes(raw_terminal)
        (options.output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, indent=2))
    return 0 if receipt["success"] else 1


if __name__ == "__main__":
    sys.exit(main())
