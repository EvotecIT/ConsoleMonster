using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Shared plumbing of the interactive cmdlets: the interaction mode for the current host (console, remoting, ISE,
    /// -NonInteractive), choices built from pipeline objects, scriptblock adapters, and loaders that run in their own
    /// runspace so they never block the screen.
    /// </summary>
    internal static class PSInteractive {
        /// <summary>The options for a prompt asked by a cmdlet in its host.</summary>
        public static InteractiveOptions Options(PSCmdlet cmdlet, InteractionMode? mode) {
            InteractiveOptions options = InteractiveOptions.Default.Clone();
            if (mode.HasValue) {
                options.Mode = mode.Value;
                if (mode.Value == InteractionMode.Line || mode.Value == InteractionMode.Accessible) {
                    options.LineConsole = HostConsole(cmdlet);
                }
                return options;
            }
            if (IsNonInteractiveSession()) {
                options.Mode = InteractionMode.None;
                return options;
            }
            LiveSession? live = LiveSession.Current;
            if (live != null && live.IsRunning) {
                return options;
            }
            string host = cmdlet.Host?.Name ?? string.Empty;
            if (host == "ConsoleHost" || host == "Default Host" || host.Length == 0) {
                return options;
            }
            // Remoting, the ISE and other hosts: questions and answers go through the host, one line at a time.
            options.Mode = InteractionMode.Line;
            options.LineConsole = HostConsole(cmdlet);
            return options;
        }

        private static ILineConsole HostConsole(PSCmdlet cmdlet) => cmdlet.Host?.UI != null ? new HostLineConsole(cmdlet.Host.UI) : (ILineConsole)ConsoleLineConsole.Instance;

        /// <summary>Whether PowerShell was started with -NonInteractive (pwsh -NonInteractive, powershell -noni).</summary>
        public static bool IsNonInteractiveSession() {
            foreach (string arg in Environment.GetCommandLineArgs()) {
                if (arg.Length >= 5 && arg.StartsWith("-noni", StringComparison.OrdinalIgnoreCase) && "-noninteractive".StartsWith(arg, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Reads a property by name, or runs a scriptblock with $_ set to the item.</summary>
        public static object? Select(object? item, object? selector) {
            selector = PSText.Unwrap(selector);
            if (selector == null || item == null) {
                return null;
            }
            if (selector is ScriptBlock script) {
                return Last(InvokeWith(script, item));
            }
            string name = selector.ToString() ?? string.Empty;
            if (PSText.Unwrap(item) is IDictionary dictionary && dictionary.Contains(name)) {
                return dictionary[name];
            }
            PSPropertyInfo? property = PSObject.AsPSObject(item).Properties[name];
            return property?.Value;
        }

        /// <summary>Runs a scriptblock on the pipeline thread with $_ (and $PSItem) set.</summary>
        public static Collection<PSObject> InvokeWith(ScriptBlock script, object? item, params KeyValuePair<string, object?>[] variables) {
            List<PSVariable> list = new List<PSVariable> { new PSVariable("_", item), new PSVariable("PSItem", item) };
            foreach (KeyValuePair<string, object?> v in variables) {
                list.Add(new PSVariable(v.Key, v.Value));
            }
            return script.InvokeWithContext(null, list, item);
        }

        public static object? Last(Collection<PSObject> output) {
            for (int i = output.Count - 1; i >= 0; i--) {
                if (output[i] != null) {
                    return output[i];
                }
            }
            return null;
        }

        public static string Text(object? value) {
            object? raw = PSText.Unwrap(value);
            if (raw == null) {
                return string.Empty;
            }
            if (raw is RichText rich) {
                return rich.PlainText;
            }
            return LanguagePrimitives.ConvertTo<string>(raw) ?? string.Empty;
        }

        /// <summary>Builds choices from objects; the chosen objects themselves are returned to the script.</summary>
        public static List<Choice> Choices(IEnumerable<object?> items, object? display, object? group, object? description, object? badge, object? hotkey, ScriptBlock? disabled, object? selected) {
            List<Choice> choices = new List<Choice>();
            HashSet<string> preselected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ScriptBlock? selectedScript = PSText.Unwrap(selected) as ScriptBlock;
            if (selected != null && selectedScript == null) {
                foreach (object? s in Enumerate(selected)) {
                    preselected.Add(Text(s));
                }
            }
            foreach (object? item in items) {
                if (PSText.Unwrap(item) is Choice existing) {
                    choices.Add(existing);
                    continue;
                }
                object? title = display != null ? Select(item, display) : null;
                Choice choice = new Choice(item, title != null ? Text(title) : Text(item));
                if (PSText.Unwrap(title) is RichText richTitle) {
                    choice.RichTitle = richTitle;
                }
                if (group != null) {
                    string g = Text(Select(item, group));
                    choice.Group = g.Length == 0 ? null : g;
                }
                if (description != null) {
                    string d = Text(Select(item, description));
                    choice.Description = d.Length == 0 ? null : d;
                }
                if (badge != null) {
                    object? b = Select(item, badge);
                    if (PSText.Unwrap(b) is RichText richBadge) {
                        choice.Badge = richBadge;
                    } else if (b != null && Text(b).Length > 0) {
                        choice.Badge = RichText.Plain(Text(b));
                    }
                }
                if (hotkey != null) {
                    string h = Text(Select(item, hotkey));
                    if (h.Length > 0) {
                        choice.Hotkey = h[0];
                    }
                }
                if (disabled != null) {
                    object? reason = Last(InvokeWith(disabled, item));
                    if (reason != null && !(PSText.Unwrap(reason) is bool b && !b)) {
                        choice.DisabledReason = PSText.Unwrap(reason) is bool ? "unavailable" : Text(reason);
                    }
                }
                if (selectedScript != null) {
                    choice.Selected = LanguagePrimitives.IsTrue(Last(InvokeWith(selectedScript, item)));
                } else if (preselected.Count > 0) {
                    choice.Selected = preselected.Contains(choice.Title) || preselected.Contains(Text(item));
                }
                choices.Add(choice);
            }
            return choices;
        }

        public static IEnumerable<object?> Enumerate(object? value) {
            object? raw = PSText.Unwrap(value);
            if (raw == null) {
                yield break;
            }
            if (raw is string || !(raw is IEnumerable list)) {
                yield return value;
                yield break;
            }
            foreach (object? item in list) {
                yield return item;
            }
        }

        /// <summary>Finds the choice for a default given as an object, a title or a value's text.</summary>
        public static Choice? Find(IList<Choice> choices, object? value) {
            object? raw = PSText.Unwrap(value);
            if (raw == null) {
                return null;
            }
            foreach (Choice c in choices) {
                if (Equals(PSText.Unwrap(c.Value), raw)) {
                    return c;
                }
            }
            string text = Text(raw);
            foreach (Choice c in choices) {
                if (string.Equals(c.Title, text, StringComparison.OrdinalIgnoreCase) || string.Equals(Text(c.Value), text, StringComparison.OrdinalIgnoreCase)) {
                    return c;
                }
            }
            return null;
        }

        /// <summary>A preview function from a scriptblock: its output becomes a widget (widgets, rich text, text, or an object as a list).</summary>
        public static Func<Choice, IWidget?>? Preview(ScriptBlock? preview) {
            if (preview == null) {
                return null;
            }
            return choice => ToWidget(InvokeWith(preview, choice.Value));
        }

        public static IWidget? ToWidget(Collection<PSObject> output) {
            List<IWidget> widgets = new List<IWidget>();
            List<string> lines = new List<string>();
            List<PSObject> objects = new List<PSObject>();
            foreach (PSObject item in output) {
                if (item == null) {
                    continue;
                }
                object? raw = item.BaseObject;
                IWidget? widget = PSWidgets.AsWidget(raw);
                if (widget != null) {
                    widgets.Add(widget);
                } else if (raw is string s) {
                    lines.Add(s);
                } else if (raw is ValueType) {
                    lines.Add(Text(raw));
                } else {
                    objects.Add(item);
                }
            }
            if (lines.Count > 0) {
                widgets.Insert(0, new TextBlock(string.Join("\n", lines)) { IsMarkup = false });
            }
            if (objects.Count > 0) {
                widgets.Add(PSFormatting.BuildList(objects, null, null, false, 1, null));
            }
            if (widgets.Count == 0) {
                return null;
            }
            return widgets.Count == 1 ? widgets[0] : new Rows(widgets.ToArray()) { Gap = 1 };
        }

        /// <summary>
        /// A validator from a scriptblock: $true or no output accepts, $false refuses with a generic message, a string is the
        /// error message.
        /// </summary>
        public static string? Validation(ScriptBlock script, object? value, PromptLabels labels, params KeyValuePair<string, object?>[] variables) {
            object? result;
            try {
                result = PSText.Unwrap(Last(InvokeWith(script, value, variables)));
            } catch (RuntimeException ex) {
                return ex.Message;
            }
            if (result == null || result is true) {
                return null;
            }
            if (result is false) {
                return labels.InvalidFormat;
            }
            string text = Text(result);
            return text.Length == 0 ? null : text;
        }

        /// <summary>
        /// A loader that runs a scriptblock in a new runspace on a worker thread (so the spinner keeps turning and Esc can
        /// cancel it). The script sees the current values as $Values and gets them as its first argument; variables of the
        /// caller are not visible, so pass what it needs through the values or -ArgumentList.
        /// </summary>
        public static Func<FormValues, CancellationToken, Task<IEnumerable<Choice>>> Loader(ScriptBlock script, object? display, object? group, object? description, object?[]? arguments) {
            string text = script.ToString();
            return (values, token) => Task.Run<IEnumerable<Choice>>(() => {
                Hashtable map = new Hashtable(StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, object?> pair in values) {
                    map[pair.Key] = pair.Value;
                }
                return RunIsolated(text, map, arguments, display, group, description, token);
            }, token);
        }

        /// <summary>A loader for a menu selection item.</summary>
        public static Func<MenuContext, CancellationToken, Task<IEnumerable<Choice>>> MenuLoader(ScriptBlock script, object? display, object? group, object? description, object?[]? arguments) {
            string text = script.ToString();
            return (context, token) => Task.Run<IEnumerable<Choice>>(() => RunIsolated(text, new Hashtable(), arguments, display, group, description, token), token);
        }

        private static IEnumerable<Choice> RunIsolated(string script, Hashtable values, object?[]? arguments, object? display, object? group, object? description, CancellationToken token) {
            using (System.Management.Automation.PowerShell shell = System.Management.Automation.PowerShell.Create(RunspaceMode.NewRunspace)) {
                shell.Runspace.SessionStateProxy.SetVariable("Values", values);
                shell.AddScript(script).AddArgument(values);
                if (arguments != null) {
                    foreach (object? argument in arguments) {
                        shell.AddArgument(argument);
                    }
                }
                using (token.Register(() => {
                    try {
                        shell.Stop();
                    } catch (Exception) {
                    }
                })) {
                    Collection<PSObject> output = shell.Invoke();
                    token.ThrowIfCancellationRequested();
                    if (shell.HadErrors && shell.Streams.Error.Count > 0 && output.Count == 0) {
                        throw new InvalidOperationException(shell.Streams.Error[0].ToString());
                    }
                    // Properties are read here, on the worker thread: the objects come from the isolated runspace and need no scriptblocks.
                    List<object?> items = new List<object?>();
                    foreach (PSObject item in output) {
                        if (item != null) {
                            items.Add(item);
                        }
                    }
                    return Choices(items, PropertyOnly(display), PropertyOnly(group), PropertyOnly(description), null, null, null, null);
                }
            }
        }

        // Scriptblocks belong to the caller's runspace and cannot run on the loader's thread; only property names are used there.
        private static object? PropertyOnly(object? selector) => PSText.Unwrap(selector) is ScriptBlock ? null : selector;

        public static SecureString ToSecureString(string text) {
            SecureString secure = new SecureString();
            foreach (char c in text) {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        /// <summary>Turns the cancellation and unavailable errors of prompts into PowerShell errors.</summary>
        public static void Fail(PSCmdlet cmdlet, Exception ex, string title) {
            switch (ex) {
                case PromptCancelledException cancelled:
                    cmdlet.ThrowTerminatingError(new ErrorRecord(cancelled, "PromptCancelled", ErrorCategory.OperationStopped, title));
                    return;
                case PromptUnavailableException unavailable:
                    cmdlet.ThrowTerminatingError(new ErrorRecord(unavailable, "PromptNeedsValue", ErrorCategory.InvalidOperation, title));
                    return;
                case Input.InputClosedException closed:
                    cmdlet.ThrowTerminatingError(new ErrorRecord(closed, "InputClosed", ErrorCategory.ReadError, title));
                    return;
            }
            throw ex;
        }
    }
}
