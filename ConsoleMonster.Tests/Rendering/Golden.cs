using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

namespace ConsoleMonster.Tests.Rendering {
    /// <summary>
    /// Golden-file comparison. Set CONSOLEMONSTER_UPDATE_GOLDEN=1 to rewrite the files after an intentional visual change;
    /// review the diff of the golden files like any other code change.
    /// </summary>
    internal static class Golden {
        public static void Match(string name, string actual, [CallerFilePath] string callerPath = "") {
            string directory = Path.Combine(Path.GetDirectoryName(callerPath) ?? ".", "..", "Golden");
            string path = Path.GetFullPath(Path.Combine(directory, name));
            string normalized = actual.Replace("\r\n", "\n");
            if (Environment.GetEnvironmentVariable("CONSOLEMONSTER_UPDATE_GOLDEN") == "1" || !File.Exists(path)) {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, normalized, new UTF8Encoding(false));
                if (Environment.GetEnvironmentVariable("CONSOLEMONSTER_UPDATE_GOLDEN") != "1") {
                    Assert.Fail("Golden file " + name + " did not exist and was created; review it and run the tests again.");
                }
                return;
            }
            string expected = File.ReadAllText(path, Encoding.UTF8).Replace("\r\n", "\n");
            Assert.Equal(expected, normalized);
        }
    }
}
