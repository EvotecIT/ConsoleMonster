using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using ConsoleMonster.Styling;

namespace ConsoleMonster.Overview {
    internal static class OverviewWindowsSecurity {
        internal static OverviewSection? Collect(CancellationToken token) {
            // No audit-policy changes or privilege enabling. Standard users normally cannot open this channel.
            IntPtr failures = EvtQuery(IntPtr.Zero, "Security", "*[System[(EventID=4625) and TimeCreated[timediff(@SystemTime) <= 86400000]]]", 0x201);
            if (failures == IntPtr.Zero) { return null; }
            List<OverviewItem> items = new List<OverviewItem>();
            try {
                int count = 0;
                while (count < 1024 && Next(failures, token, out IntPtr entry)) {
                    EvtClose(entry);
                    count++;
                }
                items.Add(new OverviewItem("Failed logons (24 h)", count.ToString(CultureInfo.InvariantCulture) + (count == 1024 ? "+ (sample limit)" : string.Empty), null, count > 0 ? ThemeToken.Warn : ThemeToken.Text));
            } finally { EvtClose(failures); }
            IntPtr successes = EvtQuery(IntPtr.Zero, "Security", "*[System[EventID=4624] and EventData[Data[@Name='LogonType']='2' or Data[@Name='LogonType']='10' or Data[@Name='LogonType']='11']]", 0x201);
            if (successes != IntPtr.Zero) {
                try {
                    for (int i = 0; i < 3 && Next(successes, token, out IntPtr entry); i++) {
                        try { AddLogin(items, entry); } finally { EvtClose(entry); }
                    }
                } finally { EvtClose(successes); }
            }
            return new OverviewSection("Security", "Security", items);
        }

        private static bool Next(IntPtr query, CancellationToken token, out IntPtr entry) {
            token.ThrowIfCancellationRequested();
            IntPtr[] entries = new IntPtr[1];
            bool success = EvtNext(query, 1, entries, 0, 0, out uint count);
            if (!success && Marshal.GetLastWin32Error() != 259) { throw new IOException("Security events are not available."); }
            bool result = success && count == 1;
            entry = entries[0];
            return result;
        }

        private static void AddLogin(List<OverviewItem> items, IntPtr entry) {
            EvtRender(IntPtr.Zero, entry, 1, 0, IntPtr.Zero, out uint size, out uint _);
            if (size == 0 || size > 65536) { return; }
            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try {
                if (!EvtRender(IntPtr.Zero, entry, 1, size, buffer, out uint _, out uint _)) { return; }
                string xml = Marshal.PtrToStringUni(buffer) ?? string.Empty;
                using (StringReader source = new StringReader(xml))
                using (XmlReader reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null })) {
                    XDocument document = XDocument.Load(reader);
                    XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
                    string? user = document.Descendants(ns + "Data").FirstOrDefault(node => (string?)node.Attribute("Name") == "TargetUserName")?.Value;
                    string? timestamp = (string?)document.Descendants(ns + "TimeCreated").FirstOrDefault()?.Attribute("SystemTime");
                    if (!string.IsNullOrWhiteSpace(user) && DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset time)) {
                        items.Add(new OverviewItem(time.ToLocalTime().ToString("MM-dd HH:mm", CultureInfo.InvariantCulture), user!));
                    }
                }
            } finally { Marshal.FreeHGlobal(buffer); }
        }

        [DllImport("wevtapi.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr EvtQuery(IntPtr session, string path, string query, uint flags);
        [DllImport("wevtapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvtNext(IntPtr query, uint size, [Out] IntPtr[] events, uint timeout, uint flags, out uint returned);
        [DllImport("wevtapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvtRender(IntPtr context, IntPtr entry, uint flags, uint bytes, IntPtr buffer, out uint used, out uint properties);
        [DllImport("wevtapi.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EvtClose(IntPtr handle);
    }
}
