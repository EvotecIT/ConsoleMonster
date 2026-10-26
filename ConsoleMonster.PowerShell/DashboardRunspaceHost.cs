using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Runs dashboard scripts in runspaces the module owns. Each runspace gets the module's cmdlets and a
    /// <c>$ConsoleSession</c> variable, and its Information/Warning/Verbose/Debug/Error streams are routed into the session's
    /// log, so script output never writes over the live screen. Parallel items are submitted as they arrive with
    /// backpressure: the producer waits while the number of running items is at the throttle limit, so neither memory nor
    /// startup time grows with the size of the input. Results and errors are collected for the caller.
    /// </summary>
    internal sealed class DashboardRunspaceHost : IDisposable {
        private readonly LiveSession _session;
        private readonly InitialSessionState _state;
        private readonly List<Invocation> _running = new List<Invocation>();
        private RunspacePool? _pool;
        private int _throttle = 1;

        public DashboardRunspaceHost(LiveSession session, string[]? modules, System.Collections.IDictionary? usings = null, IDictionary<string, string>? functions = null) {
            _session = session;
            _state = InitialSessionState.CreateDefault2();
            foreach (Type type in typeof(DashboardRunspaceHost).Assembly.GetTypes()) {
                object[] attributes = type.GetCustomAttributes(typeof(CmdletAttribute), false);
                if (attributes.Length == 1 && !type.IsAbstract) {
                    CmdletAttribute cmdlet = (CmdletAttribute)attributes[0];
                    _state.Commands.Add(new SessionStateCmdletEntry(cmdlet.VerbName + "-" + cmdlet.NounName, type, null));
                }
            }
            _state.Variables.Add(new SessionStateVariableEntry("ConsoleSession", session, "The running ConsoleMonster live session."));
            if (usings != null) {
                foreach (System.Collections.DictionaryEntry entry in usings) { _state.Variables.Add(new SessionStateVariableEntry((string)entry.Key, entry.Value, "A $using: value from the caller.")); }
            }
            if (functions != null) {
                foreach (KeyValuePair<string, string> function in functions) { _state.Commands.Add(new SessionStateFunctionEntry(function.Key, function.Value)); }
            }
            if (modules != null && modules.Length > 0) {
                _state.ImportPSModule(modules);
            }
        }

        public List<PSObject> Output { get; } = new List<PSObject>();

        public List<ErrorRecord> Errors { get; } = new List<ErrorRecord>();

        public Exception? Failure { get; private set; }

        /// <summary>Runs one script with arguments; returns when it completes, fails or is cancelled.</summary>
        public void Run(string script, object[]? arguments, Func<bool> cancelled) {
            using (Runspace runspace = RunspaceFactory.CreateRunspace(_state)) {
                runspace.Open();
                System.Management.Automation.PowerShell shell = System.Management.Automation.PowerShell.Create();
                shell.Runspace = runspace;
                shell.AddScript(script);
                AddArguments(shell, arguments);
                Invocation invocation = new Invocation(shell, new PowerShellStreamCapture(shell, _session));
                invocation.Async = shell.BeginInvoke();
                Complete(invocation, cancelled);
            }
        }

        /// <summary>Opens the runspace pool used by <see cref="Submit"/>.</summary>
        public void OpenPool(int throttle) {
            _throttle = Math.Max(1, throttle);
            _pool = RunspaceFactory.CreateRunspacePool(_state);
            _pool.SetMaxRunspaces(_throttle);
            _pool.SetMinRunspaces(1);
            _pool.Open();
        }

        /// <summary>Starts the script for one item, first waiting while the throttle limit is reached.</summary>
        public void Submit(string script, object item, object[]? arguments, Func<bool> cancelled) {
            if (_pool == null) {
                throw new InvalidOperationException("OpenPool must be called before Submit.");
            }
            while (_running.Count >= _throttle && !cancelled()) {
                CollectFinished(cancelled, true);
            }
            if (cancelled()) {
                return;
            }
            System.Management.Automation.PowerShell shell = System.Management.Automation.PowerShell.Create();
            shell.RunspacePool = _pool;
            shell.AddScript(script).AddArgument(item);
            AddArguments(shell, arguments);
            Invocation invocation = new Invocation(shell, new PowerShellStreamCapture(shell, _session, isolateProgress: true));
            invocation.Async = shell.BeginInvoke();
            _running.Add(invocation);
            CollectFinished(cancelled, false);
        }

        /// <summary>Waits for every submitted item.</summary>
        public void WaitAll(Func<bool> cancelled) {
            while (_running.Count > 0) {
                CollectFinished(cancelled, true);
            }
        }

        public void Dispose() {
            foreach (Invocation invocation in _running) {
                invocation.Dispose();
            }
            _running.Clear();
            _pool?.Dispose();
        }

        private void CollectFinished(Func<bool> cancelled, bool wait) {
            if (wait && _running.Count > 0) {
                WaitHandle[] handles = new WaitHandle[Math.Min(64, _running.Count)];
                for (int i = 0; i < handles.Length; i++) {
                    handles[i] = _running[i].Async!.AsyncWaitHandle;
                }
                WaitHandle.WaitAny(handles, 50);
            }
            for (int i = _running.Count - 1; i >= 0; i--) {
                Invocation invocation = _running[i];
                if (invocation.Async!.IsCompleted || cancelled()) {
                    _running.RemoveAt(i);
                    Complete(invocation, cancelled);
                }
            }
        }

        private void Complete(Invocation invocation, Func<bool> cancelled) {
            try {
                while (!invocation.Async!.AsyncWaitHandle.WaitOne(50)) {
                    if (cancelled()) {
                        invocation.Shell.Stop();
                        break;
                    }
                }
                foreach (PSObject result in invocation.Shell.EndInvoke(invocation.Async)) {
                    Output.Add(result);
                }
            } catch (PipelineStoppedException) {
            } catch (RuntimeException exception) {
                Failure ??= exception;
                _session.Post(new LiveEvent("$log", "write", text: exception.Message, status: "err"));
            } finally {
                Errors.AddRange(invocation.Shell.Streams.Error);
                invocation.Dispose();
            }
        }

        private static void AddArguments(System.Management.Automation.PowerShell shell, object[]? arguments) {
            if (arguments == null) {
                return;
            }
            foreach (object argument in arguments) {
                shell.AddArgument(argument);
            }
        }

        private sealed class Invocation : IDisposable {
            public Invocation(System.Management.Automation.PowerShell shell, PowerShellStreamCapture capture) {
                Shell = shell;
                Capture = capture;
            }

            public System.Management.Automation.PowerShell Shell { get; }

            public PowerShellStreamCapture Capture { get; }

            public IAsyncResult? Async { get; set; }

            public void Dispose() {
                Capture.Dispose();
                Shell.Dispose();
            }
        }
    }
}
