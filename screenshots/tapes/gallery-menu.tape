# gallery-menu.tape — Termina.Demo.Gallery main menu.
# Keyboard navigation through the component gallery menu.
Output docs/public/gallery/gallery-menu.gif

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

# Walk the menu items.
Sleep 1500ms
Down
Sleep 700ms
Down
Sleep 700ms
Down
Sleep 700ms
Down
Sleep 900ms
Up
Sleep 700ms
Up
Sleep 2500ms

Hide
Type "q"
Sleep 1s
