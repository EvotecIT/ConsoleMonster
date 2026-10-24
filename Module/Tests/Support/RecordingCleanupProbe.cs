using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// Exercise the command's terminal lifetime without risking the test runner's own terminal.
public static class RecordingCleanupProbe {
    public static void Validate(Type command) {
        MethodInfo finish = command.GetMethod("RestoreAndFinish", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (string failure in new[] { "input", "recorder", "output", "none" }) {
            var calls = new List<string>();
            bool restored = false;
            Action<string> step = name => {
                calls.Add(name);
                if (name == "input") restored = true;
                else if (!restored) throw new InvalidOperationException("The physical terminal was still raw during finalization.");
                if (failure == name) throw new IOException("Synthetic " + name + " failure");
            };
            try {
                finish.Invoke(null, new object[] {
                    new Cleanup(() => step("input")), (Action)(() => step("recorder")),
                    (Action)(() => step("output")), (Action)(() => step("title"))
                });
                if (failure != "none") throw new InvalidOperationException("Cleanup swallowed its failure.");
            } catch (TargetInvocationException error) {
                if (!(error.InnerException is IOException) || failure == "none") throw;
            }
            if (string.Join(",", calls) != "input,recorder,output,title") {
                throw new InvalidOperationException("A failed cleanup skipped another resource: " + string.Join(",", calls));
            }
        }
    }

    private sealed class Cleanup : IDisposable {
        private readonly Action dispose;
        internal Cleanup(Action dispose) { this.dispose = dispose; }
        public void Dispose() { dispose(); }
    }
}
