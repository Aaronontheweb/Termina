# Terminal Conformance

This directory holds the first-pass terminal conformance harness for Termina.

Goals:

- verify that alternate-scroll does not regress native text selection
- verify that wheel input remains distinct from keyboard arrows when raw input + kitty reporting are enabled
- verify that fallback / baseline terminal behavior still produces the expected setup sequences

The initial automation target is Linux on GitHub-hosted runners:

- plain PTY capture
- `tmux`
- `kitty` under `Xvfb`

The suite intentionally does **not** merge into the main validation workflow yet. It is designed to be iterated on in a dedicated branch until the runner behavior is stable.
