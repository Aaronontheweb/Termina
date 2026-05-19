# modal.tape — Termina.Demo modal overlay.
# Opens with the add-task modal already up so the GIF leads on a modal,
# then submits to the priority picker modal.
Output docs/public/gallery/modal.gif

Source screenshots/tapes/_common.tape

# Launch, navigate to the todo list, and open the add-task modal — all
# hidden — so the first visible frame already shows a modal overlay.
Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo"
Enter
Sleep 5s
Type "t"
Sleep 1200ms
Type "a"
Sleep 1200ms
Show

# Frame 1: the "Add New Task" modal over a dimmed backdrop.
Sleep 1500ms
Type "Deploy release to production"
Sleep 1500ms

# Submit -> the priority picker modal appears.
Enter
Sleep 2s

# Move through the priority options, then confirm.
Down
Sleep 800ms
Up
Sleep 1200ms
Enter
Sleep 2500ms

Hide
Type "q"
Sleep 1s
