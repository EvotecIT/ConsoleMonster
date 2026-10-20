using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Text;
using ConsoleMonster.Interactive;
using ConsoleMonster.Styling;

namespace ConsoleMonster.PowerShell {
    /// <summary>Menu helpers for the cmdlets: collecting items from a scriptblock and running actions on the pipeline thread.</summary>
    internal static class PSMenus {
        /// <summary>Runs a content scriptblock and keeps the menu items it outputs.</summary>
        /// <param name="content">The scriptblock.</param>
        /// <returns>The items.</returns>
        public static List<MenuItem> CollectItems(ScriptBlock? content) => CollectItems(content, null);

        /// <summary>Runs a content scriptblock and keeps the menu items it outputs, and the built-in presets when asked.</summary>
        /// <param name="content">The scriptblock.</param>
        /// <param name="presets">Receives the presets (New-ConsoleMenuPreset) it outputs, or null to ignore them.</param>
        /// <returns>The items.</returns>
        public static List<MenuItem> CollectItems(ScriptBlock? content, List<MenuPreset>? presets) {
            List<MenuItem> items = new List<MenuItem>();
            if (content == null) {
                return items;
            }
            foreach (PSObject output in content.Invoke()) {
                if (output?.BaseObject is MenuItem item) {
                    items.Add(item);
                } else if (presets != null && output?.BaseObject is MenuPreset preset) {
                    presets.Add(preset);
                }
            }
            return items;
        }

        /// <summary>
        /// Runs an action scriptblock with $MenuContext (and $_) set. Write-Host, warnings and non-terminating errors are
        /// redirected into the menu's action output (shown as a toast or an output dialog when the action returns) instead of
        /// being drawn over the app; ordinary output is discarded as before.
        /// </summary>
        /// <param name="script">The action.</param>
        /// <param name="context">The menu context.</param>
        /// <param name="variables">More variables to define.</param>
        public static void Invoke(ScriptBlock script, MenuContext context, params KeyValuePair<string, object?>[] variables) {
            List<PSVariable> list = new List<PSVariable> {
                new PSVariable("_", context),
                new PSVariable("PSItem", context),
                new PSVariable("MenuContext", context)
            };
            foreach (KeyValuePair<string, object?> v in variables) {
                list.Add(new PSVariable(v.Key, v.Value));
            }
            // The wrapper runs in the action's session state (bound to its module when it has one), so the action still sees
            // $MenuContext and the caller's variables through normal scoping.
            ScriptBlock capture = ScriptBlock.Create("param($__cmAction) & $__cmAction 2>&1 3>&1 6>&1");
            if (script.Module != null) {
                capture = script.Module.NewBoundScriptBlock(capture);
            }
            Collection<PSObject> output = capture.InvokeWithContext(null, list, script);
            Route(output, context);
        }

        private static void Route(Collection<PSObject> output, MenuContext context) {
            StringBuilder pending = new StringBuilder();
            foreach (PSObject item in output) {
                switch (item?.BaseObject) {
                    case InformationRecord record:
                        if (record.MessageData is HostInformationMessage message) {
                            pending.Append(message.Message);
                            if (message.NoNewLine == true) {
                                continue;
                            }
                            Flush(pending, context);
                        } else {
                            Flush(pending, context);
                            context.Log(PSWidgets.ToText(record.MessageData), StatusKind.Info);
                        }
                        break;
                    case WarningRecord warning:
                        Flush(pending, context);
                        context.Log(warning.Message, StatusKind.Warn);
                        break;
                    case ErrorRecord error:
                        Flush(pending, context);
                        context.Log(error.ToString(), StatusKind.Fail);
                        break;
                }
            }
            Flush(pending, context);
        }

        private static void Flush(StringBuilder pending, MenuContext context) {
            if (pending.Length == 0) {
                return;
            }
            foreach (string line in pending.ToString().Replace("\r\n", "\n").Split('\n')) {
                context.Log(line, StatusKind.Info);
            }
            pending.Clear();
        }
    }
}
