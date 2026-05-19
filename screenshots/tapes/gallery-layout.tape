# gallery-layout.tape — Termina.Demo.Gallery layout page.
# A 2x2 grid showcasing vertical, horizontal, panel, and nested layouts.
Output docs/public/gallery/gallery-layout.gif

Source screenshots/tapes/_common.tape

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
