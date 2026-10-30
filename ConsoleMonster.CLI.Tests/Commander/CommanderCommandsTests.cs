using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Apps.Commander;
using ConsoleMonster.FileSystem;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Tests.FileSystem;
using Xunit;

namespace ConsoleMonster.Tests.Apps.Commander {
    public sealed class CommanderCommandsTests {
        private static (TempFolder Temp, CommanderModel Model, FakeHost Host, CommanderContext Context) Setup() {
            TempFolder temp = new TempFolder();
            temp.File("left/report.csv", "r");
            temp.File("left/notes.md", "n");
            temp.File("left/build.ps1", "b");
            temp.Folder("left/docs");
            temp.Folder("right/backup");
            CommanderModel model = new CommanderModel(new CommanderState {
                Left = new PanelState { Folder = temp.Combine("left"), ComputeFolderSizes = false },
                Right = new PanelState { Folder = temp.Combine("right"), ComputeFolderSizes = false }
            });
            FakeHost host = new FakeHost();
            return (temp, model, host, new CommanderContext(model, host));
        }

        private static InputEvent Key(string chord) {
            KeyChord parsed = KeyChord.Parse(chord);
            return parsed.IsCharacter ? InputEvent.Character(parsed.Char) : InputEvent.Press(parsed.Key, parsed.Modifiers);
        }

        [Fact]
        public void DefaultRegistry_BindsEverySpecKey() {
            CommanderCommands commands = CommanderCommands.CreateDefault();
            Dictionary<string, string> expected = new Dictionary<string, string> {
                ["Tab"] = "switch-panel", ["Enter"] = "open", ["Backspace"] = "parent", ["Ctrl+PgUp"] = "parent",
                ["Insert"] = "mark", ["Space"] = "mark", ["+"] = "mark-pattern", ["-"] = "unmark-pattern", ["*"] = "invert-marks",
                ["F1"] = "help", ["F2"] = "menu", ["F3"] = "view", ["Alt+3"] = "view", ["F4"] = "edit", ["Alt+4"] = "edit",
                ["F5"] = "copy", ["Alt+5"] = "copy", ["F6"] = "move", ["Alt+6"] = "move", ["F7"] = "mkdir", ["Alt+7"] = "mkdir",
                ["F8"] = "delete", ["Alt+8"] = "delete", ["Delete"] = "delete", ["Shift+F8"] = "delete-permanently",
                ["F9"] = "palette", ["Alt+9"] = "palette", ["F10"] = "quit", ["Alt+0"] = "quit", ["Ctrl+Q"] = "quit",
                ["Ctrl+O"] = "output", ["Ctrl+R"] = "refresh", ["Ctrl+S"] = "quick-search", ["Alt+F1"] = "left-root", ["Alt+F2"] = "right-root",
                ["Ctrl+\\"] = "swap-panels", ["Ctrl+U"] = "same-folder",
                ["Up"] = "up", ["Down"] = "down", ["PgUp"] = "page-up", ["PgDn"] = "page-down", ["Home"] = "first", ["End"] = "last"
            };
            foreach (KeyValuePair<string, string> pair in expected) {
                Assert.True(commands.Match(Key(pair.Key))?.Key == pair.Value, pair.Key + " should run " + pair.Value + " but runs " + commands.Match(Key(pair.Key))?.Key);
            }
            Assert.All(commands, command => Assert.False(string.IsNullOrWhiteSpace(command.Label)));
            Assert.Equal(Enumerable.Range(1, 10), commands.Where(command => command.FunctionKey != 0).Select(command => command.FunctionKey).OrderBy(n => n));
            Assert.Equal("F5", commands.Find("copy")!.Gesture.ToString());
            Assert.Equal(new[] { "Alt+5" }, commands.Find("copy")!.Alternatives.Select(chord => chord.ToString()));
            Assert.Empty(commands.Find("hidden")!.Keys);
        }

        [Fact]
        public void TheVtDecoder_DeliversCtrlBackslashToSwap() {
            List<InputEvent> events = new List<InputEvent>();
            new VtInputDecoder().Feed("\u001c", events);
            InputEvent input = Assert.Single(events);
            Assert.Equal("swap-panels", CommanderCommands.CreateDefault().Match(input)?.Key);
            Assert.Equal("Ctrl+\\", KeyChord.Parse("Ctrl+\\").ToString());
        }

