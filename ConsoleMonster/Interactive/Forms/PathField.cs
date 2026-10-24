using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>A file or folder path with completion.</summary>
    public sealed class PathField : Field {
        /// <summary>Creates a path field.</summary>
        /// <param name="key">The key.</param>
        /// <param name="label">The label.</param>
        public PathField(string key, string? label = null) : base(key, label) {
        }

        /// <inheritdoc />
        public override FieldKind Kind => FieldKind.Path;

        /// <summary>What is accepted.</summary>
        public PathKind PathKind { get; set; } = PathKind.Any;

        /// <summary>Whether the path must exist.</summary>
        public bool MustExist { get; set; }

        /// <summary>Extensions offered as suggestions.</summary>
        public IList<string> Extensions { get; } = new List<string>();

        internal PathPrompt Prompt() {
            PathPrompt prompt = new PathPrompt(Label) { Kind = PathKind, MustExist = MustExist };
            foreach (string e in Extensions) {
                prompt.Extensions.Add(e);
            }
            return prompt;
        }

        internal override string? Check(object? value, FormValues values, PromptLabels labels) {
            string text = value as string ?? string.Empty;
            if (text.Length > 0) {
                string? problem = Prompt().Validate(text, labels);
                if (problem != null) {
                    return problem;
                }
            }
            return base.Check(value, values, labels);
        }
    }
}
