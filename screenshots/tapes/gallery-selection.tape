# gallery-selection.tape — Termina.Demo.Gallery selection list page.
# Single-select, multi-select, and a fill-height numbered list.
Output docs/public/gallery/gallery-selection-list.gif

Source screenshots/tapes/_common.tape

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Gallery"
Enter
Sleep 5s
Show

# Quick-select the Selection Lists page (menu item 1).
Sleep 1s
Type "1"
Sleep 2s

# List 1: single-select navigation.
Down
Sleep 600ms
Down
Sleep 1000ms

# Tab to list 2: multi-select with rich content.
Tab
Sleep 800ms
Down
Sleep 500ms
Space
Sleep 700ms
Down
Sleep 500ms
Space
Sleep 1000ms

# Tab to list 3: fill-height list, opens pre-selected at item #50.
Tab
Sleep 900ms
Down
Sleep 500ms
Down
Sleep 500ms
Up
Sleep 2500ms

Hide
Escape
Sleep 500ms
Type "q"
Sleep 1s
