using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;

namespace ConsoleMonster.PowerShell {
    /// <summary>Weak control registrations retain closures without a process-wide active app or cross-run event handlers.</summary>
    internal sealed class PSAppCallbacks : IDisposable {
        private static readonly ConditionalWeakTable<InputControl, Registration> Registrations = new ConditionalWeakTable<InputControl, Registration>();
        private readonly PSCmdlet _owner;
        private readonly AppState _state;
        private readonly ConcurrentQueue<Action<AppState>> _pending = new ConcurrentQueue<Action<AppState>>();
        private readonly List<Action> _detach = new List<Action>();

        internal PSAppCallbacks(PSCmdlet owner, AppState state) { _owner = owner; _state = state; }

        internal static void Register(InputControl control, ScriptBlock? change, ScriptBlock? click = null) {
            if (change != null || click != null) {
                Registrations.Add(control, new Registration(change, click));
            }
        }

        /// <summary>Key binding scripts run synchronously on the input thread, where keys are handled.</summary>
        internal void AttachKeys(IEnumerable<KeyBinding> bindings) {
            foreach (KeyBinding binding in bindings) {
                if (PSKeyBindings.Action(binding) is PSKeyAction action) {
                    _detach.Add(action.Script.Attach(_owner));
                    action.State = _state;
                    _detach.Add(() => action.State = null);
                }
            }
        }

        internal void Attach(IWidget widget) {
            if (widget is AppScreens screens) {
                foreach (AppScreen screen in screens.Screens) {
                    if (PSScreens.TitleScript(screen) is PSAppScript title) { _detach.Add(title.Attach(_owner)); }
                    AttachKeys(screen.Bindings);
                }
            }
            if (widget is InputControl input) { AttachKeys(input.Bindings); }
            if (widget is AppBinding binding) {
                PSAppScript? script = PSAppScript.BindingScript(binding);
                if (script != null) { _detach.Add(script.Attach(_owner)); }
                return;
            }
            if (widget is InputControl control && Registrations.TryGetValue(control, out Registration? registration) && registration != null) {
                if (registration.Change != null) {
                    PSAppScript script = registration.Change;
                    _detach.Add(script.Attach(_owner));
                    EventHandler<AppValueChangedEventArgs> handler = (_, change) =>
                        _pending.Enqueue(state => { script.Invoke(state, control, change.Value); });
                    control.Changed += handler;
                    _detach.Add(() => control.Changed -= handler);
                }
                if (control is Button button && registration.Click != null) {
                    PSAppScript script = registration.Click;
                    _detach.Add(script.Attach(_owner));
                    EventHandler handler = (_, __) => {
                        object value = button.Value;
                        _pending.Enqueue(state => { script.Invoke(state, button, value); });
                    };
                    button.Clicked += handler;
                    _detach.Add(() => button.Clicked -= handler);
                }
            }
            if (widget is IContainer container) {
                foreach (IWidget child in container.Children) { Attach(child); }
            }
        }

        /// <summary>Called only by ConsoleApp.ProcessEvents, synchronously on the owning input thread.</summary>
        internal void Drain(AppState state) {
            while (_pending.TryDequeue(out Action<AppState>? callback)) { callback(state); }
        }

        public void Dispose() {
            for (int index = _detach.Count - 1; index >= 0; index--) { _detach[index](); }
            _detach.Clear();
            while (_pending.TryDequeue(out _)) { }
        }

        private sealed class Registration {
            internal Registration(ScriptBlock? change, ScriptBlock? click) {
                Change = change == null ? null : new PSAppScript(change);
                Click = click == null ? null : new PSAppScript(click);
            }
            internal PSAppScript? Change { get; }
            internal PSAppScript? Click { get; }
        }
    }
}
