using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleMonster.Sessions {
    /// <summary>Resolves the Unix shell and UTF-8 environment without changing the recording host.</summary>
    internal static class UnixChildEnvironment {
        internal static string DefaultShell(string? shell, bool mac) => string.IsNullOrWhiteSpace(shell) ? (mac ? "/bin/zsh" : "/bin/sh") : shell!;

        internal static List<string> Build(SessionRecordingOptions options, IDictionary inherited, bool mac) {
            Dictionary<string, string?> variables = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in inherited) { variables[(string)entry.Key] = (string?)entry.Value; }
            variables["TERM"] = "xterm-256color";
            foreach (string name in new[] { "LANG", "LC_CTYPE" }) {
                if (!variables.TryGetValue(name, out string? value) || string.IsNullOrWhiteSpace(value)) { variables[name] = mac ? "en_US.UTF-8" : "C.UTF-8"; }
            }
            foreach (KeyValuePair<string, string?> entry in options.Environment) { variables[entry.Key] = entry.Value; }
            List<string> result = new List<string>();
            foreach (KeyValuePair<string, string?> entry in variables) { if (entry.Value != null) { result.Add(entry.Key + "=" + entry.Value); } }
            return result;
        }
    }
}
