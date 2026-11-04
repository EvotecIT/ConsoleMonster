using ConsoleMonster.Graphics;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Live;
using ConsoleMonster.Output;
using ConsoleMonster.Widgets;

// ConsoleMonster.Graphics draws diagrams with the ChartForgeX layout; without it a diagram prints as a tree.
DiagramRendererProvider.Register();

// The same architecture file ChartForgeX draws as SVG, PNG or an HTML player.
bool capture = args.Contains("--capture");
Diagram diagram = Diagram.Load(Path.Combine(
    capture ? Environment.CurrentDirectory : AppContext.BaseDirectory, "ad-replication.cfx"));

// Play the first use case; Left and Right step, Tab switches use case, Up and Down focus a node, Esc quits.
diagram.Play();
if (!capture) { diagram.Run(); return; }

// Recording uses the same app and playback with explicit logical checkpoints, independent of repaint scheduling.
InteractiveOptions interactive = InteractiveOptions.Default.Clone();
interactive.Mode = InteractionMode.Rich;
interactive.NonInteractive = false;
interactive.Input = new ScriptedInput(string.Empty) { EndsQuietly = true };
var recording = interactive.Target as RecordingTarget
    ?? throw new InvalidOperationException("A recording target is required.");
int checkpoints = 0;
diagram.Run(new ConsoleAppOptions {
    Interactive = interactive,
    Mouse = false,
    ProcessEvents = state => {
        if (checkpoints == 8) { state.RequestExit(); return; }
        LiveSession session = LiveSession.Current
            ?? throw new InvalidOperationException("The app has no live session.");
        session.Delay(TimeSpan.FromMilliseconds(900));
        recording.Screens.Hold(TimeSpan.FromMilliseconds(900));
        checkpoints++;
    }
});
if (checkpoints != 8 || diagram.StepIndex != 7)
    throw new InvalidOperationException("The recording did not reach the eighth playback step.");
