# gallery-clipboard.tape — Termina.Demo.Gallery clipboard page.
# Copyable text with toast and inline-indicator feedback.
Output docs/public/gallery/gallery-clipboard.gif

Set Shell "bash"
Set Theme "Catppuccin Mocha"
Set Width 1200
Set Height 700
Set FontSize 16
Set Padding 20
Set TypingSpeed 50ms

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Gallery"
Enter
Sleep 5s
Show

# Quick-select the Clipboard page (menu item 3).
Sleep 1s
Type "3"
Sleep 2s

# OAuth URL: select all, then copy -> toast feedback (top-right).
Ctrl+A
Sleep 900ms
Enter
Sleep 2500ms

# Access token: Tab to it, select all, copy -> inline check feedback.
# (Enter copies; Ctrl+C is avoided — the terminal would treat it as SIGINT.)
Tab
Sleep 800ms
Ctrl+A
Sleep 700ms
Enter
Sleep 2800ms

Hide
Escape
Sleep 500ms
Type "q"
Sleep 1s
