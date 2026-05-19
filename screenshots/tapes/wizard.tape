# wizard.tape — Termina.Demo.Wizard multi-step setup wizard.
# Steps through Provider -> Authentication -> Confirm.
Output docs/public/gallery/wizard.gif

Set Shell "bash"
Set Theme "Catppuccin Mocha"
Set Width 1200
Set Height 440
Set FontSize 16
Set Padding 20
Set TypingSpeed 50ms

# Sleep inside the hidden block also swallows the demo's stderr trace line.
Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Wizard"
Enter
Sleep 5s
Show

# Step 1: pick a cloud provider (AWS -> Azure -> GCP).
Sleep 1500ms
Down
Sleep 700ms
Down
Sleep 900ms
Enter
Sleep 1500ms

# Step 2: type a username.
Type "admin"
Sleep 800ms
Enter
Sleep 1500ms

# Step 3: review the summary, then complete setup.
Sleep 2000ms
Enter
Sleep 3000ms

Hide
Type "q"
Sleep 1s
