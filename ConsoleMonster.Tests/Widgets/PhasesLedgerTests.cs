using System;
using System.Linq;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Tests.Rendering;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class PhasesLedgerTests {
        private static string Render(Phases phases, int width, GlyphTier tier) => Terminal.RenderText(
            new Panel(phases, "Connection ledger"), TerminalProfile.Fixed(width, 100, ColorDepth.None, tier), Theme.Dark);

        // A build ledger in a short inline area: the window follows the furthest started step, keeps the next one in
        // view, and does not go back to an older step that is still running while the next step has not started yet.
        [Fact]
        public void AShortLedger_FollowsTheFurthestStartedStepAndNeverGoesBack() {
            var phases = new Phases { StepCounter = true };
            for (int i = 1; i <= 12; i++) { phases.Set("s" + i, StatusKind.Pending, "Step " + i); }
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(40, 6, ColorDepth.None, GlyphTier.Ascii), Theme.Dark);
            string[] Window() {
                CellBuffer buffer = new CellBuffer(40, 6);
                phases.Render(new Region(buffer), context);
                return Enumerable.Range(0, buffer.Height).Select(y => buffer.GetRowText(y).TrimEnd()).ToArray();
            }
            Assert.Contains("Step 1", Window()[0]);
            phases.Set("s1", StatusKind.Pass).Set("s2", StatusKind.Pass).Set("s3", StatusKind.Running);
            phases.Set("s4", StatusKind.Running).Set("s4", StatusKind.Pass).Set("s5", StatusKind.Running).Set("s6", StatusKind.Running);
            string[] running = Window();
            Assert.Contains(running, row => row.Contains("Step 6"));
            Assert.Contains(running, row => row.Contains("Step 7"));
            // Step 3 is still running above the window: it stays pinned under "+3 earlier".
            Assert.Equal(new[] { "+3", "Step 3", "Step 5", "Step 6", "Step 7", "+5" },
                running.Select(row => row.StartsWith("+", StringComparison.Ordinal) ? row.Substring(0, 2) : row.Substring(row.IndexOf("Step", StringComparison.Ordinal), 6)));
            // Steps 5 and 6 finished and step 7 has not started; step 3 is still running further up.
            phases.Set("s5", StatusKind.Pass).Set("s6", StatusKind.Pass);
            string[] gap = Window();
            Assert.Contains(gap, row => row.Contains("Step 6"));
            Assert.Contains(gap, row => row.Contains("Step 7"));
            Assert.DoesNotContain(gap, row => row.Contains("Step 2"));
            phases.Set("s7", StatusKind.Running);
            Assert.Contains(Window(), row => row.Contains("Step 8"));
        }
        // A release run: a finished signing group with 149 package rows must not push the running next step off a short
        // area. The group folds into its parent row there; the final, unbounded ledger still lists every package.
        [Fact]
        public void AFinishedGroupFoldsIntoItsRowSoTheRunningStepStaysVisible() {
            var phases = new Phases { StepCounter = true };
            phases.Set("build", StatusKind.Pass, "Build", "14s").Set("sign", StatusKind.Running, "Sign NuGet packages");
            for (int i = 1; i <= 149; i++) { phases.SetItem("sign", "p" + i, StatusKind.Pass, "Package " + i + "/149", "signed"); }
            phases.Set("sign", StatusKind.Pass, null, "40s").Set("help", StatusKind.Running, "Extract help").Set("pack", StatusKind.Pending, "Pack");
            CellBuffer buffer = new CellBuffer(50, 6);
            phases.Render(new Region(buffer), Terminal.CreateContext(TerminalProfile.Fixed(50, 6, ColorDepth.None, GlyphTier.Ascii), Theme.Dark));
            string[] rows = Enumerable.Range(0, buffer.Height).Select(y => buffer.GetRowText(y).TrimEnd()).ToArray();
            Assert.Contains(rows, row => row.Contains("Extract help"));
            // The ASCII tier separates the count from the time with a hyphen, as its other separators do.
            Assert.Contains(rows, row => row.Contains("Sign NuGet packages") && row.EndsWith("149/149 - 40s", StringComparison.Ordinal));
            Assert.DoesNotContain(rows, row => row.Contains("Package 1/149"));
            string final = Render(phases, 50, GlyphTier.Ascii);
            Assert.Contains("Package 1/149", final);
            Assert.Contains("Package 149/149", final);
        }

        // A running row shows it is alive: its marker spins and its elapsed time follows the session clock; when it
        // finishes, the time it took stays fixed.
        [Fact]
        public void ARunningRowSpinsAndCountsItsTimeThenKeepsTheDuration() {
            var phases = new Phases("steps") { StepCounter = true };
            phases.Set("help", StatusKind.Pending, "Extract help");
            phases.Apply(new LiveEvent("steps", "state", "help", status: "running", time: 1000));
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(50, 3, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            string Row(long tick, double seconds) {
                CellBuffer buffer = new CellBuffer(50, 1);
                phases.Render(new Region(buffer), context.WithClock(tick, TimeSpan.FromSeconds(seconds)));
                return buffer.GetRowText(0).TrimEnd();
            }
            string early = Row(0, 3), later = Row(1, 6);
            Assert.EndsWith(NumberFormat.Duration(TimeSpan.FromSeconds(2)), early);
            Assert.EndsWith(NumberFormat.Duration(TimeSpan.FromSeconds(5)), later);
            Assert.NotEqual(early.Substring(8, 1), later.Substring(8, 1));
            phases.Apply(new LiveEvent("steps", "state", "help", status: "pass", time: 7500));
            Assert.EndsWith(NumberFormat.Duration(TimeSpan.FromSeconds(6.5)), Row(2, 60));
        }
        // A package lane planned after the run started: its step goes in where it belongs, its signing stage nests the
        // packages one level down, and once the stage finishes a short area folds it into one "done/total · time" row.
        [Fact]
        public void ALateStepGoesInPlaceAndAFinishedStageFoldsItsPackages() {
            var phases = new Phases("steps") { StepCounter = true };
            void Post(string op, string key, string? text = null, string? status = null, long time = 0) => phases.Apply(new LiveEvent("steps", op, key, text, status, time: time));
            string Item(params string[] path) => string.Join(Phases.ItemSeparator.ToString(), path);
            Post("add", "build", "Build module");
            Post("add", "help", "Extract help");
            Post("state", "build", status: "pass", time: 0);
            phases.Apply(new LiveEvent("steps", "insert", "lane", "Package lane", value: 1));
            Post("state", "lane", status: "running", time: 1000);
            Post("item", Item("lane", "sign"), "Sign NuGet packages", "running", 1000);
            for (int i = 1; i <= 30; i++) {
                Post("item", Item("lane", "sign", "p" + i), "Package " + i, "running", 1000 + i * 100);
                Post("item", Item("lane", "sign", "p" + i), status: "pass", time: 1100 + i * 100);
            }
            Post("item", Item("lane", "sign"), status: "pass", time: 13000);
            Assert.Equal(new[] { "build", "lane", "help" }, phases.Items.Select(p => p.Key));
            CellBuffer buffer = new CellBuffer(60, 5);
            phases.Render(new Region(buffer), Terminal.CreateContext(TerminalProfile.Fixed(60, 5, ColorDepth.None, GlyphTier.Ascii), Theme.Dark).WithClock(0, TimeSpan.FromSeconds(14)));
            string[] rows = Enumerable.Range(0, buffer.Height).Select(y => buffer.GetRowText(y).TrimEnd()).ToArray();
            Assert.Contains(rows, row => row.Contains("[02/03]") && row.Contains("Package lane"));
            Assert.Contains(rows, row => row.Contains("Sign NuGet packages") && row.EndsWith("30/30 - 12s", StringComparison.Ordinal));
            Assert.DoesNotContain(rows, row => row.Contains("Package 1 "));
            string[] final = Render(phases, 60, GlyphTier.Ascii).Split('\n');
            int stage = Array.FindIndex(final, row => row.Contains("Sign NuGet packages"));
            int package = Array.FindIndex(final, row => row.Contains("Package 30"));
            Assert.True(stage >= 0 && package > stage, "Packages are listed under their stage.");
            Assert.True(final[package].IndexOf("Package 30", StringComparison.Ordinal) > final[stage].IndexOf("Sign NuGet", StringComparison.Ordinal), "Packages hang one level further in than their stage.");
        }

        // A step that changes what it does gets a new label without losing its state or its running time.
        [Fact]
        public void AddingAnExistingStepChangesOnlyItsLabel() {
            var phases = new Phases("steps") { StepCounter = true };
            phases.Apply(new LiveEvent("steps", "add", "publish", "Publish"));
            phases.Apply(new LiveEvent("steps", "state", "publish", status: "running", time: 1000));
            phases.Apply(new LiveEvent("steps", "add", "publish", "Publish and sign outputs", time: 2000));
            CellBuffer buffer = new CellBuffer(60, 1);
            phases.Render(new Region(buffer), Terminal.CreateContext(TerminalProfile.Fixed(60, 1, ColorDepth.None, GlyphTier.Ascii), Theme.Dark).WithClock(0, TimeSpan.FromSeconds(5)));
            string row = buffer.GetRowText(0).TrimEnd();
            Assert.Equal(StatusKind.Running, phases.Items[0].Status);
            Assert.Contains("Publish and sign outputs", row);
            Assert.EndsWith(NumberFormat.Duration(TimeSpan.FromSeconds(4)), row);
        }

        // An item row has its own clock: it counts while running and keeps the time it took after its detail.
        [Fact]
        public void AnItemCountsItsTimeWhileRunningAndKeepsItWhenDone() {
            var phases = new Phases("steps") { StepCounter = true };
            string key = "sign" + Phases.ItemSeparator + "p1";
            phases.Apply(new LiveEvent("steps", "add", "sign", "Sign"));
            phases.Apply(new LiveEvent("steps", "state", "sign", status: "running", time: 0));
            phases.Apply(new LiveEvent("steps", "item", key, "Package 1", "running", time: 1000));
            phases.Apply(new LiveEvent("steps", "item-detail", key, "timestamping", time: 1000));
            RenderContext context = Terminal.CreateContext(TerminalProfile.Fixed(60, 2, ColorDepth.None, GlyphTier.Full), Theme.Dark);
            string ItemRow(double seconds) {
                CellBuffer buffer = new CellBuffer(60, 2);
                phases.Render(new Region(buffer), context.WithClock(0, TimeSpan.FromSeconds(seconds)));
                return buffer.GetRowText(1).TrimEnd();
            }
            Assert.EndsWith("timestamping · " + NumberFormat.Duration(TimeSpan.FromSeconds(3)), ItemRow(4));
            phases.Apply(new LiveEvent("steps", "item", key, status: "pass", time: 6000));
            phases.Apply(new LiveEvent("steps", "item-detail", key, "signed", time: 6000));
            Assert.EndsWith("signed · " + NumberFormat.Duration(TimeSpan.FromSeconds(5)), ItemRow(30));
        }

        // A long item result must not cut the item's label down to nothing and then draw over it: it goes below.
        [Fact]
        public void AnItemResultTooWideToShareTheRowGoesBelowItsLabel() {
            var phases = new Phases { StepCounter = true };
            phases.Set("sign", StatusKind.Running, "Sign");
            phases.SetItem("sign", "p1", StatusKind.Pass, "Package one", "timestamped by fallback");
            CellBuffer buffer = new CellBuffer(40, 4);
            phases.Render(new Region(buffer), Terminal.CreateContext(TerminalProfile.Fixed(40, 4, ColorDepth.None, GlyphTier.Ascii), Theme.Dark));
            string[] rows = Enumerable.Range(0, buffer.Height).Select(y => buffer.GetRowText(y).TrimEnd()).ToArray();
            int label = Array.FindIndex(rows, row => row.Contains("Package one"));
            Assert.True(label > 0, "The item label stays readable.");
            Assert.EndsWith("timestamped by fallback", rows[label + 1]);
        }

        // A running step with more running items than the area holds still leaves the next step in view.
        [Fact]
        public void AFrontierStepTallerThanTheAreaStillShowsTheNextStep() {
            var phases = new Phases { StepCounter = true };
            phases.Set("build", StatusKind.Pass, "Build").Set("sign", StatusKind.Running, "Sign").Set("pack", StatusKind.Pending, "Pack");
            for (int i = 1; i <= 20; i++) { phases.SetItem("sign", "p" + i, StatusKind.Running, "Package " + i); }
            CellBuffer buffer = new CellBuffer(40, 6);
            phases.Render(new Region(buffer), Terminal.CreateContext(TerminalProfile.Fixed(40, 6, ColorDepth.None, GlyphTier.Ascii), Theme.Dark));
            string[] rows = Enumerable.Range(0, buffer.Height).Select(y => buffer.GetRowText(y).TrimEnd()).ToArray();
            Assert.Contains("Sign", rows[0]);
            Assert.Contains("[03/03]", rows[5]);
            Assert.Contains("Package 4", rows[4]);
        }

        // Item paths join keys with U+001F, so a key containing it would alias another owner ("p\u001fa" as a phase and
        // item "a" of phase "p" would share one item list).
        [Fact]
        public void KeysCannotContainTheItemSeparator() {
            string joined = "p" + Phases.ItemSeparator + "a";
            var phases = new Phases().Set("p", StatusKind.Running).SetItem("p", "a", StatusKind.Running);
            Assert.Throws<ArgumentException>(() => phases.Set(joined));
            Assert.Throws<ArgumentException>(() => phases.Insert(0, joined));
            Assert.Throws<ArgumentException>(() => phases.SetItem("p", joined));
            Assert.Throws<ArgumentException>(() => phases.SetSubItem("p", "a", joined));
            Assert.Equal(new[] { "p" }, phases.Items.Select(p => p.Key));
        }

        [Theory]
        [InlineData(48, GlyphTier.Full)] [InlineData(40, GlyphTier.Full)]
        [InlineData(48, GlyphTier.Ascii)] [InlineData(40, GlyphTier.Ascii)]
        public void LabelAndResultDoNotSqueezeEachOther(int width, GlyphTier tier) {
            var phases = new Phases { StepCounter = true };
            phases.Set("chain", StatusKind.Fail, "Validate chain", "expired intermediate");
            phases.Set("expiry", StatusKind.Skip, "Expiry and revocation", "skipped");
            string[] lines = Render(phases, width, tier).Split('\n');
            int label = Array.FindIndex(lines, line => line.Contains("Validate chain"));
            int detail = Array.FindIndex(lines, line => line.Contains("expired intermediate"));
            Assert.True(label >= 0, "The full label should fit before the result is allocated space.");
            Assert.True(detail > label, "A result that cannot share the label's row belongs below it.");
            Assert.Contains(lines, line => line.Contains("Expiry and revocation"));
            Assert.Contains(lines, line => line.Contains("skipped"));
        }

        [Theory]
        [InlineData(48, GlyphTier.Full)] [InlineData(40, GlyphTier.Full)]
        [InlineData(48, GlyphTier.Ascii)] [InlineData(40, GlyphTier.Ascii)]
        public void HangingLabelsAndResultRowsMatchNarrowGoldens(int width, GlyphTier tier) {
            var phases = new Phases { StepCounter = true };
            phases.Set("dns", StatusKind.Pass, "Resolve DNS", "203.0.113.42");
            phases.Set("connect", StatusKind.Pass, "Connect :443", "connected through fallback route");
            phases.Set("handshake", StatusKind.Pass, "TLS handshake", "TLS 1.3");
            phases.Set("chain", StatusKind.Fail, "Validate certificate chain and issuer trust", "expired intermediate");
            phases.Set("expiry", StatusKind.Skip, "Expiry and revocation", "skipped");
            Golden.Match("phases-ledger." + width + "." + tier.ToString().ToLowerInvariant() + ".txt", Render(phases, width, tier));
        }
    }
}
