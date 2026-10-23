using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Threading;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Retains the original script and refuses invocation outside its creating runspace and active app input thread.</summary>
    internal sealed class PSAppScript {
        private readonly ScriptBlock _script;
        private static readonly ConditionalWeakTable<AppBinding, PSAppScript> Bindings = new ConditionalWeakTable<AppBinding, PSAppScript>();
        private int _thread;
        private readonly Runspace? _runspace = Runspace.DefaultRunspace;

        internal PSAppScript(ScriptBlock script) { _script = script; }

        /// <summary>Acquires thread ownership for one run, allowing creation in an earlier pipeline of the same runspace.</summary>
        internal Action Attach(PSCmdlet cmdlet) {
            if (!ReferenceEquals(Runspace.DefaultRunspace, _runspace)) {
                throw new InvalidOperationException(PSAppMessages.CreatingRunspace);
            }
            if (Interlocked.CompareExchange(ref _thread, Thread.CurrentThread.ManagedThreadId, 0) != 0) {
                throw new InvalidOperationException(PSAppMessages.AlreadyAttached);
            }
            Cmdlet = cmdlet;
            return () => {
                Cmdlet = null;
                Interlocked.Exchange(ref _thread, 0);
            };
        }

        /// <summary>The active runner receives formatting diagnostics instead of the completed factory command.</summary>
        internal PSCmdlet? Cmdlet { get; private set; }

        /// <summary>Associates a dependent widget with its script for scoped input-thread ownership.</summary>
        internal AppBinding CreateBinding(Func<AppState, IWidget> build, string? name = null) {
            AppBinding binding = new AppBinding(build, name);
            Bindings.Add(binding, this);
            return binding;
        }

        internal static PSAppScript? BindingScript(AppBinding binding) =>
            Bindings.TryGetValue(binding, out PSAppScript? script) ? script : null;

        /// <summary>Invokes with native state and commits changed keys through the public state API. Output stays local.</summary>
        internal Collection<PSObject> Invoke(AppState state, InputControl? control = null, object? value = null) {
            if (Thread.CurrentThread.ManagedThreadId != _thread || !ReferenceEquals(Runspace.DefaultRunspace, _runspace)) {
                throw new InvalidOperationException(PSAppMessages.ActiveInputThread);
            }
            Hashtable before = Snapshot(state);
            Hashtable values = (Hashtable)before.Clone();
            List<PSVariable> variables = new List<PSVariable> {
                new PSVariable("State", values), new PSVariable("Value", value), new PSVariable("Control", control)
            };
            Collection<PSObject> output = _script.InvokeWithContext(null, variables,
                control == null ? new object?[] { values } : new object?[] { value, values });
            // AppState has no removal API. State is a value map: add or assign keys to write back.
            foreach (DictionaryEntry entry in values) {
                if (!(entry.Key is string key)) {
                    throw new InvalidOperationException(PSAppMessages.StringStateKeys);
                }
                if (!before.ContainsKey(key) || !Equals(before[key], entry.Value)) {
                    state[key] = entry.Value;
                }
            }
            return output;
        }

        internal static Hashtable Snapshot(AppState state) {
            Hashtable values = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, object?> pair in state.Snapshot()) { values[pair.Key] = pair.Value; }
            return values;
        }
    }
}
