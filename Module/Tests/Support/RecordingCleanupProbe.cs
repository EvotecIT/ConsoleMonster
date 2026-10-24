using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

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

    public static void ValidateDemo(Type command) {
        MethodInfo finish = command.GetMethod("FinishDemo", BindingFlags.Static | BindingFlags.NonPublic);
        foreach (string failure in new[] { "task", "title", "recording", "none" }) {
            var calls = new List<string>();
            Action<string> step = name => {
                calls.Add(name);
                if (failure == name) throw new IOException("Synthetic " + name + " failure");
            };
            var run = failure == "task" ? Task.FromException<string>(new IOException("Synthetic task failure")) : Task.FromResult("done");
            try {
                finish.Invoke(null, new object[] {
                    run, (Action)(() => step("title")), (Action)(() => step("recording")), new Cleanup(() => step("stop"))
                });
                if (failure != "none") throw new InvalidOperationException("Cleanup swallowed its failure.");
            } catch (TargetInvocationException error) {
                if (!(error.InnerException is IOException) || failure == "none") throw;
            }
            if (string.Join(",", calls) != "title,recording,stop") throw new InvalidOperationException("Demo cleanup skipped a resource: " + string.Join(",", calls));
        }
    }

    private sealed class Cleanup : IDisposable {
        private readonly Action dispose;
        internal Cleanup(Action dispose) { this.dispose = dispose; }
        public void Dispose() { dispose(); }
    }
}
