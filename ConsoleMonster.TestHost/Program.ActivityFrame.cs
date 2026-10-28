using System;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Rendering;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.TestHost {
    public static partial class Program {
        private static void WriteActivityFrame() {
            var activities = new ActivityList { ShowIds = true, ShowElapsed = false };
            foreach (string key in new[] { "collecting", "evaluating", "report", "zone-甲" }) {
                activities.Apply(new LiveEvent(ActivityList.ProgressTarget, "update", key: key, text: key, value: -1, total: -1));
            }
            var context = new RenderContext(TerminalProfile.Fixed(70, 8, ColorDepth.None, GlyphTier.Full), Theme.Dark)
                .WithClock(7, TimeSpan.Zero);
            Console.Write(FrameComposer.RenderBlock(activities, 70, context).ToPlainText());
        }
    }
}
