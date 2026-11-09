using System;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Frames {
    /// <summary>Fluent builders for a <see cref="DashboardFrame"/>: the same parts New-ConsoleDashboardFrame takes as records.</summary>
    public static partial class FramesFluent {
        /// <summary>Sets the title.</summary><param name="frame">The frame.</param><param name="title">The literal title.</param><returns>The frame.</returns>
        public static DashboardFrame Title(this DashboardFrame frame, string title) {
            Require(frame).Header.Title = title ?? throw new ArgumentNullException(nameof(title));
            return frame;
        }

        /// <summary>Adds facts after the title, such as the scope and counts.</summary><param name="frame">The frame.</param><param name="facts">The facts, shown literally.</param><returns>The frame.</returns>
        public static DashboardFrame Fact(this DashboardFrame frame, params string[] facts) {
            foreach (string fact in facts ?? throw new ArgumentNullException(nameof(facts))) { Require(frame).Header.Facts.Add(fact); }
            return frame;
        }

        /// <summary>Adds a state at the right of the title line, such as <c>× 1 failing</c>.</summary><param name="frame">The frame.</param><param name="state">The health state.</param><param name="count">How many.</param><param name="word">The word after the count.</param><returns>The frame.</returns>
        public static DashboardFrame State(this DashboardFrame frame, HealthState state, long count, string word) {
            Require(frame).Header.States.Add(FrameState.Of(state, count, word));
            return frame;
        }

        /// <summary>Sets when the data was observed; the title line shows how long ago.</summary><param name="frame">The frame.</param><param name="observedAt">The observation time.</param><returns>The frame.</returns>
        public static DashboardFrame ObservedAt(this DashboardFrame frame, DateTimeOffset observedAt) {
            Require(frame).Header.ObservedAt = observedAt;
            return frame;
        }

        /// <summary>Adds a headline meter; meters print in the order added and a narrow row keeps the first ones.</summary><param name="frame">The frame.</param><param name="meter">The meter.</param><returns>The frame.</returns>
        public static DashboardFrame Meter(this DashboardFrame frame, Meter meter) {
            Require(frame).Meters.Add(meter ?? throw new ArgumentNullException(nameof(meter)));
            return frame;
        }

        /// <summary>Adds an item to Needs attention.</summary><param name="frame">The frame.</param><param name="state">The item's state.</param><param name="identity">What it is, such as a server name.</param><param name="reason">Why it needs attention.</param><param name="values">Short values shown beside it.</param><returns>The frame.</returns>
        public static DashboardFrame Attention(this DashboardFrame frame, HealthState state, string identity, string reason, params string[] values) {
            AttentionItem item = new AttentionItem(state, identity, reason, Require(frame).Attention.Items.Count);
            foreach (string value in values ?? Array.Empty<string>()) { item.Values.Add(value); }
            frame.Attention.Items.Add(item);
            return frame;
        }

        /// <summary>Adds the findings an <see cref="AttentionTally"/> counted.</summary><param name="frame">The frame.</param><param name="tally">The counted findings.</param><returns>The frame.</returns>
        public static DashboardFrame Attention(this DashboardFrame frame, AttentionTally tally) {
            (tally ?? throw new ArgumentNullException(nameof(tally))).AddTo(Require(frame).Attention);
            return frame;
        }

        /// <summary>Names what the attention list counts, as in <c>3 of 48 servers</c>.</summary><param name="frame">The frame.</param><param name="noun">The singular noun.</param><param name="outOf">The total, or null to leave it out.</param><returns>The frame.</returns>
        public static DashboardFrame AttentionOf(this DashboardFrame frame, string noun, long? outOf = null) {
            Require(frame).Attention.Noun = noun ?? throw new ArgumentNullException(nameof(noun));
            frame.Attention.OutOf = outOf;
            return frame;
        }

        /// <summary>Adds a section built from content; <paramref name="configure"/> sets its placement, facts and widths.</summary><param name="frame">The frame.</param><param name="title">The section title.</param><param name="content">The section's widgets.</param><param name="configure">Optional section settings.</param><returns>The frame.</returns>
        public static DashboardFrame Section(this DashboardFrame frame, string title, Action<ContentBuilder> content, Action<FrameSection>? configure = null) {
            IWidget body = ContentBuilder.Build(content ?? throw new ArgumentNullException(nameof(content)));
            FrameSection section = new FrameSection(title, _ => body);
            configure?.Invoke(section);
            Require(frame).Sections.Add(section);
            return frame;
        }

        /// <summary>Adds a legend entry for a state the frame uses.</summary><param name="frame">The frame.</param><param name="state">The state.</param><param name="word">Its word.</param><returns>The frame.</returns>
        public static DashboardFrame Legend(this DashboardFrame frame, HealthState state, string word) {
            Require(frame).Legend.Add(state, word);
            return frame;
        }

        /// <summary>Adds a count to the footer.</summary><param name="frame">The frame.</param><param name="count">The count text.</param><returns>The frame.</returns>
        public static DashboardFrame FooterCount(this DashboardFrame frame, string count) {
            Require(frame).Footer.Counts.Add(count ?? throw new ArgumentNullException(nameof(count)));
            return frame;
        }

        /// <summary>Sets the footer hint.</summary><param name="frame">The frame.</param><param name="hint">The hint.</param><returns>The frame.</returns>
        public static DashboardFrame FooterHint(this DashboardFrame frame, FrameHint hint) {
            Require(frame).Footer.Hint = hint;
            return frame;
        }

        private static DashboardFrame Require(DashboardFrame frame) => frame ?? throw new ArgumentNullException(nameof(frame));
    }
}
