using System;
using System.Linq;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    [Collection("Live sessions")]
    public sealed class WidgetPagerTests {
        private static TextBlock Document(int count = 40) => TextBlock.Plain(string.Join("\n", Enumerable.Range(1, count).Select(i => "Line " + i.ToString("00", System.Globalization.CultureInfo.InvariantCulture))));

        [Fact]
        public void LongDocument_RespondsToNavigationAndQuit() {
            RecordingSurface surface = new RecordingSurface(60, 10, false);
            InteractiveOptions options = PromptHarness.Options("Down PageDown End Home Escape", surface);
            Assert.True(PromptHarness.Guard(surface, () => WidgetPager.TryShow(Document(), "Read [literal]", options)));
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Line 02") && !f.ToPlainText().Contains("Line 01"));
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Line 10"));
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Line 40"));
            Assert.Contains("Line 01", surface.Frames.Last().ToPlainText());
            Golden.Match("interactive/markdown.pager.txt", surface.Frames.First().ToPlainText().TrimEnd() + "\n");
        }

        [Fact]
        public void ShortOrNonInteractiveDocument_DoesNotAcquireInput() {
            using (ScriptedInput input = new ScriptedInput("Escape")) {
                InteractiveOptions options = new InteractiveOptions { Input = input, Profile = TerminalProfile.Fixed(40, 10), Mode = InteractionMode.Rich };
                Assert.False(WidgetPager.TryShow(Document(10), options: options));
                options.NonInteractive = true;
                Assert.False(WidgetPager.TryShow(Document(), options: options));
                options.NonInteractive = false;
                options.Profile = options.Profile.WithMode(OutputMode.Stream);
                Assert.False(WidgetPager.TryShow(Document(), options: options));
                Assert.True(input.Read(TimeSpan.Zero, CancellationToken.None)!.Is(ConsoleKey.Escape));
            }
        }

        [Fact]
        public void Resize_ReflowsAndClampsViewport() {
            TextBlock document = TextBlock.Plain(string.Join("\n", Enumerable.Repeat("alpha beta gamma delta epsilon zeta", 6)));
            WidgetPagerView view = new WidgetPagerView(document, "Document", new PromptLabels());
            RenderContext context = new RenderContext(TerminalProfile.Fixed(20, 6), Theme.Dark);
            RecordingSurface surface = new RecordingSurface(20, 6, false);
            InteractionLoop.Draw(view, surface, context, 0, TimeSpan.Zero);
            view.Handle(InputEvent.Press(ConsoleKey.End));
            InteractionLoop.Draw(view, surface, context, 1, TimeSpan.Zero);
            surface = new RecordingSurface(60, 10, false);
            InteractionLoop.Draw(view, surface, context, 2, TimeSpan.Zero);
            string text = surface.Frames.Last().ToPlainText();
            Assert.Contains("1–6 / 6", text);
            Assert.Contains("alpha beta gamma delta epsilon zeta", text);
        }

        [Fact]
        public void Cancellation_ReleasesInputLease() {
            using (CancellationTokenSource stop = new CancellationTokenSource()) {
                using (CancellingInput input = new CancellingInput(stop)) {
                    IInputSource? previous = ConsoleInput.Override;
                    ConsoleInput.Override = input;
                    try {
                        InteractiveOptions options = new InteractiveOptions {
                            Mode = InteractionMode.Rich, Capture = new ScreenCapture(40, 10), Theme = Theme.Dark
                        };
                        Assert.Throws<OperationCanceledException>(() => WidgetPager.TryShow(Document(), options: options, cancellationToken: stop.Token));
                        Assert.False(ConsoleInput.IsAcquired);
                        using (InputLease lease = ConsoleInput.Acquire()) {
                            Assert.Same(input, lease.Source);
                        }
                    } finally {
                        ConsoleInput.Override = previous;
                    }
                }
            }
        }

        private sealed class CancellingInput : IInputSource {
            private readonly CancellationTokenSource _stop;

            internal CancellingInput(CancellationTokenSource stop) => _stop = stop;

            public string Name => "cancellation fixture";

            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                _stop.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }

            public void Dispose() { }
        }
    }
}
