using System;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>One row of a <see cref="Lanes"/> widget: a worker, server or item with progress and result counts.</summary>
    public sealed class Lane {
        /// <summary>Creates a lane.</summary>
        /// <param name="key">Unique key.</param>
        /// <param name="label">Display label (plain text).</param>
        public Lane(string key, string label) {
            Key = key;
            _label = label ?? string.Empty;
        }

        private string _label;
        private RichText? _labelText;
        private string? _detail;
        private RichText? _detailText;

        /// <summary>Unique key.</summary>
        public string Key { get; }

        /// <summary>Display label (literal; setting it clears <see cref="LabelText"/>).</summary>
        public string Label {
            get => _label;
            set {
                _label = value ?? string.Empty;
                _labelText = null;
            }
        }

        /// <summary>A multicolour label, or null for the literal <see cref="Label"/> (which becomes its plain text).</summary>
        public RichText? LabelText {
            get => _labelText;
            set {
                _labelText = value;
                if (value != null) {
                    _label = value.PlainText;
                }
            }
        }

        /// <summary>Planned steps (0 = unknown).</summary>
        public double Total { get; set; }

        /// <summary>Completed steps.</summary>
        public double Done { get; set; }

        /// <summary>Steps that passed.</summary>
        public int Passed { get; set; }

        /// <summary>Steps that failed.</summary>
        public int Failed { get; set; }

        /// <summary>Steps with warnings.</summary>
        public int Warned { get; set; }

        /// <summary>Steps skipped.</summary>
        public int Skipped { get; set; }

        /// <summary>The lane status.</summary>
        public StatusKind Status { get; set; } = StatusKind.Pending;

        /// <summary>What the lane is doing now (current rule, step, file); literal, and setting it clears <see cref="DetailText"/>.</summary>
        public string? Detail {
            get => _detail;
            set {
                _detail = value;
                _detailText = null;
            }
        }

        /// <summary>A multicolour detail, or null for the literal <see cref="Detail"/> (which becomes its plain text).</summary>
        public RichText? DetailText {
            get => _detailText;
            set {
                _detailText = value;
                if (value != null) {
                    _detail = value.PlainText;
                }
            }
        }

        /// <summary>When the lane started (session time), if it has.</summary>
        public TimeSpan? Started { get; set; }

        /// <summary>When the lane finished (session time), if it has.</summary>
        public TimeSpan? Finished { get; set; }

        /// <summary>Insertion order, used to keep rows stable.</summary>
        public int Order { get; set; }

        // The smoothed rate for the Rate and Remaining progress columns.
        internal RateMeter Meter { get; } = new RateMeter();

        /// <summary>Whether the lane has finished.</summary>
        public bool IsFinished => Status == StatusKind.Pass || Status == StatusKind.Fail || Status == StatusKind.Warn || Status == StatusKind.Skip;

        /// <summary>The fraction complete (0-1); finished lanes are 1.</summary>
        public double Fraction => IsFinished ? 1 : (Total <= 0 ? 0 : Math.Min(1, Done / Total));
    }
}
