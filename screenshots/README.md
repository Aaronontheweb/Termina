# Termina Demo Screenshots

This directory holds the pipeline that records the animated GIFs of Termina's
demo apps shown throughout the documentation. It uses
[**VHS**](https://github.com/charmbracelet/vhs) — a tool that drives a terminal
with a scripted `.tape` file and records the result.

```
screenshots/
  capture.sh        orchestration script (build demos -> run tapes)
  tapes/*.tape      one VHS script per demo / feature
docs/public/gallery/  generated GIFs (committed, served at /termina/gallery/)
```

The generated assets are committed to the repo under `docs/public/gallery/` and
deployed automatically by the `docs.yml` GitHub Actions workflow — there is **no
CI step that runs VHS**. Regeneration is a local/manual task: run `capture.sh`
when a demo's UI changes.

## Prerequisites

| Tool | Purpose | Install |
|------|---------|---------|
| .NET 10 SDK | builds & runs the demos | <https://dotnet.microsoft.com/download> |
| `vhs` | records the terminal | `brew install vhs` &nbsp;or&nbsp; `go install github.com/charmbracelet/vhs@latest` &nbsp;or&nbsp; [release binary](https://github.com/charmbracelet/vhs/releases) |
| `ttyd` | terminal VHS records into | `brew install ttyd` &nbsp;or&nbsp; `apt install ttyd` |
| `ffmpeg` | encodes frames into GIF | `brew install ffmpeg` &nbsp;or&nbsp; `apt install ffmpeg` |

`capture.sh` checks for all four and fails fast with a pointer here if any are
missing.

## Usage

Run from anywhere in the repo:

```bash
# Regenerate everything (builds all demos, runs all tapes)
./screenshots/capture.sh

# Regenerate a single asset while iterating on a tape
./screenshots/capture.sh wizard
```

Output lands in `docs/public/gallery/`. Preview it locally with the docs site:

```bash
cd docs && npm install && npm run dev
```

## Embedding a GIF in the docs

Assets in `docs/public/` are served at the site root, so reference them
**without** the `/termina/` base path — VitePress adds it automatically:

```markdown
![SpinnerNode demo: several spinner styles animating](/gallery/gallery-spinners.gif)
```

The root `README.md` is rendered by GitHub, which does not know the VitePress
base — it must use an absolute raw URL instead:

```markdown
![Termina in action](https://raw.githubusercontent.com/Aaronontheweb/termina/refs/heads/dev/docs/public/gallery/hero-overview.gif)
```

## Adding a new tape

1. Copy an existing `tapes/*.tape` as a starting point.
2. Keep the standard `Set` header (theme `Catppuccin Mocha`, 1200x700) so the
   new GIF matches the others. Use `Set Width 1400` only if content clips.
3. Launch the demo inside a `Hide` / `Show` block with
   `dotnet run --no-build -c Release --project demos/<Demo>` so the build/launch
   noise is not recorded.
4. Point the tape's `Output` at `docs/public/gallery/`.
5. If the tape records a brand-new demo project, add that project to the build
   loop in `capture.sh`.

## Notes

- The demos have a `--test` flag that injects its own scripted keystrokes. It is
  **intentionally not used** here — VHS drives the real keystrokes itself.
- Demos quit cleanly inside a trailing `Hide` block so the GIF ends on the last
  app frame (a clean loop) rather than on a shell prompt.
- Keep recordings short (≈8–20s). GIFs are committed to git; Git LFS is not used
  because the README references assets over `raw.githubusercontent.com`, which
  serves LFS pointer files rather than the binary.
