# gallery-animations.tape — Termina.Demo.Gallery animations page.
# Cycles spinner styles with a live, continuously animating preview.
Output docs/public/gallery/gallery-spinners.gif

Source screenshots/tapes/_common.tape

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Gallery"
Enter
Sleep 5s
Show

# Quick-select the Animations page (menu item 5).
Sleep 1s
Type "5"
Sleep 2s

# Step through spinner styles; Enter previews each one live.
Down
Sleep 400ms
Enter
Sleep 2200ms
Down
Sleep 400ms
Enter
Sleep 2200ms
Down
Sleep 400ms
Enter
Sleep 2200ms
Down
Sleep 400ms
Enter
Sleep 2200ms
Down
Sleep 400ms
Enter
Sleep 3000ms

Hide
Escape
Sleep 500ms
Type "q"
Sleep 1s
