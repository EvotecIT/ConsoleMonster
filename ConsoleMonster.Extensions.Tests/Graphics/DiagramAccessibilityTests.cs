using System;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Graphics;
using ConsoleMonster.Layout;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Extensions.Tests.Graphics {
    public sealed class DiagramAccessibilityTests {
        public DiagramAccessibilityTests() { DiagramRendererProvider.Register(); }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AccessibleTree_KeepsStatusFocusAndActiveFailureWhenCompanionsAreDisabled(bool companions) {
            var diagram = Ui.Diagram("Ordering")
                .Node("Shopper").Node("API", node => node.Status(StatusKind.Warn))
                .Node("Queue", node => node.Status(StatusKind.Fail))
                .Link("Shopper", "API", link => link.Label("POST"))
                .Link("API", "Queue", link => link.Label("enqueue").Status(StatusKind.Fail))
                .UseCase("Order", useCase => useCase.Step("Shopper", "API", "POST")
                    .Step("API", "Queue", "Rejected", step => step.Kind(DiagramStepKind.Failed)));
            diagram.ShowSummary = diagram.ShowLegend = diagram.ShowDetails = diagram.ShowStepPanel = companions;
            diagram.FocusNode("API");
            diagram.Step(1);
            var profile = TerminalProfile.Fixed(120, 60, ColorDepth.None, GlyphTier.Ascii).WithAccessibility(true, true);
            string text = FrameComposer.RenderBlock(diagram, 120, new RenderContext(profile, Theme.Dark)).ToPlainText();
            Assert.Contains("API (Warn) (focused)", text);
            Assert.Contains("Queue (Fail)", text);
            Assert.Contains("enqueue (link Fail) (active failed)", text);
            Assert.DoesNotContain("+---", text);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReducedMotion_HoldsTheCurrentStepAndResumesWithoutSkipping(bool accessible) {
            var diagram = Ui.Diagram("Playback").UseCase("Order", useCase => useCase.Delay(1000)
                .Step("A", "B", "first").Reply("B", "A", "second").Step("A", "B", "third"));
            diagram.Play();
            void Frame(int seconds, bool reduced) {
                var profile = TerminalProfile.Fixed(120, 60, ColorDepth.None, GlyphTier.Full).WithAccessibility(reduced && accessible, reduced);
                var context = new RenderContext(profile, Theme.Dark, elapsed: TimeSpan.FromSeconds(seconds));
                FrameComposer.RenderBlock(diagram, 120, context);
            }
            Frame(10, false);
            Assert.Equal(0, diagram.StepIndex);
            Frame(50, true); Frame(100, true);
            Assert.Equal(0, diagram.StepIndex);
            Frame(0, false); // A resize can replay a stored context while the motion preference changes.
            Assert.Equal(0, diagram.StepIndex);
            Frame(101, false);
            Assert.Equal(0, diagram.StepIndex);
            Frame(102, false);
            Assert.Equal(1, diagram.StepIndex);
        }
    }
}
