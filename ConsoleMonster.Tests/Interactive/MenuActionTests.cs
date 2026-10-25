using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// What happens around menu actions: console output collected instead of drawn over the app, async actions that ask
    /// questions through the menu's thread, the blocked Run button that opens the fix, and leaving a form with unsaved
    /// changes. In the live-session collection because actions redirect Console.Out.
    /// </summary>
    [Collection("Live sessions")]
    public sealed class MenuActionTests {
        private static RecordingSurface Run(Menu menu, string keys, MenuLayout layout, bool realTime = false, int width = 100, int height = 26) {
            RecordingSurface surface = new RecordingSurface(width, height, false);
            InteractiveOptions options = PromptHarness.Options(keys, surface);
            ((ScriptedInput)options.Input!).RealTime = realTime;
            PromptHarness.Guard(surface, () => {
                menu.Run(new MenuOptions { Layout = layout, Interactive = options, StorageRoot = Path.Combine(Path.GetTempPath(), "cm-menu-none-" + Guid.NewGuid().ToString("N").Substring(0, 6)) });
                return 0;
            });
            return surface;
        }

        [Fact]
        public void Action_ConsoleOutput_BecomesAToastOrAnOutputDialog() {
            Menu menu = new Menu("Tools");
            menu.Item("Say", i => i.Hotkey('s').Action(ctx => Console.WriteLine("hello from the action")));
            menu.Item("Many", i => i.Hotkey('m').Action(ctx => {
                Console.WriteLine("first line");
                Console.Error.WriteLine("second line failed");
                ctx.Log("third line", StatusKind.Warn);
            }));
            menu.Exit("Quit");
            RecordingSurface surface = Run(menu, "s m x Ctrl+Q", MenuLayout.Classic);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("hello from the action"));
            string dialog = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Many · Output"));
            Assert.Contains("first line", dialog);
            Assert.Contains("second line failed", dialog);
            Assert.Contains("third line", dialog);
            Assert.Contains("any key closes", dialog);
            Assert.DoesNotContain("first line", surface.LastText());
        }

        [Fact]
        public void AsyncAction_AsksThroughTheMenuThread() {
            Menu menu = new Menu("Tools");
            List<string> log = new List<string>();
            TaskCompletionSource<bool> ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int menuThread = Thread.CurrentThread.ManagedThreadId, actionThread = menuThread;
            menu.Item("Deploy", i => i.Hotkey('d').ActionAsync(async (ctx, token) => {
                using (token.Register(() => ready.TrySetCanceled())) {
                    await ready.Task.ConfigureAwait(false);
                }
                actionThread = Thread.CurrentThread.ManagedThreadId;
                bool yes = ctx.Confirm("Deploy to staging?", false);
                log.Add(yes ? "deployed" : "stopped");
                ctx.Toast(yes ? "Deployed" : "Stopped");
            }));
            menu.Exit("Quit");
            RecordingSurface surface = new RecordingSurface(100, 26, false);
            using (AsyncQuestionInput input = new AsyncQuestionInput(surface, () => ready.TrySetResult(true))) {
                InteractiveOptions options = PromptHarness.Options(string.Empty, surface);
                options.Input = input;
                PromptHarness.Guard(surface, () => {
                    menu.Run(new MenuOptions {
                        Layout = MenuLayout.Classic, Interactive = options,
                        StorageRoot = Path.Combine(Path.GetTempPath(), "cm-menu-none-" + Guid.NewGuid().ToString("N").Substring(0, 6))
                    });
                    return 0;
                });
            }
            Assert.NotEqual(menuThread, actionThread);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Deploy to staging?"));
            Assert.Equal(new[] { "deployed" }, log.ToArray());
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Deployed"));
        }

        // Input follows observed UI states; the deadline only detects a stuck interaction.
        private sealed class AsyncQuestionInput : IInputSource {
            private readonly RecordingSurface _surface;
            private readonly Action _release;
            private readonly ScriptedInput _keys = new ScriptedInput("d y Ctrl+Q") { Paced = true };
            private readonly System.Diagnostics.Stopwatch _watchdog = System.Diagnostics.Stopwatch.StartNew();
            private int _step;

            public AsyncQuestionInput(RecordingSurface surface, Action release) { _surface = surface; _release = release; }
            public string Name => "async menu question and toast";

            public InputEvent? Read(TimeSpan timeout, CancellationToken cancellationToken) {
                cancellationToken.ThrowIfCancellationRequested();
                if (timeout == TimeSpan.Zero) { return null; }
                if (_step == 1) { _release(); }
                string expected = _step == 1 ? "Deploy to staging?" : "Deployed";
                if (_step == 0 || _step >= 3 || _surface.LastText().Contains(expected)) {
                    _step++;
                    return _keys.Read(timeout, cancellationToken);
                }
                if (_watchdog.Elapsed > TimeSpan.FromSeconds(30)) {
                    throw new TimeoutException("The menu never rendered " + expected + ".\n" + _surface.LastText());
                }
                cancellationToken.WaitHandle.WaitOne(timeout);
                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }

            public void Dispose() { _keys.Dispose(); }
        }

        [Fact]
        public void MenuContext_FromAnotherThread_IsRefusedClearly() {
            Menu menu = new Menu("Tools");
            Exception? error = null;
            menu.Item("Leak", i => i.Hotkey('l').Action(ctx => {
                // A real second thread (Task.Wait could run the work inline on the menu's own thread).
                Thread stray = new Thread(() => {
                    try {
                        ctx.Confirm("From a stray thread?");
                    } catch (Exception ex) {
                        error = ex;
                    }
                });
                stray.Start();
                stray.Join();
            }));
            menu.Exit("Quit");
            Run(menu, "l Ctrl+Q", MenuLayout.Classic);
            InvalidOperationException refused = Assert.IsType<InvalidOperationException>(error);
            Assert.Contains("menu's own thread", refused.Message);
        }

        [Fact]
        public void Review_BlockedRun_ShowsTheCountAndOpensTheItemThatFixesIt() {
            List<string> log = new List<string>();
            Menu menu = new Menu("Toolbox");
            MenuItem targets = new MenuItem("Targets").Hotkey('t').MultiSelect(new[] { "SRV01", "SRV02" }, s => s);
            menu.Add(targets);
            menu.Item("Review", i => i.Hotkey('r').Review(ctx => {
                ReviewModel review = new ReviewModel();
                review.Row("Targets", string.Join(", ", targets.GetSelection<string>()));
                if (targets.Selection.Count == 0) {
                    review.Problem("Pick at least one target", targets);
                }
                review.Run = c => log.Add("run");
                return review;
            }));
            menu.Exit("Quit");
            RecordingSurface surface = Run(menu, "r Enter Space Escape Escape r Enter Ctrl+Q", MenuLayout.Shell);
            string blocked = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("Run · 1 problem"));
            Assert.Contains("✗ Pick at least one target", blocked);
            Assert.True(blocked.IndexOf("✗ Pick at least one target", StringComparison.Ordinal) > blocked.IndexOf("Targets  ", StringComparison.Ordinal));
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Toolbox › Review › Targets"));
            Assert.Equal(new[] { "run" }, log.ToArray());
        }

        [Fact]
        public void Form_LeavingWithUnsavedChanges_AsksAndDiscardRestoresTheValues() {
            Menu menu = new Menu("Toolbox");
            Form options = new Form("Options") { Review = false };
            TextField folder = options.Text("Folder", "Report folder");
            folder.Default = @"C:\Reports";
            menu.Item("Options", i => i.Hotkey('o').Form(options));
            menu.Exit("Quit");
            RecordingSurface surface = Run(menu, "o End \"X\" Escape n Escape y Ctrl+Q", MenuLayout.Shell);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Discard the changes to Options?"));
            Assert.Equal(@"C:\Reports", options.Find("Folder")!.Value);
        }
    }
}
