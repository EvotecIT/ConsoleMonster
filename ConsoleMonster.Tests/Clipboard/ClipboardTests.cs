using System;
using System.Collections.Generic;
using ConsoleMonster.Clipboard;
using Xunit;

namespace ConsoleMonster.Tests.Clipboard {
    /// <summary>Provider discovery and failures are tested at OS boundaries without writing to any real clipboard.</summary>
    public sealed class ClipboardTests {
        [Theory]
        [InlineData("MacOS", "pbcopy", "", null, null)]
        [InlineData("Linux", "wl-copy", "--type text/plain;charset=utf-8", "wayland-0", null)]
        [InlineData("Linux", "xclip", "-selection clipboard -in", null, ":0")]
        public void AvailableProviderReceivesUnicodeDocumentOverStdin(string os, string command, string arguments, string? wayland, string? display) {
            FakePlatform platform = new FakePlatform((ClipboardOS)Enum.Parse(typeof(ClipboardOS), os));
            platform.Variables["WAYLAND_DISPLAY"] = wayland;
            platform.Variables["DISPLAY"] = display;
            string executable = "/mock tools/" + command;
            platform.Executables[command] = executable;
            string document = "日本 zażółć <b>text</b>\n```text\n$(do-not-execute);'\"";
            Assert.True(new ClipboardService(platform).TryCopy(document, out string? error));
            Assert.Null(error);
            Assert.Equal(executable, platform.Launched);
            Assert.Equal(arguments, platform.Arguments);
            Assert.Equal(document, platform.Input);
        }

        [Fact]
        public void X11IsUsedWhenWaylandProviderIsMissingInMixedDesktop() {
            FakePlatform platform = new FakePlatform(ClipboardOS.Linux);
            platform.Variables["WAYLAND_DISPLAY"] = "wayland-1";
            platform.Variables["DISPLAY"] = ":0";
            platform.Executables["xclip"] = "/mock/xclip";
            Assert.True(new ClipboardService(platform).TryCopy("data", out _));
            Assert.Equal("/mock/xclip", platform.Launched);
        }

        [Theory]
        [InlineData("SSH_CONNECTION")]
        [InlineData("SSH_CLIENT")]
        [InlineData("SSH_TTY")]
        public void SshNeverTouchesEvenAnAvailableLocalClipboard(string variable) {
            FakePlatform platform = new FakePlatform(ClipboardOS.Windows);
            platform.Variables[variable] = "remote";
            Assert.False(new ClipboardService(platform).TryCopy("private", out string? error));
            Assert.Contains("SSH", error!);
            Assert.False(platform.WindowsCalled);
            Assert.Null(platform.Launched);
            Assert.Empty(platform.Lookups);
        }

        [Fact]
        public void SshUsesOsc52WithoutTouchingTheRemoteClipboard() {
            FakePlatform platform = new FakePlatform(ClipboardOS.MacOS);
            platform.Variables["SSH_TTY"] = "/dev/ttys001";
            string? sequence = null;
            Assert.True(new ClipboardService(platform).TryCopy("zażółć 日本", out string? error, value => sequence = value));
            Assert.Null(error);
            Assert.Equal("\u001b]52;c;" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("zażółć 日本")) + "\u0007", sequence);
            Assert.Empty(platform.Lookups);
            Assert.Null(platform.Launched);
        }

        [Fact]
        public void LocalProviderDoesNotUseOsc52() {
            FakePlatform platform = new FakePlatform(ClipboardOS.MacOS);
            platform.Executables["pbcopy"] = "/mock/pbcopy";
            Assert.True(new ClipboardService(platform).TryCopy("command", out _, _ => throw new InvalidOperationException("OSC 52 is reserved for SSH.")));
            Assert.Equal("/mock/pbcopy", platform.Launched);
        }

        [Fact]
        public void HeadlessLinuxDoesNotLaunchProviders() {
            FakePlatform platform = new FakePlatform(ClipboardOS.Linux);
            platform.Executables["xclip"] = "/mock/xclip";
            Assert.False(new ClipboardService(platform).TryCopy("data", out string? error));
            Assert.Contains("display", error!);
            Assert.Empty(platform.Lookups);
        }

        [Theory]
        [InlineData("MacOS", null, "pbcopy")]
        [InlineData("Linux", "DISPLAY", "xclip")]
        [InlineData("Linux", "WAYLAND_DISPLAY", "wl-copy")]
        public void MissingExecutableGivesAnActionableFailure(string os, string? displayVariable, string provider) {
            FakePlatform platform = new FakePlatform((ClipboardOS)Enum.Parse(typeof(ClipboardOS), os));
            if (displayVariable != null) {
                platform.Variables[displayVariable] = "desktop";
            }
            Assert.False(new ClipboardService(platform).TryCopy("data", out string? error));
            Assert.Contains(provider, error!);
            Assert.Null(platform.Launched);
        }

        [Fact]
        public void ProviderFailureIsNotReportedAsCopiedAndDoesNotFallBackAfterLaunch() {
            FakePlatform platform = new FakePlatform(ClipboardOS.Linux) { Failure = "provider exit code 23" };
            platform.Variables["WAYLAND_DISPLAY"] = "desktop";
            platform.Variables["DISPLAY"] = ":0";
            platform.Executables["wl-copy"] = "/mock/wl-copy";
            platform.Executables["xclip"] = "/mock/xclip";
            Assert.False(new ClipboardService(platform).TryCopy("data", out string? error));
            Assert.Equal("provider exit code 23", error);
            Assert.Equal(new[] { "wl-copy" }, platform.Lookups);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("busy")]
        public void WindowsResultReflectsNativeBoundary(string? failure) {
            FakePlatform platform = new FakePlatform(ClipboardOS.Windows) { Failure = failure };
            Assert.Equal(failure == null, new ClipboardService(platform).TryCopy("data", out string? error));
            Assert.Equal(failure, error);
            Assert.True(platform.WindowsCalled);
            Assert.Equal("data", platform.Input);
            Assert.Empty(platform.Lookups);
        }

        private sealed class FakePlatform : IClipboardPlatform {
            internal FakePlatform(ClipboardOS os) { OS = os; }
            public ClipboardOS OS { get; }
            internal Dictionary<string, string?> Variables { get; } = new Dictionary<string, string?>();
            internal Dictionary<string, string> Executables { get; } = new Dictionary<string, string>();
            internal List<string> Lookups { get; } = new List<string>();
            internal string? Launched { get; private set; }
            internal string? Arguments { get; private set; }
            internal string? Input { get; private set; }
            internal string? Failure { get; set; }
            internal bool WindowsCalled { get; private set; }
            public string? Environment(string name) => Variables.TryGetValue(name, out string? value) ? value : null;
            public string? FindExecutable(string name) {
                Lookups.Add(name);
                return Executables.TryGetValue(name, out string? value) ? value : null;
            }
            public bool Run(string executable, string arguments, string text, out string? error) {
                Launched = executable;
                Arguments = arguments;
                Input = text;
                error = Failure;
                return error == null;
            }
            public bool CopyWindows(string text, out string? error) {
                WindowsCalled = true;
                Input = text;
                error = Failure;
                return error == null;
            }
        }
    }
}
