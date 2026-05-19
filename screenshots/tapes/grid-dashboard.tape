# grid-dashboard.tape — Termina.Demo.Grid dashboard.
# Nested grids, progress bars, and a log table; metrics update live every 2s.
Output docs/public/gallery/grid-dashboard.gif

Source screenshots/tapes/_common.tape
Set Width 1400
Set Height 800

Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Grid"
Enter
Sleep 5s
Show

# Let the simulated metrics tick a couple of times.
Sleep 4500ms

# Select log rows.
Down
Sleep 700ms
Down
Sleep 700ms
Down
Sleep 1200ms

# Force a metrics refresh.
Type "r"
Sleep 2500ms

# Clear the selection and let metrics keep moving.
Type "c"
Sleep 4000ms

Hide
Escape
Sleep 1s
