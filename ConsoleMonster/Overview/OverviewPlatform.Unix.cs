using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace ConsoleMonster.Overview {
    internal static partial class OverviewPlatform {
        /// <summary>Uses the marketed OS identity rather than a verbose kernel build string.</summary>
        internal static string OperatingSystem() {
            string os = RuntimeInformation.OSDescription;
            try {
                if (Mac) {
                    XDocument plist = XDocument.Load("/System/Library/CoreServices/SystemVersion.plist");
                    string version = string.Empty;
                    foreach (XElement key in plist.Descendants("key")) {
                        if (key.Value == "ProductVersion") { version = (key.NextNode as XElement)?.Value ?? string.Empty; }
                    }
                    if (version.Length > 0) { os = MacName(version); }
                } else if (Linux) {
                    os = LinuxName(File.ReadAllLines("/etc/os-release"));
                }
            } catch (Exception ex) when (OptionalFailure(ex) || ex is System.Xml.XmlException) { }
            return Windows ? os : os + " · " + RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
        }

        internal static string MacName(string version) {
            string major = version.Split('.')[0];
            string name = major == "26" ? "Tahoe" : major == "15" ? "Sequoia" : major == "14" ? "Sonoma"
                : major == "13" ? "Ventura" : major == "12" ? "Monterey" : major == "11" ? "Big Sur" : string.Empty;
            return "macOS " + version + (name.Length == 0 ? string.Empty : " (" + name + ")");
        }

        internal static string LinuxName(IEnumerable<string> lines) {
            string name = "Linux", version = string.Empty;
            foreach (string line in lines) {
                int separator = line.IndexOf('=');
                if (separator <= 0) { continue; }
                string key = line.Substring(0, separator);
                string value = line.Substring(separator + 1).Trim().Trim('"', '\'');
                if (key == "PRETTY_NAME") { return value; }
                if (key == "NAME") { name = value; }
                if (key == "VERSION_ID") { version = value; }
            }
            return name + (version.Length == 0 ? string.Empty : " " + version);
        }

        internal static bool VisibleVolume(string path, bool mac) {
            if (!mac) { return path == "/" || (!path.StartsWith("/proc", StringComparison.Ordinal) && !path.StartsWith("/sys", StringComparison.Ordinal) && !path.StartsWith("/dev", StringComparison.Ordinal) && !path.StartsWith("/run", StringComparison.Ordinal)); }
            if (path == "/") { return true; }
            if (path.IndexOf("CoreSimulator", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("Snapshot", StringComparison.OrdinalIgnoreCase) >= 0) { return false; }
            return path.StartsWith("/Volumes/", StringComparison.Ordinal) && path.Substring(9).IndexOf('/') < 0
                && !string.Equals(path, "/Volumes/Preboot", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(path, "/Volumes/VM", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(path, "/Volumes/Recovery", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool HiddenInterface(string name) => name.StartsWith("utun", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("awdl", StringComparison.OrdinalIgnoreCase) || name.StartsWith("llw", StringComparison.OrdinalIgnoreCase);

        private static bool HasGateway(NetworkInterface adapter) {
            try { return adapter.GetIPProperties().GatewayAddresses.Count > 0; }
            catch (Exception ex) when (OptionalFailure(ex) || ex is NetworkInformationException) { return false; }
        }
    }
}
