# hero.tape — Termina.Demo.Gallery guided tour for the docs hero.
# Menu -> animations -> selection lists, showing off the framework at a glance.
Output docs/public/gallery/hero-overview.gif

Set Shell "bash"
Set Theme "Catppuccin Mocha"
Set Width 1200
Set Height 560
Set FontSize 16
Set Padding 20
Set TypingSpeed 50ms

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Gallery"
Enter
Sleep 5s
Show

# Land on the gallery menu.
Sleep 1500ms
Down
Sleep 600ms
Down
Sleep 1000ms

# Animations: live spinner preview.
Type "5"
Sleep 2000ms
Down
Sleep 400ms
Enter
Sleep 2200ms
Down
Sleep 400ms
Enter
Sleep 2500ms
Escape
Sleep 1200ms

# Selection lists: keyboard-driven multi-list page.
Type "1"
Sleep 1800ms
Tab
Sleep 900ms
Tab
Sleep 1800ms
Escape
Sleep 2000ms

Hide
Type "q"
Sleep 1s
