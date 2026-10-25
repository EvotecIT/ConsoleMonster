using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>
    /// Line-based questions for <see cref="InteractionMode.Line"/> (remoting, the ISE, consoles without VT) and
    /// <see cref="InteractionMode.Accessible"/> (screen readers): a question, numbered choices, a typed answer, no redraws.
    /// Accessible mode also confirms every answer as text.
    /// </summary>
    internal sealed class LinePrompter {
        private readonly ILineConsole _io;
        private readonly PromptLabels _labels;
        private readonly bool _accessible;

        public LinePrompter(ILineConsole io, PromptLabels labels, bool accessible) {
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            _accessible = accessible;
        }

        public bool Accessible => _accessible;

        public PromptLabels Labels => _labels;

        public void Say(string text) => _io.WriteLine(text);

        public bool Confirm(string title, string? description, bool? defaultValue, CancellationToken token) {
            if (!_accessible) {
                int? chosen = _io.Choose(title, description ?? string.Empty, new[] { "&" + _labels.Yes, "&" + _labels.No }, defaultValue.HasValue ? (defaultValue.Value ? 0 : 1) : -1);
                if (chosen.HasValue) {
                    return chosen.Value == 0;
                }
            }
            WriteDescription(description);
            string yes = _labels.Yes.Substring(0, 1).ToLowerInvariant();
            string no = _labels.No.Substring(0, 1).ToLowerInvariant();
            string hint = defaultValue.HasValue ? (defaultValue.Value ? yes.ToUpperInvariant() + "/" + no : yes + "/" + no.ToUpperInvariant()) : yes + "/" + no;
            while (true) {
                token.ThrowIfCancellationRequested();
                _io.Write(title + " (" + hint + "): ");
                string? line = _io.ReadLine();
                if (line == null) {
                    if (defaultValue.HasValue) {
                        return defaultValue.Value;
                    }
                    throw new InputClosedException();
                }
                string answer = line.Trim().ToLowerInvariant();
                if (answer.Length == 0 && defaultValue.HasValue) {
                    Confirmed(defaultValue.Value ? _labels.Yes : _labels.No);
                    return defaultValue.Value;
                }
                if (answer == yes || answer == _labels.Yes.ToLowerInvariant() || answer == "y" || answer == "yes") {
                    Confirmed(_labels.Yes);
                    return true;
                }
                if (answer == no || answer == _labels.No.ToLowerInvariant() || answer == "n" || answer == "no") {
                    Confirmed(_labels.No);
                    return false;
                }
                _io.WriteLine(PromptLabels.Format(_labels.AnswerYesNo, yes, no));
            }
        }

        public string Text(string title, string? description, string? defaultValue, Func<string, string?>? validate, bool secret, CancellationToken token) {
            WriteDescription(description);
            while (true) {
                token.ThrowIfCancellationRequested();
                string question = title;
                if (!string.IsNullOrEmpty(defaultValue) && !secret) {
                    question += " (" + PromptLabels.Format(_labels.DefaultHint, defaultValue) + ")";
                }
                _io.Write(question + ": ");
                string? line = secret ? _io.ReadSecret() : _io.ReadLine();
                if (line == null) {
                    if (defaultValue != null) {
                        return defaultValue;
                    }
                    throw new InputClosedException();
                }
                string answer = line;
                if (answer.Length == 0 && defaultValue != null) {
                    answer = defaultValue;
                }
                string? error = validate?.Invoke(answer);
                if (error != null) {
                    _io.WriteLine(error);
                    continue;
                }
                if (!secret) {
                    Confirmed(answer);
                }
                return answer;
            }
        }

        /// <summary>Several lines of text, ended by an empty line (or the end of input).</summary>
        public string Lines(string title, string? description, string? defaultValue, Func<string, string?>? validate, CancellationToken token) {
            WriteDescription(description);
            while (true) {
                token.ThrowIfCancellationRequested();
                _io.WriteLine(title + " (" + _labels.EndWithEmptyLine + "):");
                List<string> lines = new List<string>();
                while (true) {
                    string? line = _io.ReadLine();
                    if (line == null || line.Length == 0) {
                        break;
                    }
                    lines.Add(line);
                }
                string answer = string.Join("\n", lines);
                if (answer.Length == 0 && defaultValue != null) {
                    answer = defaultValue;
                }
                string? error = validate?.Invoke(answer);
                if (error != null) {
                    _io.WriteLine(error);
                    continue;
                }
                Confirmed(lines.Count <= 1 ? answer : PromptLabels.Format(_labels.TextAreaCounter, lines.Count, answer.Length));
                return answer;
            }
        }
        public int Select(string title, string? description, IReadOnlyList<Choice> choices, int defaultIndex, CancellationToken token) {
            if (choices.Count == 0) {
                throw new ArgumentException("There is nothing to choose from.", nameof(choices));
            }
            _io.WriteLine(title);
            WriteDescription(description);
            List<int> order = WriteChoices(choices, null);
            while (true) {
                token.ThrowIfCancellationRequested();
                string question = _labels.EnterNumber;
                if (defaultIndex >= 0) {
                    question += " (" + PromptLabels.Format(_labels.DefaultHint, order.IndexOf(defaultIndex) + 1) + ")";
                }
                _io.Write(question + ": ");
                string? line = _io.ReadLine();
                if (line == null) {
                    if (defaultIndex >= 0) {
                        return defaultIndex;
                    }
                    throw new InputClosedException();
                }
                string answer = line.Trim();
                if (answer.Length == 0 && defaultIndex >= 0) {
                    Confirmed(choices[defaultIndex].Label);
                    return defaultIndex;
                }
                if (int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) && number >= 1 && number <= order.Count) {
                    Choice chosen = choices[order[number - 1]];
                    if (!chosen.IsEnabled) {
                        _io.WriteLine(PromptLabels.Format(_labels.Disabled, chosen.DisabledReason));
                        continue;
                    }
                    Confirmed(chosen.Label);
                    return order[number - 1];
                }
                int byTitle = FindByTitle(choices, answer);
                if (byTitle >= 0) {
                    Confirmed(choices[byTitle].Label);
                    return byTitle;
                }
                _io.WriteLine(PromptLabels.Format(_labels.NotAChoice, answer));
            }
        }

        public List<int> MultiSelect(string title, string? description, IReadOnlyList<Choice> choices, int? minimum, int? maximum, CancellationToken token) {
            HashSet<int> selected = new HashSet<int>();
            for (int i = 0; i < choices.Count; i++) {
                if (choices[i].Selected && choices[i].IsEnabled) {
                    selected.Add(i);
                }
            }
            _io.WriteLine(title);
            WriteDescription(description);
            List<int> order = WriteChoices(choices, selected);
            while (true) {
                token.ThrowIfCancellationRequested();
                string question = _labels.EnterNumbers;
                if (selected.Count > 0) {
                    List<string> current = new List<string>();
                    foreach (int i in Sorted(selected)) {
                        current.Add((order.IndexOf(i) + 1).ToString(CultureInfo.InvariantCulture));
                    }
                    question += " (" + PromptLabels.Format(_labels.SelectedHint, string.Join(", ", current.ToArray())) + ")";
                }
                _io.Write(question + ": ");
                string? line = _io.ReadLine();
                if (line == null) {
                    throw new InputClosedException();
                }
                string answer = line.Trim();
                HashSet<int> result;
                if (answer.Length == 0) {
                    result = new HashSet<int>(selected);
                } else if (!TryParseNumbers(answer, order, choices, out result, out string? problem)) {
                    _io.WriteLine(problem!);
                    continue;
                }
                if (minimum.HasValue && result.Count < minimum.Value) {
                    _io.WriteLine(PromptLabels.Format(_labels.MinimumSelected, minimum.Value));
                    continue;
                }
                if (maximum.HasValue && result.Count > maximum.Value) {
                    _io.WriteLine(PromptLabels.Format(_labels.MaximumSelected, maximum.Value));
                    continue;
                }
                List<int> list = Sorted(result);
                List<string> titles = new List<string>();
                foreach (int i in list) {
                    titles.Add(choices[i].Label);
                }
                Confirmed(titles.Count == 0 ? _labels.KeyNone : string.Join(", ", titles.ToArray()));
                return list;
            }
        }

        private static List<int> Sorted(HashSet<int> set) {
            List<int> list = new List<int>(set);
            list.Sort();
            return list;
        }

        private bool TryParseNumbers(string answer, List<int> order, IReadOnlyList<Choice> choices, out HashSet<int> result, out string? problem) {
            result = new HashSet<int>();
            problem = null;
            string lower = answer.ToLowerInvariant();
            if (lower == "all" || lower == _labels.KeyAll.ToLowerInvariant()) {
                foreach (int i in order) {
                    if (choices[i].IsEnabled) {
                        result.Add(i);
                    }
                }
                return true;
            }
            if (lower == "none" || lower == _labels.KeyNone.ToLowerInvariant()) {
                return true;
            }
            foreach (string raw in answer.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)) {
                string part = raw.Trim();
                int dash = part.IndexOf('-');
                int from;
                int to;
                if (dash > 0 && int.TryParse(part.Substring(0, dash), NumberStyles.Integer, CultureInfo.InvariantCulture, out from)
                    && int.TryParse(part.Substring(dash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out to)) {
                } else if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out from)) {
                    to = from;
                } else {
                    problem = PromptLabels.Format(_labels.NotAChoice, part);
                    return false;
                }
                for (int n = Math.Min(from, to); n <= Math.Max(from, to); n++) {
                    if (n < 1 || n > order.Count) {
                        problem = PromptLabels.Format(_labels.NotAChoice, n);
                        return false;
                    }
                    Choice c = choices[order[n - 1]];
                    if (!c.IsEnabled) {
                        problem = PromptLabels.Format(_labels.Disabled, c.DisabledReason);
                        return false;
                    }
                    result.Add(order[n - 1]);
                }
            }
            return true;
        }

        // Writes the choices numbered in display order (ungrouped first, then each group under a heading) and returns the
        // choice index for each number.
        private List<int> WriteChoices(IReadOnlyList<Choice> choices, HashSet<int>? selected) {
            List<int> order = new List<int>();
            List<string> groups = new List<string>();
            for (int i = 0; i < choices.Count; i++) {
                if (string.IsNullOrEmpty(choices[i].Group)) {
                    order.Add(i);
                } else if (!groups.Contains(choices[i].Group!)) {
                    groups.Add(choices[i].Group!);
                }
            }
            int width = choices.Count.ToString(CultureInfo.InvariantCulture).Length;
            int number = 1;
            foreach (int i in order) {
                WriteChoice(choices[i], number++, width, selected?.Contains(i));
            }
            foreach (string group in groups) {
                _io.WriteLine(PromptLabels.Format(_labels.GroupHeading, group));
                for (int i = 0; i < choices.Count; i++) {
                    if (string.Equals(choices[i].Group, group, StringComparison.Ordinal)) {
                        order.Add(i);
                        WriteChoice(choices[i], number++, width, selected?.Contains(i));
                    }
                }
            }
            return order;
        }

        private void WriteChoice(Choice choice, int number, int width, bool? selected) {
            StringBuilder sb = new StringBuilder("  ");
            sb.Append(number.ToString(CultureInfo.InvariantCulture).PadLeft(width)).Append(") ");
            if (selected.HasValue) {
                sb.Append(selected.Value ? "[x] " : "[ ] ");
            }
            sb.Append(choice.Label);
            if (choice.Badge != null) {
                sb.Append(" (").Append(choice.Badge.PlainText).Append(')');
            }
            if (!choice.IsEnabled) {
                sb.Append(" (").Append(PromptLabels.Format(_labels.Unavailable, choice.DisabledReason)).Append(')');
            } else if (!string.IsNullOrEmpty(choice.Description) && _accessible) {
                sb.Append(" - ").Append(choice.Description);
            }
            _io.WriteLine(sb.ToString());
        }

        private static int FindByTitle(IReadOnlyList<Choice> choices, string answer) {
            for (int i = 0; i < choices.Count; i++) {
                if (choices[i].IsEnabled && string.Equals(choices[i].Label, answer, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }
            return -1;
        }

        private void WriteDescription(string? description) {
            if (!string.IsNullOrEmpty(description)) {
                _io.WriteLine(description!);
            }
        }

        private void Confirmed(string answer) {
            if (_accessible) {
                _io.WriteLine(PromptLabels.Format(_labels.SelectedHint, answer));
            }
        }
    }
}
