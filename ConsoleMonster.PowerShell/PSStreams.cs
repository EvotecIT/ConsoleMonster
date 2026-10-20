using System;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell {
    /// <summary>Helpers for records collected from merged streams (<c>*&gt;&amp;1</c>).</summary>
    internal static class PSStreams {
        /// <summary>
        /// Whether an error record is a line a native program wrote to stderr (merged with 2&gt;&amp;1), which many tools use
        /// for progress and notes rather than failures.
        /// </summary>
        internal static bool IsNativeErrorLine(ErrorRecord record) {
            return record.Exception is RemoteException
                && record.FullyQualifiedErrorId != null
                && record.FullyQualifiedErrorId.StartsWith("NativeCommandError", StringComparison.Ordinal);
        }
    }
}
