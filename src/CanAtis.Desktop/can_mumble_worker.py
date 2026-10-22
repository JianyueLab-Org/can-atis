"""Small CAN ATIS Mumble sender used by the desktop client.

The protocol and channel lifecycle intentionally follow atis/broadcast.py:
connect with pymumble, create FREQ_xxxxxx as a temporary root channel when
needed, wait for the server echo, move in, then enqueue 20 ms PCM frames.
"""
import argparse
import os
import sys
import time

base = os.path.dirname(os.path.abspath(__file__))
python_dir = os.path.dirname(sys.executable)
sys.path.insert(0, base)
os.environ["PATH"] = (base + os.pathsep + python_dir + os.pathsep +
                       os.environ.get("PATH", ""))

import mumblecompat
mumblecompat.install()
import pymumble_py3 as pymumble
from pymumble_py3 import messages
from pymumble_py3.constants import PYMUMBLE_CLBK_CONNECTED


def find_channel(mumble, name):
    try:
        return mumble.channels.find_by_name(name)
    except Exception:
        return None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", required=True)
    parser.add_argument("--port", type=int, required=True)
    parser.add_argument("--cid", required=True)
    parser.add_argument("--password", required=True)
    parser.add_argument("--callsign", required=True)
    parser.add_argument("--frequency", required=True)
    args = parser.parse_args()

    khz = int(round(float(args.frequency) * 1000))
    channel_name = f"FREQ_{khz:06d}"
    user = f"{args.cid}_atis{khz:06d}_{args.callsign}"
    pcm = sys.stdin.buffer.read()
    if not pcm:
        return 2

    connected = False
    mumble = pymumble.Mumble(args.host, user, password=args.password,
                             port=args.port, reconnect=False)

    def on_connected():
        nonlocal connected
        connected = True

    mumble.callbacks.set_callback(PYMUMBLE_CLBK_CONNECTED, on_connected)
    mumble.start()
    deadline = time.time() + 15
    while not connected and time.time() < deadline:
        time.sleep(0.05)
    if not connected:
        mumble.stop()
        return 3

    channel = find_channel(mumble, channel_name)
    if channel is None:
        mumble.execute_command(messages.CreateChannel(0, channel_name, True),
                               blocking=False)
        deadline = time.time() + 5
        while channel is None and time.time() < deadline:
            channel = find_channel(mumble, channel_name)
            time.sleep(0.05)
    if channel is None:
        mumble.stop()
        return 4

    channel_id = channel["channel_id"]
    if mumble.users.myself["channel_id"] != channel_id:
        mumble.execute_command(
            messages.MoveCmd(mumble.users.myself_session, channel_id),
            blocking=False)
        deadline = time.time() + 5
        while mumble.users.myself["channel_id"] != channel_id and time.time() < deadline:
            time.sleep(0.05)
    if mumble.users.myself["channel_id"] != channel_id:
        mumble.stop()
        return 5

    frame_bytes = 960 * 2
    for offset in range(0, len(pcm), frame_bytes):
        frame = pcm[offset:offset + frame_bytes]
        if len(frame) < frame_bytes:
            frame += b"\x00" * (frame_bytes - len(frame))
        mumble.sound_output.add_sound(frame)
        time.sleep(0.02)

    deadline = time.time() + 5
    while time.time() < deadline:
        try:
            if mumble.sound_output.get_buffer_size() <= 0:
                break
        except Exception:
            break
        time.sleep(0.05)
    mumble.stop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