        [Fact]
        public void Registry_RejectsDuplicateKeysChordsAndSlots() {
            Action<CommanderContext> nothing = _ => { };
            Assert.Throws<ArgumentException>(() => new CommanderCommands(new[] { new CommanderCommand("a", "A", nothing), new CommanderCommand("A", "B", nothing) }));
            Assert.Throws<ArgumentException>(() => new CommanderCommands(new[] { new CommanderCommand("a", "A", nothing, KeyChord.Parse("F5")), new CommanderCommand("b", "B", nothing, KeyChord.Parse("F5")) }));
            Assert.Throws<ArgumentException>(() => new CommanderCommands(new[] { new CommanderCommand("a", "A", nothing) { FunctionKey = 3 }, new CommanderCommand("b", "B", nothing) { FunctionKey = 3 } }));
            Assert.Throws<ArgumentException>(() => new CommanderCommands(new[] { new CommanderCommand("a", "A", nothing) { FunctionKey = 11 } }));
        }

        [Fact]
        public void KeyBar_HasTenSlotsAndDimsWhatCantRunNow() {
            var (temp, model, host, context) = Setup();
            using (temp) using (model) {
                CommanderCommands commands = CommanderCommands.CreateDefault();
                IReadOnlyList<CommanderKeyBarItem> bar = commands.KeyBar(context);
                Assert.Equal("1Help 2Menu 3View 4Edit 5Copy 6Move 7Mkdir 8Delete 9Palette 10Quit", string.Join(" ", bar.Select(item => item.ToString())));
                Assert.Equal("F5", bar[4].KeyText);
                // The cursor is on "..": nothing to view, copy or delete.
                Assert.False(bar[2].IsEnabled);
                Assert.False(bar[4].IsEnabled);
                Assert.True(bar[6].IsEnabled);
                model.Left.MoveTo(temp.Combine("left", "report.csv"));
                Assert.True(commands.KeyBar(context)[2].IsEnabled);
                host.Unsupported.Add("view");
                Assert.False(commands.KeyBar(context)[2].IsEnabled);
                Assert.Equal("Alt+0", commands.KeyBar(context, alternatives: true)[9].KeyText);
            }
        }

        [Fact]
        public void FileKeys_PlanOperationsForTheHost() {
            var (temp, model, host, context) = Setup();
            using (temp) using (model) {
                CommanderCommands commands = CommanderCommands.CreateDefault();
                Assert.True(commands.Dispatch(Key("F5"), context));
                Assert.Empty(host.Started);
                model.Left.MarkPattern("*.csv;*.md");
                commands.Dispatch(Key("F5"), context);
                FileOperation copy = Assert.Single(host.Started);
                Assert.Equal(FileOperationKind.Copy, copy.Kind);
                Assert.Equal(new[] { temp.Combine("right", "notes.md"), temp.Combine("right", "report.csv") }, copy.Targets);
                commands.Dispatch(Key("Alt+6"), context);
                Assert.Equal(FileOperationKind.Move, host.Started[1].Kind);
                commands.Dispatch(Key("Delete"), context);
                Assert.Equal(FileOperationKind.Delete, host.Started[2].Kind);
                commands.Dispatch(Key("Shift+F8"), context);
                Assert.Equal(FileOperationKind.DeletePermanently, host.Started[3].Kind);
                host.NextPrompt = "archive/2026";
                commands.Dispatch(Key("F7"), context);
                Assert.Equal("New folder", host.Prompts.Last());
                Assert.Equal(new[] { temp.Combine("left", "archive", "2026") }, host.Started[4].Sources);
            }
        }

        [Fact]
        public async Task CopyThenAfterOperation_RefreshesBothPanels() {
            var (temp, model, host, context) = Setup();
            using (temp) using (model) {
                model.Left.MoveTo(temp.Combine("left", "report.csv"));
                FileOperation copy = model.PlanCopy(new FileOperationOptions { ConflictPolicy = FileConflictPolicy.Skip })!;
                Assert.True((await copy.RunAsync()).Succeeded);
                model.AfterOperation(copy);
                Assert.NotNull(model.Right.Find(temp.Combine("right", "report.csv")));

                FileOperation folder = model.PlanCreateFolder("fresh");
                Assert.True((await folder.RunAsync()).Succeeded);
                model.AfterOperation(folder);
                Assert.Equal("fresh", model.Left.Current!.Name);

                FileOperation rename = model.PlanMove(destination: "renamed")!;
                Assert.True((await rename.RunAsync()).Succeeded);
                model.AfterOperation(rename);
                Assert.Equal("renamed", model.Left.Current!.Name);
            }
        }

