using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SiteFrames.Capture {
    internal static partial class PowerShellSyntax {
        /// <summary>Reads literal command and parameter names with the native AST, without executing a script.</summary>
        public static IReadOnlyList<HashSet<string>> Commands(IReadOnlyList<string> blocks) {
            string folder = Path.Combine(Path.GetTempPath(), "cmsite-commands-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                string input = Path.Combine(folder, "blocks.json");
                string script = Path.Combine(folder, "commands.ps1");
                File.WriteAllText(input, JsonSerializer.Serialize(blocks), new UTF8Encoding(false));
                File.Copy(Path.Combine(AppContext.BaseDirectory, "Capture", "Read-PowerShellCommands.ps1"), script);
                List<string> lines = Run(PowerShellHost.Core, script, input, blocks.Count, required: true)!;
                HashSet<string>[] result = blocks.Select(_ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)).ToArray();
                foreach (string line in lines) {
                    string[] parts = line.Split('\t', 2);
                    if (parts.Length != 2 || !int.TryParse(parts[0], out int index) || index < 0 || index >= blocks.Count) {
                        throw new InvalidDataException("Unexpected command AST output: " + line);
                    }
                    result[index].Add(parts[1]);
                }
                return result;
            } finally { Site.RemoveTemporary(folder); }
        }

    }
}
