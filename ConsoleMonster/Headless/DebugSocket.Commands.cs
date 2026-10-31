using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Export;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;

namespace ConsoleMonster.Headless {
    public sealed partial class DebugSocket {
        private const int MaxTreeNodes = 2000;

        private Dictionary<string, object?> Handle(string command, Dictionary<string, object?> request) {
            switch (command) {
                case "hello":
                    return Ok(new Dictionary<string, object?> {
                        ["protocol"] = ProtocolVersion, ["pid"] = Process.GetCurrentProcess().Id,
                        ["width"] = _terminal.Width, ["height"] = _terminal.Height
                    });
                case "screen":
                    return Screen(Text(request, "format") ?? "text");
                case "tree":
                    return Tree();
                case "keys":
                    _terminal.Send(Text(request, "keys") ?? throw new FormatException("'keys' is required."));
                    return Ok(null);
                case "text":
                    foreach (char c in Text(request, "text") ?? throw new FormatException("'text' is required.")) {
                        _terminal.Send(c == '\n' ? InputEvent.Press(ConsoleKey.Enter) : InputEvent.Character(c));
                    }
                    return Ok(null);
                case "resize":
                    _terminal.Resize(Number(request, "width", _terminal.Width, 2, 1000), Number(request, "height", _terminal.Height, 2, 1000));
                    return Ok(null);
                case "click":
                    int x = Number(request, "x", null, 0, _terminal.Width - 1), y = Number(request, "y", null, 0, _terminal.Height - 1);
                    int button = Number(request, "button", 1, 1, 3);
                    _terminal.Send(InputEvent.Mouse(x, y, button, true));
                    _terminal.Send(InputEvent.Mouse(x, y, button, false));
                    return Ok(null);
                case "wait":
                    string expected = Text(request, "text") ?? throw new FormatException("'text' is required.");
                    int timeout = Number(request, "timeout", 5000, 1, 600000);
                    // Matched against the masked screen, so waiting cannot reveal a masked secret one guess at a time.
                    bool found = _terminal.TryWaitFor(frame => ScreenRedaction.Apply(frame, _redactor).ToPlainText().IndexOf(expected, StringComparison.Ordinal) >= 0, TimeSpan.FromMilliseconds(timeout), _stopToken);
                    return Ok(new Dictionary<string, object?> { ["found"] = found });
                default:
                    throw new FormatException("Unknown command '" + command + "'. Use hello, screen, tree, keys, text, resize, click or wait.");
            }
        }

        private static Dictionary<string, object?> Ok(Dictionary<string, object?>? values) {
            Dictionary<string, object?> reply = new Dictionary<string, object?> { ["ok"] = true };
            if (values != null) { foreach (KeyValuePair<string, object?> pair in values) { reply[pair.Key] = pair.Value; } }
            return reply;
        }

        private Dictionary<string, object?> Screen(string format) {
            CellBuffer frame = ScreenRedaction.Apply(_terminal.Snapshot(), _redactor);
            Dictionary<string, object?> values = new Dictionary<string, object?> { ["width"] = frame.Width, ["height"] = frame.Height };
            if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase)) {
                values["text"] = frame.ToPlainText();
            } else if (string.Equals(format, "png", StringComparison.OrdinalIgnoreCase)) {
                // The PNG encoder lives in ConsoleMonster.Graphics; an app opts in by registering its provider.
                values["png"] = Convert.ToBase64String(FrameExporter.Export(frame, "Png", new ExportOptions { Width = frame.Width, Height = frame.Height }));
            } else {
                throw new FormatException("Use format text or png.");
            }
            return Ok(values);
        }

        // A best-effort snapshot: the app keeps running while the tree is read, so a read that meets a changing container retries.
        private Dictionary<string, object?> Tree() {
            LiveSession? session = LiveSession.CurrentFor(_terminal);
            int budget = MaxTreeNodes;
            string? focus = null;
            Dictionary<string, object?>? root = null;
            for (int attempt = 0; session != null; attempt++) {
                budget = MaxTreeNodes;
                focus = null;
                try { root = Node(session.Root, 0, ref budget, ref focus); break; }
                catch (InvalidOperationException) when (attempt < 2) { Thread.Sleep(10); }
            }
            return Ok(new Dictionary<string, object?> {
                ["root"] = root, ["focus"] = focus, ["truncated"] = budget <= 0,
                // Nodes laid out by rows and columns in the last app frame carry their measured and arranged size and the rule.
                ["layout"] = LayoutNotes.HasFrame
            });
        }

        // Types, names, labels, bounds and focus only: input values never leave the process.
        private static Dictionary<string, object?>? Node(IWidget widget, int depth, ref int budget, ref string? focus) {
            if (budget-- <= 0 || depth > 64) { return null; }
            Dictionary<string, object?> node = new Dictionary<string, object?> { ["type"] = widget.GetType().Name };
            if (widget is ILiveTarget target && !string.IsNullOrEmpty(target.WidgetId)) { node["id"] = target.WidgetId; }
            if (LayoutNotes.Get(widget) is LayoutNote note) {
                node["layout"] = new Dictionary<string, object?> {
                    ["measured"] = new Dictionary<string, object?> { ["width"] = note.Measured.Width, ["height"] = note.Measured.Height },
                    ["arranged"] = new Dictionary<string, object?> { ["width"] = note.Arranged.Width, ["height"] = note.Arranged.Height },
                    ["rule"] = note.Rule
                };
            }
            if (widget is InputControl input) {
                node["key"] = input.Key;
                if (input.Label != null) { node["label"] = input.Label; }
                node["focused"] = input.Focused;
                node["disabled"] = input.Disabled;
                Rect bounds = input.Bounds;
                if (bounds.Width > 0 && bounds.Height > 0) {
                    node["bounds"] = new Dictionary<string, object?> { ["x"] = bounds.X, ["y"] = bounds.Y, ["width"] = bounds.Width, ["height"] = bounds.Height };
                }
                if (input.Focused) { focus = input.Key; }
            }
            if (widget is IContainer container) {
                List<object?> children = new List<object?>();
                foreach (IWidget child in container.Children) {
                    Dictionary<string, object?>? item = Node(child, depth + 1, ref budget, ref focus);
                    if (item == null) { break; }
                    children.Add(item);
                }
                if (children.Count > 0) { node["children"] = children; }
            }
            return node;
        }
    }
}
