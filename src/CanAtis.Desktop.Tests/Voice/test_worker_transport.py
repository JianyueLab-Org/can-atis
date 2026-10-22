"""Run with the Python environment used to build the bundled voice worker."""
import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest

path = Path(__file__).resolve().parents[2] / "CanAtis.Desktop" / "can_mumble_worker.py"
spec = importlib.util.spec_from_file_location("can_worker", path)
worker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(worker)


class TransportTests(unittest.TestCase):
    def test_udp_uses_udp_rtt_and_tcp_fallback_uses_tcp_rtt(self):
        connection = SimpleNamespace(udp_active=True,
                                     udp_ping_stats={"nb": 2, "avg": 12.5},
                                     ping_stats={"nb": 3, "avg": 42.0})
        self.assertEqual(worker.transport_status(connection), ("UDP", 12.5))
        connection.udp_active = False
        self.assertEqual(worker.transport_status(connection), ("TCP", 42.0))

    def test_no_reply_does_not_show_library_default_as_measured_latency(self):
        connection = SimpleNamespace(ping_stats={"nb": 0, "avg": 40.0})
        self.assertEqual(worker.transport_status(connection), ("TCP", None))

    def test_invalid_latency_is_not_reported(self):
        for value in (-1, float("nan"), float("inf")):
            connection = SimpleNamespace(ping_stats={"nb": 1, "avg": value})
            self.assertEqual(worker.transport_status(connection), ("TCP", None))


if __name__ == "__main__":
    unittest.main()
