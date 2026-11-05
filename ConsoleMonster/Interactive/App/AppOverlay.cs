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
        internal void Publish(AppScreens.View? view, FooterBar? footer, IReadOnlyList<KeyHint>? hints, int focusVersion, KeyScope scope, LiveSession? session) {
            Snapshot snapshot = new Snapshot(view, footer, hints, focusVersion, scope);
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
            // The hints and crumbs it draws belong to the key scope the snapshot was published for.
            using (Targets.Collect(snapshot?.Scope))
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
                // Hints and crumbs the popup covers are not clickable through it.
                Targets.CoverArea(place);
            }
            AppContextMenu? menu;
            lock (_gate) { menu = _menu; }
            if (menu != null) {
                Rect anchor = new Rect(Math.Max(body.Bounds.X, Math.Min(menu.Column, body.Bounds.Right - 1)), Math.Max(body.Bounds.Y, Math.Min(menu.Row, body.Bounds.Bottom - 1)), 1, 1);
                Size size = menu.Measure(new Constraints(body.Width, body.Height), context);
                // Below and to the right of the pointer when it fits, otherwise moved back inside the body.
                int x = Math.Max(body.Bounds.X, Math.Min(anchor.X, body.Bounds.Right - size.Width));
                int y = anchor.Y + size.Height <= body.Bounds.Bottom ? anchor.Y : Math.Max(body.Bounds.Y, anchor.Y + 1 - size.Height);
                Rect place = new Rect(x, y, size.Width, Math.Min(size.Height, body.Bounds.Bottom - y));
                Region area = region.Sub(place.X - region.Bounds.X, place.Y - region.Bounds.Y, place.Width, place.Height);
                area.Fill(Style.Plain);
                using (Overlay.Painting()) { menu.Render(area, context); }
                menu.Place(place);
                Targets.CoverArea(place);
            }
            IWidget? dialog = _actions.ModalLayer(context, body.Width, body.Height, out int maxWidth);
            if (dialog != null) { Overlay.Paint(body, dialog, context, maxWidth, false, true); placed.Clear(); Targets.CoverBody(body.Bounds); }
            _modalDrawn = dialog != null;
            Toast? toast;
            lock (_gate) { _popups.Clear(); _popups.AddRange(placed); toast = _toast; }
            if (toast != null) {
                Targets.CoverRows(region.Bounds.Y + region.Height - 1);
                Region row = region.Sub(0, region.Height - 1, region.Width, 1);
                row.Fill(Style.Plain);
                new FooterBar { Message = toast.Line(context, Style.Plain) }.Render(row, context);
            }
        }

        /// <summary>The footer hints and breadcrumbs the last frame drew.</summary>
        internal PointerTargets Targets { get; } = new PointerTargets(regions: false);

        private AppContextMenu? _menu;
        private volatile bool _modalDrawn;

        // Whether the last frame drew a dialog, so its hints (the dialog's keys) are the ones on screen.
        internal bool ModalDrawn => _modalDrawn;

        internal AppContextMenu? Menu {
            get { lock (_gate) { return _menu; } }
            set { lock (_gate) { _menu = value; } }
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
        // The owner of the popup that took the pointer, and its linked selection before the event, so the app publishes
        // only a selection the popup changed.
        internal bool RoutePointer(InputEvent input, RenderContext context, out InputControl? owner, out int? before) {
            owner = null;
            before = null;
            Placed[] popups;
            lock (_gate) { popups = _popups.ToArray(); }
            for (int i = popups.Length - 1; i >= 0; i--) {
                if (Contains(popups[i].Area, input)) {
                    owner = popups[i].Owner;
                    before = (owner as ILinkedSelection)?.SelectedIndex;
                    owner.HandlePopupInput(input, context);
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
            internal Snapshot(AppScreens.View? view, FooterBar? footer, IReadOnlyList<KeyHint>? hints, int focusVersion, KeyScope scope) { View = view; Footer = footer; Hints = hints; FocusVersion = focusVersion; Scope = scope; }
            internal AppScreens.View? View { get; } internal FooterBar? Footer { get; } internal IReadOnlyList<KeyHint>? Hints { get; } internal int FocusVersion { get; }
            internal KeyScope Scope { get; }
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
