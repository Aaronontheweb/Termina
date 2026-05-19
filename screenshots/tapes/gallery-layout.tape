# gallery-layout.tape — Termina.Demo.Gallery layout page.
# A 2x2 grid showcasing vertical, horizontal, panel, and nested layouts.
Output docs/public/gallery/gallery-layout.gif

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

# Quick-select the Layouts page (menu item 4) and hold on the showcase.
Sleep 1s
Type "4"
Sleep 5s

Hide
Escape
Sleep 500ms
Type "q"
Sleep 1s