        [Fact]
        public void PanelKeys_SwitchSwapSameFolderMarkAndNavigate() {
            var (temp, model, host, context) = Setup();
            using (temp) using (model) {
                CommanderCommands commands = CommanderCommands.CreateDefault();
                CommanderPanel left = model.Left;
                commands.Dispatch(Key("Down"), context);
                commands.Dispatch(Key("Down"), context);
                commands.Dispatch(Key("Insert"), context);
                Assert.Equal("build.ps1", Assert.Single(left.MarkedEntries).Name);
                commands.Dispatch(Key("*"), context);
                Assert.Equal(new[] { "notes.md", "report.csv" }, left.MarkedEntries.Select(entry => entry.Name));
                host.NextPrompt = "*.md";
                commands.Dispatch(Key("-"), context);
                Assert.Equal(1, left.MarkedCount);
                commands.Dispatch(Key("End"), context);
                Assert.Equal(left.Entries.Count - 1, left.CursorIndex);
                commands.Dispatch(Key("Home"), context);
                commands.Dispatch(Key("Down"), context);
                commands.Dispatch(Key("Enter"), context);
                Assert.Equal(temp.Combine("left", "docs"), left.Folder);
                commands.Dispatch(Key("Backspace"), context);
                Assert.Equal(temp.Combine("left"), left.Folder);
                Assert.Equal("docs", left.Current!.Name);

                commands.Dispatch(Key("Tab"), context);
                Assert.Same(model.Right, model.Active);
                commands.Dispatch(Key("Ctrl+U"), context);
                Assert.Equal(model.Right.Folder, model.Left.Folder);
                commands.Dispatch(Key("Tab"), context);
                model.Left.Navigate(temp.Combine("left"));
                commands.Dispatch(Key("Ctrl+\\"), context);
                Assert.Equal(temp.Combine("left"), model.Right.Folder);
                Assert.Same(left, model.Active);

                commands.Dispatch(Key("Ctrl+F6"), context);
                Assert.Equal(PanelSortColumn.Size, left.SortColumn);
                commands.Dispatch(Key("Ctrl+F6"), context);
                Assert.True(left.SortDescending);
                commands.Dispatch(Key("F10"), context);
                Assert.Equal(1, host.Quits);
            }
        }

        [Fact]
        public void TypingSearches_EscEnds_AndEnterOnAFileGoesToTheHost() {
            var (temp, model, host, context) = Setup();
            using (temp) using (model) {
                CommanderCommands commands = CommanderCommands.CreateDefault();
                Assert.True(commands.Dispatch(InputEvent.Character('n'), context));
                Assert.Equal("notes.md", model.Left.Current!.Name);
                commands.Dispatch(InputEvent.Character('o'), context);
                Assert.Equal("no", model.Left.QuickSearch);
                // While searching, + and space are typed rather than run.
                commands.Dispatch(InputEvent.Character('-'), context);
                Assert.Equal("no", model.Left.QuickSearch);
                Assert.Equal(0, model.Left.MarkedCount);
                commands.Dispatch(Key("Escape"), context);
                Assert.Equal(string.Empty, model.Left.QuickSearch);
                commands.Dispatch(Key("Enter"), context);
                Assert.Equal("notes.md", Assert.Single(host.Opened).Name);
                commands.Dispatch(InputEvent.Character('b'), context);
                commands.Dispatch(Key("Down"), context);
                Assert.Equal(string.Empty, model.Left.QuickSearch);
                Assert.False(commands.Dispatch(InputEvent.Paste("x"), context));
            }
        }

        [Fact]
        public void Bindings_FeedThePhase2KeyMapAndFooter() {
            var (temp, model, host, context) = Setup();
            using (temp) using (model) {
                IReadOnlyList<KeyBinding> bindings = CommanderCommands.CreateDefault().Bindings(context);
                KeyBinding copy = bindings.Single(binding => binding.Label == "Copy to the other panel");
                Assert.Equal("f5 / alt+5", copy.DisplayKeys());
                Assert.True(copy.InFooter);
                Assert.Equal("Files", copy.Group);
                KeyBinding tab = bindings.Single(binding => binding.Label == "Switch panel");
                Assert.False(tab.InFooter);
                tab.Action();
                Assert.Equal(CommanderSide.Right, model.ActiveSide);
                Assert.DoesNotContain(bindings, binding => binding.Label == "Show or hide hidden files");
            }
        }

        private sealed class FakeHost : ICommanderHost {
            internal HashSet<string> Unsupported { get; } = new HashSet<string>();
            internal List<FileOperation> Started { get; } = new List<FileOperation>();
            internal List<string> Prompts { get; } = new List<string>();
            internal List<PanelEntry> Opened { get; } = new List<PanelEntry>();
            internal string? NextPrompt { get; set; }
            internal int Quits { get; private set; }
            public bool Supports(string commandKey) => !Unsupported.Contains(commandKey);
            public void Prompt(string title, string text, Action<string> accepted) {
                Prompts.Add(title);
                if (NextPrompt != null) { accepted(NextPrompt); }
            }
            public void Start(FileOperation operation) => Started.Add(operation);
            public void Open(PanelEntry file) => Opened.Add(file);
            public void View(PanelEntry file) { }
            public void Edit(PanelEntry file) { }
            public void ShowHelp() { }
            public void ShowMenu() { }
            public void ShowPalette() { }
            public void ShowOutput() { }
            public void ChooseRoot(CommanderSide side) { }
            public void Quit() => Quits++;
        }
    }
}
