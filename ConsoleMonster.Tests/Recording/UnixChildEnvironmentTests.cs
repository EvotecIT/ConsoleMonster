using System.Collections;
using ConsoleMonster.Sessions;
using Xunit;

namespace ConsoleMonster.Tests.Sessions {
    public sealed class UnixChildEnvironmentTests {
        [Theory]
        [InlineData(true, "en_US.UTF-8", "/bin/zsh")]
        [InlineData(false, "C.UTF-8", "/bin/sh")]
        public void MissingLocaleAndShellUsePlatformDefaults(bool mac, string locale, string shell) {
            var environment = UnixChildEnvironment.Build(new SessionRecordingOptions(), new Hashtable { ["LANG"] = "" }, mac);
            Assert.Contains("LANG=" + locale, environment);
            Assert.Contains("LC_CTYPE=" + locale, environment);
            Assert.Contains("TERM=xterm-256color", environment);
            Assert.Equal(shell, UnixChildEnvironment.DefaultShell(null, mac));
            Assert.Equal(shell, UnixChildEnvironment.DefaultShell(" ", mac));
            Assert.Equal("/custom/shell", UnixChildEnvironment.DefaultShell("/custom/shell", mac));
        }

        [Fact]
        public void InheritedLocaleAndExplicitOverridesArePreserved() {
            var options = new SessionRecordingOptions();
            options.Environment["LC_CTYPE"] = "C.UTF-8";
            options.Environment["TERM"] = "screen";
            options.Environment["REMOVE"] = null;
            options.Environment["LANG"] = null;
            var environment = UnixChildEnvironment.Build(options, new Hashtable {
                ["LANG"] = "pl_PL.UTF-8", ["LC_CTYPE"] = "en_GB.UTF-8", ["REMOVE"] = "value", ["KEEP"] = "value"
            }, true);
            Assert.Contains("LC_CTYPE=C.UTF-8", environment);
            Assert.Contains("TERM=screen", environment);
            Assert.Contains("KEEP=value", environment);
            Assert.DoesNotContain(environment, value => value.StartsWith("LANG=") || value.StartsWith("REMOVE="));
            Assert.Contains("LANG=pl_PL.UTF-8", UnixChildEnvironment.Build(new SessionRecordingOptions(), new Hashtable { ["LANG"] = "pl_PL.UTF-8" }, true));
        }
    }
}
