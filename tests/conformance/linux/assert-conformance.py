#!/usr/bin/env python3
import argparse
import json
import sys
from pathlib import Path


def load_events(path: Path):
    events = []
    for line in path.read_text().splitlines():
        line = line.strip()
        if not line:
            continue
        events.append(json.loads(line))
    return events


def require(condition: bool, message: str):
    if not condition:
        raise AssertionError(message)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--events", required=True)
    parser.add_argument("--selection")
    parser.add_argument("--ansi-capture")
    parser.add_argument("--expect-wheel", action="store_true")
    parser.add_argument("--expect-arrows", action="store_true")
    parser.add_argument("--expect-selection")
    parser.add_argument("--require-alt-scroll-sequence", action="store_true")
    parser.add_argument("--require-alt-scroll-enable-only", action="store_true")
    parser.add_argument("--require-no-mouse-tracking", action="store_true")
    parser.add_argument("--require-legacy-mouse-tracking", action="store_true")
    parser.add_argument("--require-kitty-sequence")
    args = parser.parse_args()

    events = load_events(Path(args.events))
    require(
        any(evt.get("phase") == "startup" for evt in events), "missing startup event"
    )

    if args.expect_wheel:
        require(
            any(evt.get("kind") == "MouseScrollEvent" for evt in events),
            "missing MouseScrollEvent",
        )

    if args.expect_arrows:
        keys = [evt for evt in events if evt.get("kind") == "KeyPressed"]
        require(
            any(evt.get("key") == "UpArrow" for evt in keys), "missing UpArrow event"
        )
        require(
            any(evt.get("key") == "DownArrow" for evt in keys),
            "missing DownArrow event",
        )

    if args.expect_selection:
        selection_text = Path(args.selection).read_text()
        require(
            args.expect_selection in selection_text,
            f"selection did not contain '{args.expect_selection}'",
        )

    if args.ansi_capture:
        capture = Path(args.ansi_capture).read_bytes()
        if args.require_alt_scroll_sequence:
            require(
                b"\x1b[?1007h" in capture, "missing alternate-scroll enable sequence"
            )
            require(
                b"\x1b[?1007l" in capture, "missing alternate-scroll disable sequence"
            )
            require(b"\x1b[?1h" in capture, "missing DECCKM enable sequence")
            require(b"\x1b[?1l" in capture, "missing DECCKM disable sequence")

        if args.require_alt_scroll_enable_only:
            require(
                b"\x1b[?1007h" in capture, "missing alternate-scroll enable sequence"
            )
            require(b"\x1b[?1h" in capture, "missing DECCKM enable sequence")

        if args.require_no_mouse_tracking:
            require(
                b"\x1b[?1000h" not in capture,
                "unexpected mouse tracking enable sequence",
            )
            require(
                b"\x1b[?1006h" not in capture, "unexpected SGR mouse enable sequence"
            )

        if args.require_legacy_mouse_tracking:
            require(
                b"\x1b[?1000h" in capture,
                "missing normal mouse tracking enable sequence",
            )
            require(
                b"\x1b[?1006h" in capture,
                "missing SGR mouse enable sequence",
            )
            require(
                b"\x1b[?1007h" not in capture,
                "unexpected alternate-scroll enable sequence",
            )

        if args.require_kitty_sequence:
            expected = f"\x1b[>{args.require_kitty_sequence}u".encode()
            require(
                expected in capture,
                f"missing kitty enable sequence {args.require_kitty_sequence}",
            )
            require(b"\x1b[<u" in capture, "missing kitty disable sequence")

    print("conformance assertions passed")


if __name__ == "__main__":
    try:
        main()
    except AssertionError as exc:
        print(f"ASSERTION FAILED: {exc}", file=sys.stderr)
        sys.exit(1)
