using System;
using System.Linq;
using System.Runtime.InteropServices;
using ConsoleMonster.Examples.Showcases;
using ConsoleMonster.Interactive;
using Xunit;

namespace ConsoleMonster.Tests.Interactive {
    public sealed class ShowcasePlatformTests {
        [Fact]
        public void InstallerDefaultsFollowTheHostPlatform() {
            Form form = SetupWizard.Build();
            Field[] fields = form.Fields.ToArray();
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Field path = fields.Single(field => field.Name == "Path");
            Field data = fields.Single(field => field.Name == "Data");
            Assert.Equal(windows ? @"C:\Program Files\Toolbox Agent" : "/usr/local/lib/toolbox-agent", path.Default);
            Assert.Equal(windows ? @"C:\ProgramData\Toolbox Agent" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "/Library/Application Support/Toolbox Agent" : "/var/lib/toolbox-agent", data.Default);
            Assert.Equal(windows ? "Start menu shortcuts" : "Command-line shortcuts", fields.Single(field => field.Name == "Shortcuts").Label);
            Assert.Equal(windows ? "NetworkService" : "Dedicated", fields.Single(field => field.Name == "Account").Default);
        }
    }
}
