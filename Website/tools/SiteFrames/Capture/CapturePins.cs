using System;
using System.Collections.Generic;

namespace SiteFrames.Capture {
    /// <summary>Terminal-independent process pins. The policy participates in source fingerprints on every OS.</summary>
    internal static class CapturePins {
        internal const string Policy = "SessionRecorder/site-v3;VT-linefeeds;canonical-SGR;hosted-checkpoints;logical-50ms+holds;UTF8;en-US;UTC;now=2026-10-21T12:00:00Z;emoji=2;ambiguous=1;Release";
        internal static void Apply(IDictionary<string, string?> environment) {
            environment["TZ"] = "UTC";
            environment["LANG"] = OperatingSystem.IsMacOS() ? "en_US.UTF-8" : "C.UTF-8";
            environment["LC_ALL"] = environment["LANG"];
            environment["CONSOLEMONSTER_NOW"] = "2026-10-21T12:00:00Z";
            environment["CONSOLEMONSTER_EMOJI_WIDTH"] = "2";
            environment["CONSOLEMONSTER_AMBIGUOUS_WIDTH"] = "1";
            if (!environment.ContainsKey("CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION")) { environment["CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION"] = "Release"; }
            environment["MSBUILDDISABLENODEREUSE"] = "1";
            environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        }
    }
}
