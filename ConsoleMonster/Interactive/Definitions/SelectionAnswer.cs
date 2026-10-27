using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleMonster.Interactive {
    // Converts a selection answer to the caller's value type without reflection, so typed endings stay AOT-safe.
    internal static class SelectionAnswer {
        // The kind is checked before the question opens, so a wrong ending never asks the user first.
        internal static SelectQuestionOptions Expect(SelectQuestionOptions question, bool multiple) {
            if (question == null) { throw new ArgumentNullException(nameof(question)); }
            if (question.Multiple && !multiple) { throw new InvalidOperationException("A Multiple question returns several answers; use ShowMany<T>()."); }
            if (!question.Multiple && multiple) { throw new InvalidOperationException("ShowMany<T>() needs Multiple; use Show<T>() for one answer."); }
            return question;
        }

        internal static T One<T>(object? answer) {
            if (answer is T value) { return value; }
            if (answer == null && default(T) == null) { return default!; }
            throw new InvalidCastException("The answer is " + (answer?.GetType().Name ?? "null") + ", not " + typeof(T).Name + ".");
        }

        internal static IReadOnlyList<T> Many<T>(object? answer) {
            var values = new List<T>();
            foreach (object? item in (IEnumerable)(answer ?? Array.Empty<object>())) {
                if (item is T value) { values.Add(value); }
                else if (item == null && default(T) == null) { values.Add(default!); }
                else { throw new InvalidCastException("A chosen value is " + item!.GetType().Name + ", not " + typeof(T).Name + "."); }
            }
            return values;
        }
    }
}
