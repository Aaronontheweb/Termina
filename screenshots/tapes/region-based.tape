# region-based.tape — Termina.Demo.RegionBased reactive counter.
# Shows multiple reactive properties bound to region-based rendering.
Output docs/public/gallery/region-counter.gif

Set Shell "bash"
Set Theme "Catppuccin Mocha"
Set Width 1200
Set Height 700
Set FontSize 16
Set Padding 20
Set TypingSpeed 50ms

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.RegionBased"
Enter
Sleep 5s
Show

# Drive the counter.
Sleep 1s
Up
Sleep 550ms
Up
Sleep 550ms
Up
Sleep 550ms
Up
Sleep 800ms
Down
Sleep 1200ms

# Send a couple of messages through the input.
Type "Region-based rendering"
Sleep 500ms
Enter
Sleep 1200ms
Type "Only changed cells repaint"
Sleep 500ms
Enter
Sleep 1500ms

# A few more counter ticks, then hold.
Up
Sleep 500ms
Up
Sleep 2500ms

Hide
Escape
Sleep 1s
