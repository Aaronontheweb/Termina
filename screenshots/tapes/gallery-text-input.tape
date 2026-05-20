# gallery-text-input.tape — Termina.Demo.Gallery text input page.
# Basic input, placeholder input, and a multi-line text area.
Output docs/public/gallery/gallery-text-input.gif

Source screenshots/tapes/_common.tape

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Gallery"
Enter
Sleep 5s
Show

# Quick-select the Text Input page (menu item 2).
Sleep 1s
Type "2"
Sleep 2s

# Basic single-line input.
Type "Hello, Termina!"
Sleep 1200ms

# Tab to the placeholder input.
Tab
Sleep 700ms
Type "user@example.com"
Sleep 1200ms

# Tab to the multi-line text area.
Tab
Sleep 700ms
Type "Termina builds rich terminal UIs"
Sleep 2500ms

Hide
Escape
Sleep 500ms
Type "q"
Sleep 1s
