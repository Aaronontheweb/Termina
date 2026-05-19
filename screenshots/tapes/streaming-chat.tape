# streaming-chat.tape — Termina.Demo.Streaming.
# Token-by-token streaming text plus an interactive decision list.
Output docs/public/gallery/streaming-chat.gif

Source screenshots/tapes/_common.tape
Set Width 1400
Set Height 680

# Longer startup sleep: the demo spins up an Akka.NET ActorSystem.
Hide
Type "dotnet run --no-build -c Release --project demos/Termina.Demo.Streaming"
Enter
Sleep 8s
Show

# The "decision" keyword deterministically triggers a branching scenario:
# a "thinking" indicator, streamed intro text, then an interactive decision list.
Sleep 1500ms
Type "decision"
Sleep 600ms
Enter
Sleep 7s

# Navigate the decision list and pick a choice -> the follow-up streams in.
Down
Sleep 1000ms
Down
Sleep 1000ms
Enter
Sleep 9s

Hide
Ctrl+Q
Sleep 1s
