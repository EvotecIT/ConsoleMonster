using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// The app's overlay layer. The content paints first and records where every control was arranged; this layer then
    /// paints, in z-order, the popups controls asked for (anchored below or above their control), the open dialog over a
    /// dimmed body, and a toast in the footer row. Nothing below has to fake drawing on top. Idea credited to
    /// SharpConsoleUI's portal layer; no windowing or compositing.
    /// </summary>
    internal sealed class AppOverlay : IContainer {
        private readonly object _gate = new object();
        private readonly IWidget _content;
        private readonly AppViewport _viewport;
        private readonly AppCommandHost _actions;
        private readonly AppScreens? _screens;
        private readonly Func<TimeSpan> _clock;
        private readonly List<Placed> _popups = new List<Placed>();
        private Toast? _toast;
        private static long _next;
        // Carries the frame snapshot through the live session's queue, behind the bindings' content posted before it, so a
        // frame never shows a new screen over content that has not arrived yet.
        private readonly RegionWidget _carrier = new RegionWidget("AppFrame" + Interlocked.Increment(ref _next));

        // The shown screen, its crumbs and the app footer's hints, published together on the app thread.
        internal void Publish(AppScreens.View? view, FooterBar? footer, IReadOnlyList<KeyHint>? hints, int focusVersion, LiveSession? session) {
            Snapshot snapshot = new Snapshot(view, footer, hints, focusVersion);
            if (session == null) { _carrier.Set(snapshot); }
            else { _carrier.Handle(session).Set(snapshot); }
        }

        internal AppOverlay(IWidget content, AppViewport viewport, AppCommandHost actions, AppScreens? screens, Func<TimeSpan> clock) {
            _content = content; _viewport = viewport; _actions = actions; _screens = screens; _clock = clock;
        }

        public IReadOnlyList<IWidget> Children => new IWidget[] { _content, _carrier };

        public Size Measure(Constraints constraints, RenderContext context) => _content.Measure(constraints, context);

        public void Render(Region region, RenderContext context) {
            // A frame reconciles focus only against the focus the snapshot it draws was published with.
            Snapshot? snapshot = _carrier.Content as Snapshot;
            _viewport.BeginFrame(snapshot?.FocusVersion);
            using (AppScreens.Scope(snapshot?.View))
            using (FooterBar.FrameHints(snapshot?.Footer, snapshot?.Hints))
            using (LayoutNotes.Frame()) { _content.Render(region, context); }
            if (region.Width <= 0 || region.Height <= 0) { return; }
            Region body = region.Sub(0, 0, region.Width, Math.Max(0, region.Height - 1));
            List<Placed> placed = new List<Placed>();
            foreach (InputArrangement.Popup popup in _viewport.Popups()) {
                Rect place = Place(popup.Anchor, popup.Content, body.Bounds, context);
                if (place.Width <= 0 || place.Height <= 0) { continue; }
                Region area = region.Sub(place.X - region.Bounds.X, place.Y - region.Bounds.Y, place.Width, place.Height);
                area.Fill(Style.Plain);
                using (Overlay.Painting()) { popup.Content.Render(area, context); }
                placed.Add(new Placed(popup.Owner, place, popup.Anchor));
            }
            IWidget? dialog = _actions.ModalLayer(context, body.Width, body.Height, out int maxWidth);
            if (dialog != null) { Overlay.Paint(body, dialog, context, maxWidth, false, true); placed.Clear(); }
            Toast? toast;
            lock (_gate) { _popups.Clear(); _popups.AddRange(placed); toast = _toast; }
            if (toast != null) {
                Region row = region.Sub(0, region.Height - 1, region.Width, 1);
                row.Fill(Style.Plain);
                new FooterBar { Message = toast.Line(context, Style.Plain) }.Render(row, context);
            }
        }

        internal void ShowToast(Toast toast) {
            toast.ShownAt = _clock();
            lock (_gate) { _toast = toast; }
        }

        // Clears an expired toast; true when the frame changed.
        internal bool Expire() {
            lock (_gate) {
                if (_toast == null || !_toast.IsExpired(_clock())) { return false; }
                _toast = null;
                return true;
            }
        }

        // A press inside a popup goes to its control; a press elsewhere closes the open popups and carries on.
        internal bool RoutePointer(InputEvent input, RenderContext context) {
            Placed[] popups;
            lock (_gate) { popups = _popups.ToArray(); }
            for (int i = popups.Length - 1; i >= 0; i--) {
                if (Contains(popups[i].Area, input)) {
                    popups[i].Owner.HandlePopupInput(input, context);
                    return true;
                }
            }
            if (input.MouseAction == MouseAction.Down) {
                foreach (Placed popup in popups) {
                    // A press on the popup's own control reaches it (it toggles the popup).
                    if (!Contains(popup.Anchor, input)) { popup.Owner.ClosePopup(); }
                }
            }
            return false;
        }

        private static bool Contains(Rect area, InputEvent input) => input.Column >= area.X && input.Column < area.Right && input.Row >= area.Y && input.Row < area.Bottom;

        private sealed class Snapshot : IWidget, IUnrecorded {
            internal Snapshot(AppScreens.View? view, FooterBar? footer, IReadOnlyList<KeyHint>? hints, int focusVersion) { View = view; Footer = footer; Hints = hints; FocusVersion = focusVersion; }
            internal AppScreens.View? View { get; } internal FooterBar? Footer { get; } internal IReadOnlyList<KeyHint>? Hints { get; } internal int FocusVersion { get; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(0, 0);
            public void Render(Region region, RenderContext context) { }
        }

        private sealed class Placed {
            internal Placed(InputControl owner, Rect area, Rect anchor) { Owner = owner; Area = area; Anchor = anchor; }
            internal InputControl Owner { get; } internal Rect Area { get; } internal Rect Anchor { get; }
        }

        // Below the control when it fits, otherwise above, otherwise on the side with more room; never wider than the body.
        internal static Rect Place(Rect anchor, IWidget popup, Rect body, RenderContext context) {
            int below = body.Bottom - anchor.Bottom;
            int above = anchor.Y - body.Y;
            int width = Math.Min(body.Width, Math.Max(anchor.Width, 1));
            Size natural = popup.Measure(new Constraints(width, Math.Max(below, above)), context);
            int height = natural.Height;
            bool down = height <= below || below >= above;
            height = Math.Min(height, down ? below : above);
            int x = Math.Max(body.X, Math.Min(anchor.X, body.Right - width));
            return new Rect(x, down ? anchor.Bottom : anchor.Y - height, width, Math.Max(0, height));
        }
    }
}
