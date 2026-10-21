using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;

namespace ConsoleMonsterStreamsFixtures {
    // Real compiled PSCmdlet: unlike a Write-Warning proxy, its warning goes directly through the engine.
    [Cmdlet(VerbsCommunications.Write, "StreamsFixtureWarning")]
    public sealed class WriteStreamsFixtureWarningCommand : PSCmdlet {
        [Parameter(Mandatory = true)]
        public string Message { get; set; }

        protected override void ProcessRecord() { WriteWarning(Message); }
    }

    // A public host boundary, not a production seam. Null UI is a supported headless-host condition.
    public sealed class StreamsHost : PSHost {
        private readonly PSObject data;
        private readonly bool unavailable;
        private readonly Guid id = Guid.NewGuid();

        public StreamsHost(PSObject data, bool unavailable) { this.data = data; this.unavailable = unavailable; }
        public override Guid InstanceId { get { return id; } }
        public override string Name { get { return "StreamsHeadlessHost"; } }
        public override Version Version { get { return new Version(1, 0); } }
        public override CultureInfo CurrentCulture { get { return CultureInfo.InvariantCulture; } }
        public override CultureInfo CurrentUICulture { get { return CultureInfo.InvariantCulture; } }
        public override PSHostUserInterface UI { get { return null; } }
        public override PSObject PrivateData {
            get {
                if (unavailable) { throw new NotSupportedException("PrivateData is unavailable in this host."); }
                return data;
            }
        }
        public override void EnterNestedPrompt() { throw new NotSupportedException(); }
        public override void ExitNestedPrompt() { throw new NotSupportedException(); }
        public override void NotifyBeginApplication() { }
        public override void NotifyEndApplication() { }
        public override void SetShouldExit(int exitCode) { }
    }

    // Exercises independently writable, rejected, missing and read-only host properties in one installation.
    public sealed class StreamsPrivateData {
        private ConsoleColor verbose = ConsoleColor.Magenta;
        public bool RejectVerbose { get; set; }
        public int VerboseAttempts { get; private set; }
        public int VerboseWrites { get; private set; }
        public ConsoleColor WarningForegroundColor { get; set; }
        public ConsoleColor WarningBackgroundColor { get; set; }
        public ConsoleColor DebugBackgroundColor { get; set; }
        public ConsoleColor ErrorForegroundColor { get; set; }
        public ConsoleColor DebugForegroundColor { get { return ConsoleColor.Blue; } }
        public ConsoleColor VerboseForegroundColor {
            get { return verbose; }
            set {
                VerboseAttempts++;
                if (RejectVerbose) { throw new NotSupportedException("The host rejects verbose colour writes."); }
                verbose = value;
                VerboseWrites++;
            }
        }
        public StreamsPrivateData() {
            RejectVerbose = true;
            WarningForegroundColor = ConsoleColor.Magenta;
            WarningBackgroundColor = ConsoleColor.DarkMagenta;
            DebugBackgroundColor = ConsoleColor.DarkBlue;
            ErrorForegroundColor = ConsoleColor.DarkGreen;
        }
    }
}
