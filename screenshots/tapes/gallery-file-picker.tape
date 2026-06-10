# gallery-file-picker.tape — Termina.Demo.Gallery file picker page.
# Directory navigation, type-to-filter, file selection, and folder selection.
Output docs/public/gallery/gallery-file-picker.gif

Source screenshots/tapes/_common.tape

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Gallery"
Enter
Sleep 5s
Show

# Quick-select the File Picker page (menu item 6).
Sleep 1s
Type "6"
Sleep 2s

# Left picker: browse the repo with the file picker.
Down
Sleep 600ms
Down
Sleep 600ms
Enter
Sleep 1500ms

# Back up to the repo root.
Backspace
Sleep 1200ms

# Type-to-filter: find README.md and select it.
Type "read"
Sleep 1500ms
Enter
Sleep 2000ms

# Tab to the folder picker (right side).
Tab
Sleep 1000ms
Down
Sleep 600ms
Down
Sleep 800ms

# Space selects the highlighted folder.
Space
Sleep 2500ms

Hide
Escape
Sleep 500ms
Type "q"
Sleep 1s
