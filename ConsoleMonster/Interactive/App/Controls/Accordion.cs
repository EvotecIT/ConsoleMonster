using System;
using System.Collections.Generic;
using ConsoleMonster.Canvas;
using ConsoleMonster.Input;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Collapsible sections stacked as a group that keeps one open: expanding a section folds the others. While a header has
    /// focus, <c>+</c> expands every section for reading and <c>-</c> folds them all; both are ordinary key bindings on the
    /// sections, so the footer lists them.
    /// </summary>
    public sealed class Accordion : IContainer, IContentOwner {
        private readonly object _gate = new object();
        private readonly List<Collapsible> _sections = new List<Collapsible>();
        private readonly Dictionary<Collapsible, KeyBinding[]> _keys = new Dictionary<Collapsible, KeyBinding[]>();
        private Rows _rows = new Rows();
        private bool _all;

        /// <summary>Creates a group.</summary>
        /// <param name="children">The sections, in order.</param>
        public Accordion(params Collapsible[] children) {
            if (children != null) { foreach (Collapsible section in children) { Add(section); } }
        }

        /// <summary>The sections, in order.</summary>
        public IReadOnlyList<Collapsible> Sections { get { lock (_gate) { return _sections.ToArray(); } } }

        /// <inheritdoc />
        public IReadOnlyList<IWidget> Children { get { lock (_gate) { return _sections.ToArray(); } } }

        /// <summary>Adds a section. Add every section before the app runs.</summary>
        /// <param name="section">The section; it can belong to one group.</param>
        /// <returns>This group.</returns>
        public Accordion Add(Collapsible section) {
            if (section == null) { throw new ArgumentNullException(nameof(section)); }
            lock (_gate) {
                if (section.Group != null) { throw new ArgumentException("A section belongs to one accordion.", nameof(section)); }
                section.Group = this;
                _sections.Add(section);
                _rows.Add(section);
            }
            PromptLabels labels = PromptLabels.Default;
            KeyBinding[] keys = {
                new KeyBinding(labels.KeyExpandAll, ExpandAll, new KeyChord('+')) { Order = 2 },
                new KeyBinding(labels.KeyCollapseAll, CollapseAll, new KeyChord('-')) { Order = 3 }
            };
            foreach (KeyBinding key in keys) { section.Bindings.Add(key); }
            lock (_gate) { _keys[section] = keys; }
            section.Changed += Folded;
            return this;
        }

        /// <summary>Expands every section until one is folded or opened on its own again.</summary>
        public void ExpandAll() {
            lock (_gate) { _all = true; }
            try { foreach (Collapsible section in Sections) { section.Expanded = true; } }
            finally { lock (_gate) { _all = false; } }
        }

        /// <summary>Folds every section.</summary>
        public void CollapseAll() {
            lock (_gate) { _all = true; }
            try { foreach (Collapsible section in Sections) { section.Expanded = false; } }
            finally { lock (_gate) { _all = false; } }
        }

        // Opening one section folds the rest, unless the whole group is being expanded.
        private void Folded(object? sender, AppValueChangedEventArgs change) {
            if (!(sender is Collapsible opened) || !(change.Value is bool expanded) || !expanded) { return; }
            lock (_gate) { if (_all) { return; } }
            foreach (Collapsible section in Sections) {
                if (!ReferenceEquals(section, opened)) { section.Expanded = false; }
            }
        }

        void IContentOwner.ReplaceContent(IWidget[] content) {
            foreach (IWidget widget in content) {
                if (!(widget is Collapsible)) { throw new ArgumentException("An accordion holds collapsible sections.", nameof(content)); }
            }
            lock (_gate) {
                foreach (Collapsible section in _sections) {
                    section.Group = null;
                    section.Changed -= Folded;
                    foreach (KeyBinding key in _keys[section]) { section.Bindings.Remove(key); }
                }
                _keys.Clear();
                _sections.Clear();
                _rows = new Rows();
            }
            foreach (IWidget widget in content) { Add((Collapsible)widget); }
        }

        /// <inheritdoc />
        public Size Measure(Constraints constraints, RenderContext context) => Layout().Measure(constraints, context);

        /// <inheritdoc />
        public void Render(Region region, RenderContext context) => Layout().Render(region, context);

        private Rows Layout() { lock (_gate) { return _rows; } }
    }
}
