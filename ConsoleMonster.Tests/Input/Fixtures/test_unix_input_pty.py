"""Contracts for native PTY observations, including Darwin-only revoked terminals."""
import errno
import termios
import unittest
from unittest.mock import patch

import unix_input_pty as harness


class TerminalObservationTests(unittest.TestCase):
    def test_darwin_observes_pending_input_before_reading_all_attributes(self):
        state = [1, 2, 3, 1483 | 0x20000000, 4, 5, [b"x"]]
        calls = []

        def observe(fd, request, buffer):
            self.assertEqual((fd, request), (7, termios.FIONREAD))
            calls.append("pending")
            state[3] &= ~0x20000000

        def attributes(fd):
            calls.append("attributes")
            return state

        with patch.object(harness.sys, "platform", "darwin"), patch.object(harness.fcntl, "ioctl", side_effect=observe), patch.object(harness.termios, "tcgetattr", side_effect=attributes):
            self.assertEqual(harness.attrs(7), [1, 2, 3, 1483, 4, 5, [120]])
        self.assertEqual(calls, ["pending", "attributes"])

    def test_linux_reads_full_attributes_without_darwin_ioctl(self):
        state = [1, 2, 3, 1483, 4, 5, [b"x"]]
        with patch.object(harness.sys, "platform", "linux"), patch.object(harness.fcntl, "ioctl") as observe, patch.object(harness.termios, "tcgetattr", return_value=state):
            self.assertEqual(harness.attrs(7), [1, 2, 3, 1483, 4, 5, [120]])
            observe.assert_not_called()

    def test_disposed_attribute_leak_remains_visible(self):
        baseline = [1, 2, 3, 1483, 4, 5, [120]]
        changed = [1, 2, 3, 1483 ^ termios.ECHO, 4, 5, [120]]
        receipt = {"errors": []}
        with patch.object(harness, "attrs", return_value=changed):
            harness.record_exit_state(receipt, 7, baseline, 0)
        self.assertFalse(receipt["restored_after_exit"])
        self.assertEqual(receipt["termios_after_exit"], changed)
        self.assertTrue(receipt["errors"])

    def test_darwin_revocation_requires_successful_exit_and_live_restoration(self):
        receipt = {"errors": [], "restored_while_child_alive": True, "lease_released": True, "finished_observed": True}
        with patch.object(harness.sys, "platform", "darwin"), patch.object(harness, "attrs", side_effect=termios.error(errno.ENOTTY, "revoked")):
            harness.record_exit_state(receipt, 7, [], 0)
        self.assertTrue(receipt["pty_revoked_after_exit"])
        self.assertIsNone(receipt["termios_after_exit"])
        self.assertNotIn("restored_after_exit", receipt)
        self.assertEqual(receipt["errors"], [])

    def test_revocation_never_hides_missing_or_failed_lifetime_proof(self):
        complete = {"restored_while_child_alive": True, "lease_released": True, "finished_observed": True}
        cases = [("linux", errno.ENOTTY, 0, complete), ("darwin", errno.EIO, 0, complete),
                 ("darwin", errno.ENOTTY, 1, complete), ("darwin", errno.ENOTTY, None, complete)]
        for field in complete:
            cases.append(("darwin", errno.ENOTTY, 0, {**complete, field: False}))
        for platform, error, exit_code, proof in cases:
            with self.subTest(platform=platform, error=error, exit_code=exit_code, proof=proof):
                with patch.object(harness.sys, "platform", platform), patch.object(harness, "attrs", side_effect=termios.error(error, "terminal")):
                    with self.assertRaises(termios.error):
                        harness.record_exit_state(dict(proof), 7, [], exit_code)

    def test_successful_exit_keeps_full_snapshot_comparison(self):
        baseline = [1, 2, 3, 1483, 4, 5, [120]]
        receipt = {"errors": []}
        with patch.object(harness, "attrs", return_value=baseline):
            harness.record_exit_state(receipt, 7, baseline, 0)
        self.assertTrue(receipt["restored_after_exit"])
        self.assertEqual(receipt["termios_after_exit"], baseline)


if __name__ == "__main__":
    unittest.main()
