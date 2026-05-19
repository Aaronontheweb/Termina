# todo.tape — Termina.Demo todo list page.
# Adds tasks via the modal, then toggles completion.
Output docs/public/gallery/todo-list.gif

Source screenshots/tapes/_common.tape
Set Height 500

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo"
Enter
Sleep 5s
Show

# Counter page -> todo list.
Sleep 1s
Type "t"
Sleep 1200ms

# Add "Buy groceries" (High priority).
Type "a"
Sleep 800ms
Type "Buy groceries"
Sleep 500ms
Enter
Sleep 800ms
Enter
Sleep 1s

# Add "Write release notes" (Medium priority).
Type "a"
Sleep 800ms
Type "Write release notes"
Sleep 500ms
Enter
Sleep 800ms
Down
Sleep 400ms
Enter
Sleep 1200ms

# Navigate and toggle completion.
Down
Sleep 600ms
Space
Sleep 2500ms

Hide
Type "q"
Sleep 1s
