using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

// Exercise the real internal relay without running PowerShell script blocks on its producer thread.
public static class RecordingRelayProbe {
    public static void Validate(Type relayType) {
        Check(relayType, false);
        Check(relayType, true);
    }
    private static void Check(Type relayType, bool cancel) {
        object relay = Activator.CreateInstance(relayType, true);
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo receive = relayType.GetMethod("Receive", flags);
        MethodInfo drain = relayType.GetMethod("Drain", flags);
        MethodInfo stop = relayType.GetMethod("Stop", flags);
        int produced = 0;
        Task producer = Task.Run(() => {
            for (int i = 0; i < 512; i++) { receive.Invoke(relay, new object[] { "Output " + i }); Interlocked.Increment(ref produced); }
        });
        try {
            if (!SpinWait.SpinUntil(() => Volatile.Read(ref produced) >= 64 || producer.IsCompleted, TimeSpan.FromSeconds(5))) { throw new Exception("Producer never reached the relay capacity."); }
            if (producer.Wait(200)) { throw new Exception("The slow consumer did not backpressure the producer."); }
            if (cancel) {
                stop.Invoke(relay, null);
                if (!producer.Wait(5000)) { throw new Exception("Cancellation did not release the blocked producer."); }
                return;
            }
            List<string> output = new List<string>();
            Action<string> sink = value => output.Add(value);
            drain.Invoke(relay, new object[] { sink });
            if (output.Count == 0 || output.Count >= 64) { throw new Exception("A drain pass did not yield to the pipeline."); }
            Stopwatch deadline = Stopwatch.StartNew();
            while (!producer.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(5)) { drain.Invoke(relay, new object[] { sink }); }
            if (!producer.Wait(1000)) { throw new Exception("A draining consumer did not release backpressure."); }
            while ((bool)drain.Invoke(relay, new object[] { sink })) { }
            if (output.Count != 512) { throw new Exception("The relay lost output at child completion."); }
            for (int i = 0; i < output.Count; i++) { if (output[i] != "Output " + i) { throw new Exception("The relay changed output order."); } }
        } finally {
            stop.Invoke(relay, null);
            if (!producer.Wait(5000)) { throw new Exception("The owned relay producer did not finish."); }
            ((IDisposable)relay).Dispose();
        }
    }
}
