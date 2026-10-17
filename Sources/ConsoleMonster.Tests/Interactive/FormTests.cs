using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Interactive;
using ConsoleMonster.Tests.Rendering;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    /// <summary>
    /// Forms and wizards: pages, conditional and dependent fields, async loading with a spinner and cancellation,
    /// validation, the review page and its equivalent command, presets and remembered choices, and the non-interactive and
    /// line-based paths.
    /// </summary>
    public sealed class FormTests {
        private static Form Wizard(Func<FormValues, System.Threading.CancellationToken, Task<IEnumerable<Choice>>>? loader = null) {
            Form form = new Form("Install agent") { Command = "Install-Agent" };
            FormPage location = form.Page("Location", "Where the files go.");
            TextField path = location.Text("Path", "Install folder");
            path.Default = @"C:\Agent";
            path.Required = true;
            path.Parameter = "Path";
            path.Help = "Created when missing.";
            ChoiceField edition = location.Select("Edition", "Edition", new[] { new Choice("std", "Standard"), new Choice("pro", "Professional") });
            edition.Default = "std";
            edition.Parameter = "Edition";
            FormPage service = form.Page("Service");
            ToggleField custom = service.Toggle("Custom", "Custom account");
            custom.Parameter = "CustomAccount";
            TextField user = service.Text("User", "User name");
            user.VisibleWhen = v => v.Get<bool>("Custom");
            user.Required = true;
            user.Parameter = "UserName";
            SecretField password = service.Secret("Password", "Password");
            password.VisibleWhen = v => v.Get<bool>("Custom");
            password.Parameter = "Password";
            ChoiceField site = service.MultiSelect("Site", "Sites");
            site.Parameter = "Site";
            site.Loader = loader ?? ((values, token) => Task.FromResult<IEnumerable<Choice>>(new[] { new Choice("waw", "Warsaw"), new Choice("ber", "Berlin") }));
            return form;
        }

        private static RecordingSurface Show(Form form, string keys, out FormResult result, FormOptions? options = null, int width = 80, int height = 24) {
            RecordingSurface surface = new RecordingSurface(width, height, false);
            FormOptions o = options ?? new FormOptions();
            o.Interactive = PromptHarness.Options(keys, surface);
            result = PromptHarness.Guard(surface, () => form.Show(o));
            return surface;
        }

        [Fact]
        public void Show_WalkThroughTheWizard_ReturnsValuesAndTheEquivalentCommand() {
            RecordingSurface surface = Show(Wizard(), "Down Right PageDown Space Down \"svc\" Down \"pw\" Down Enter Space Enter PageDown Enter", out FormResult result);
            Assert.True(result.Submitted);
            Assert.Equal("pro", result.Values["Edition"]);
            Assert.Equal("svc", result.Values["User"]);
            Assert.Equal(new object?[] { "waw" }, result.Values.GetList<object>("Site").ToArray());
            Assert.Equal("Install-Agent -Edition pro -CustomAccount -UserName svc -Password <secret> -Site waw", result.CommandLine);
            Golden.Match("interactive/form.wizard.page1.txt", surface.Frames[0].ToPlainText());
            Golden.Match("interactive/form.wizard.service.txt", surface.Frames.First(f => f.ToPlainText().Contains("User name")).ToPlainText());
            Golden.Match("interactive/form.wizard.editor.txt", surface.Frames.First(f => f.ToPlainText().Contains("type to filter")).ToPlainText());
            Golden.Match("interactive/form.wizard.review.txt", surface.Frames.Last().ToPlainText());
        }

        [Fact]
        public void Show_RequiredFieldEmpty_BlocksTheNextPageWithAnInlineError() {
            Form form = Wizard();
            RecordingSurface surface = Show(form, "Ctrl+U PageDown \"D:\\\\x\" PageDown Escape Escape y", out FormResult result);
            Assert.False(result.Submitted);
            string blocked = surface.Frames.Select(f => f.ToPlainText()).First(f => f.Contains("A value is required."));
            Assert.Contains("Location", blocked);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("● Location ── ● Service"));
        }

        [Fact]
        public void Show_DependentFieldLoading_ShowsASpinnerAndEscapeCancels() {
            TaskCompletionSource<IEnumerable<Choice>> never = new TaskCompletionSource<IEnumerable<Choice>>();
            RecordingSurface surface = Show(Wizard((v, t) => never.Task), "PageDown Down Escape Ctrl+C", out FormResult result);
            Assert.False(result.Submitted);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Loading…"));
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("cancelled"));
        }

        [Fact]
        public void Show_ConditionalFields_AppearOnlyWhileTheirConditionHolds() {
            RecordingSurface surface = Show(Wizard(), "PageDown Space Space Ctrl+C", out _);
            string[] texts = surface.Frames.Select(f => f.ToPlainText()).ToArray();
            Assert.DoesNotContain("User name", texts[1]);
            Assert.Contains("User name", texts[2]);
            Assert.DoesNotContain("User name", texts[3]);
        }

        [Fact]
        public void Show_EscapeWithChanges_AsksBeforeDiscarding() {
            RecordingSurface surface = Show(Wizard(), "\"x\" Escape n Escape y", out FormResult result);
            Assert.False(result.Submitted);
            Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Discard unsaved changes and exit?"));
        }

        [Fact]
        public void BuildCommand_QuotesPerStyleAndLeavesDefaultsOut() {
            Form form = new Form("t") { Command = "Do-Thing" };
            TextField name = form.Text("Name");
            name.Parameter = "Name";
            name.Value = "two words";
            ToggleField force = form.Toggle("Force");
            force.Parameter = "Force";
            force.Value = true;
            ToggleField verify = form.Toggle("Verify");
            verify.Parameter = "Verify";
            verify.Default = true;
            verify.Value = false;
            NumberField port = form.Number("Port");
            port.Parameter = "Port";
            port.Default = 443m;
            ChoiceField servers = form.MultiSelect("Servers", null, new[] { new Choice("a"), new Choice("b c") });
            servers.Parameter = "Server";
            servers.Value = new List<object?> { "a", "b c" };
            Assert.Equal("Do-Thing -Name 'two words' -Force -Verify:$false -Server a,'b c'", form.BuildCommand());
            form.CommandStyle = CommandStyle.Posix;
            form.Command = "thing";
            Assert.Equal("thing --Name 'two words' --Force --no-Verify --Server a,'b c'", form.BuildCommand());
            form.CommandStyle = CommandStyle.Windows;
            Assert.Equal("thing /Name:\"two words\" /Force /no-Verify /Server:a,\"b c\"", form.BuildCommand());
        }

        [Fact]
        public void Wrap_BreaksBetweenArgumentsAndKeepsParametersWithValues() {
            IReadOnlyList<string> lines = CommandLine.Wrap("Install-Agent -Path 'C:\\Program Files\\Agent' -Edition pro -Site waw,ber", 32);
            Assert.Equal(new[] { "Install-Agent", "-Path 'C:\\Program Files\\Agent'", "-Edition pro -Site waw,ber" }, lines.ToArray());
        }

        [Fact]
        public void ChoiceStore_SavesPresetsAndRemembersChoices() {
            string root = Path.Combine(Path.GetTempPath(), "cm-store-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try {
                ChoiceStore store = new ChoiceStore("Test app", root);
                store.SavePreset("Install agent", "Weekly", new Dictionary<string, object?> { ["Path"] = @"D:\a ""b""", ["Site"] = new List<string> { "waw" }, ["Custom"] = true });
                Assert.Equal(new[] { "Weekly" }, store.PresetNames("Install agent").ToArray());
                IDictionary<string, object?> preset = store.LoadPreset("Install agent", "Weekly")!;
                Assert.Equal(@"D:\a ""b""", preset["Path"]);
                Assert.Equal(true, preset["Custom"]);
                Assert.True(store.DeletePreset("Install agent", "Weekly"));
                Assert.Empty(store.PresetNames("Install agent"));
                Form form = Wizard();
                RecordingSurface surface = Show(form, "Ctrl+U \"D:\\\\Remembered\" PageDown PageDown Enter", out FormResult first, new FormOptions { AppName = "Test app", StorageRoot = root, RememberLastChoices = true });
                Assert.True(first.Submitted);
                Form again = Wizard();
                Show(again, "PageDown PageDown Enter", out FormResult second, new FormOptions { AppName = "Test app", StorageRoot = root, RememberLastChoices = true });
                Assert.Equal(@"D:\Remembered", second.Values["Path"]);
                Assert.DoesNotContain("Password", File.ReadAllText(Path.Combine(root, "Test app", "last.json")));
                _ = surface;
            } finally {
                if (Directory.Exists(root)) {
                    Directory.Delete(root, true);
                }
            }
        }

        [Fact]
        public void Show_ReviewSavePreset_StoresTheValuesUnderTheName() {
            string root = Path.Combine(Path.GetTempPath(), "cm-preset-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try {
                RecordingSurface surface = Show(Wizard(), "PageDown PageDown s \"Nightly\" Enter Ctrl+C", out _, new FormOptions { AppName = "Preset app", StorageRoot = root });
                Assert.Contains(surface.Frames, f => f.ToPlainText().Contains("Preset 'Nightly' saved"));
                Assert.Equal(new[] { "Nightly" }, new ChoiceStore("Preset app", root).PresetNames("Install agent").ToArray());
            } finally {
                if (Directory.Exists(root)) {
                    Directory.Delete(root, true);
                }
            }
        }

        [Fact]
        public void MiniJson_RoundTripsNestedValues() {
            Dictionary<string, object?> value = new Dictionary<string, object?> { ["a"] = "x\n\"y\"", ["b"] = new List<object?> { "1", true, null }, ["c"] = new Dictionary<string, object?> { ["d"] = false } };
            Dictionary<string, object?> back = (Dictionary<string, object?>)MiniJson.Read(MiniJson.Write(value))!;
            Assert.Equal("x\n\"y\"", back["a"]);
            Assert.Equal(new object?[] { "1", true, null }, ((List<object?>)back["b"]!).ToArray());
            Assert.Equal(false, ((Dictionary<string, object?>)back["c"]!)["d"]);
            Assert.Throws<FormatException>(() => MiniJson.Read("{\"a\": }"));
        }

        [Fact]
        public void Show_NonInteractive_UsesDefaultsOrNamesTheMissingParameter() {
            Form form = Wizard();
            FormResult result = form.Show(new FormOptions { Interactive = new InteractiveOptions { Mode = InteractionMode.None } });
            Assert.True(result.Submitted);
            Assert.Equal(@"C:\Agent", result.Values["Path"]);
            Form missing = Wizard();
            ((TextField)missing.Find("Path")!).Default = null;
            missing.Find("Path")!.Value = null;
            PromptUnavailableException error = Assert.Throws<PromptUnavailableException>(() => missing.Show(new FormOptions { Interactive = new InteractiveOptions { Mode = InteractionMode.None } }));
            Assert.Equal("Prompt 'Install folder' needs a value in a non-interactive session; pass -Path or set a default.", error.Message);
        }

        [Fact]
        public void Show_LineMode_AsksEachVisibleFieldAndConfirms() {
            PromptModeTests.FakeLines lines = new PromptModeTests.FakeLines(string.Empty, "2", "y", "svc", "secret", "1,2", "y");
            Form form = Wizard();
            FormResult result = form.Show(new FormOptions { Interactive = new InteractiveOptions { Mode = InteractionMode.Accessible, LineConsole = lines, Labels = new PromptLabels() } });
            Assert.True(result.Submitted);
            Assert.Equal("pro", result.Values["Edition"]);
            Assert.Equal("svc", result.Values["User"]);
            Assert.Contains(lines.Output, l => l.StartsWith("Equivalent command: Install-Agent"));
        }
    }
}
