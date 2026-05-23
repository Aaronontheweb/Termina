# Running Termina Inside tmux

tmux is a terminal multiplexer that sits between your terminal emulator and the application. When you run a Termina app inside tmux, tmux becomes a second mouse owner and input intermediary that can affect how your app behaves.

This page explains the tmux settings you need for Termina apps to work correctly, what to expect in different configurations, and how to verify everything is working.

## Required Settings

### Enable Passthrough

Termina forwards bracketed-paste and kitty keyboard setup sequences to the outer terminal using tmux passthrough.

Without this, tmux silently drops the forwarded escape sequences, and features like bracketed paste will not work.

```bash
set -g allow-passthrough on
```

Add this to `~/.tmux.conf` so it applies to every new tmux server. To apply to an existing session without restarting:

```bash
tmux source ~/.tmux.conf
```

## Mouse Behavior

tmux has its own mouse handling that can interfere with native terminal selection. The `mouse` setting controls who owns mouse events.

### `mouse off` (recommended for Termina apps)

With `mouse off`, tmux passes mouse events through to the inner application. This allows:

- Native terminal drag-selection
- Clipboard copy via your terminal's built-in selection
- Termina apps to receive mouse events directly

```bash
set -g mouse off
```

This is the default in most tmux installations. If your app's `~/.tmux.conf` or a system-wide config enables mouse, you may need to override it.

### `mouse on`

With `mouse on`, tmux intercepts all mouse events. This means:

- tmux handles its own selection (Shift+drag)
- The inner application receives no mouse events
- Native terminal drag-selection from the inner app is not possible

Some users prefer `mouse on` for tmux's pane resizing, copy mode, and window switching. If you need both tmux mouse features and Termina app functionality, you can toggle per-session:

```bash
# Temporarily disable mouse for a Termina app
tmux set -g mouse off

# Re-enable after
tmux set -g mouse on
```

## Copy-On-Select Behavior

When `mouse off`, native drag-selection is handled by your **outer terminal emulator**, not by tmux or Termina. Each terminal has its own settings for whether selection automatically copies to the clipboard.

### kitty

Set in `~/.config/kitty/kitty.conf`:

```ini
copy_on_select clipboard
```

### WezTerm

Enabled by default. Configure in `wezterm.lua`:

```lua
config.front_end = "OpenGL"
config.copy_on_select = true
```

### Ghostty

Enabled by default. No configuration needed.

### Alacritty

Enabled by default. Selection copies to primary clipboard (middle-click paste).

### GNOME Terminal / VTE-based

Selection copies to primary clipboard automatically. Use middle-click to paste.

## Verifying Your Configuration

### Check tmux settings

```bash
tmux show -g mouse
tmux show -g allow-passthrough
```

Both should return `off` and `on` respectively for the recommended setup.

### Test bracketed paste

1. Copy a multi-line block of text outside of tmux
2. Open a Termina app or run this test command:
   ```bash
   cat
   ```
3. Paste with your terminal's paste shortcut (usually `Ctrl+Shift+V`)
4. If bracketed paste works, the full block appears intact with newlines preserved

### Test native selection

1. Ensure `mouse off` is set
2. Run any command that produces output:
   ```bash
   ls -la /usr/share/doc
   ```
3. Drag-select text with your mouse (no Shift needed)
4. Paste elsewhere with your terminal's paste shortcut

If selection requires Shift, check your outer terminal's configuration or verify tmux isn't intercepting the events.

## Troubleshooting

### Bracketed paste doesn't work

- Verify `allow-passthrough on` is set on the tmux server your app is running in
- Check for multiple tmux servers: `echo $TMUX` shows the socket path
- Settings apply per-server, so `tmux set -g` only affects the current server

### Selection requires Shift

- This is expected behavior when `mouse on` is set
- Either switch to `mouse off`, or use Shift+drag to bypass tmux's mouse handling
- Shift+drag always works regardless of tmux mouse setting

### Paste inserts literal escape sequences

- `allow-passthrough` may not be enabled
- Your tmux version may be older than 3.3 (check with `tmux -V`)
- Upgrade tmux or enable `allow-passthrough`

### Multiple tmux servers

If you have multiple tmux servers running, each maintains its own settings:

```bash
# Check which server your shell is attached to
echo $TMUX
# Output: /tmp/tmux-1000/default,2053,66

# Set options on a specific server
tmux -L default set -g allow-passthrough on
tmux -L default set -g mouse off
```

The easiest long-term solution is to put these in `~/.tmux.conf` so they apply automatically to every new server.

## Summary of Recommended Settings

Add these to `~/.tmux.conf`:

```bash
set -g allow-passthrough on
set -g mouse off
```

Then reload:

```bash
tmux source ~/.tmux.conf
```

With this configuration, Termina apps running inside tmux will have:

- Bracketed paste working through to the outer terminal
- Kitty keyboard protocol functioning (on supported terminals)
- Native terminal drag-selection available
- Clipboard copy working via your terminal's built-in selection

## See Also

- [Terminal Input Modes](/concepts/terminal-input-modes) — framework-level input mode configuration
- [Clipboard And Feedback](/advanced/clipboard-and-feedback) — Termina's clipboard transport architecture
- [Input Handling](/concepts/input-handling) — keyboard, mouse, and paste event routing
