using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Layout;

namespace ConsoleMonster.Interactive {
    /// <summary>A secret: masked in the form, never shown on the review page, never stored, a placeholder in the command.</summary>
    public sealed class SecretField : Field {
        /// <summary>Creates a secret field.</summary>
        /// <param name="name">The key.</param>
        /// <param name="label">The label.</param>
        public SecretField(string name, string? label = null) : base(name, label) {
            Remember = false;
            Required = true;
        }

        /// <inheritdoc />
        public override FieldKind Kind => FieldKind.Secret;

        /// <summary>What stands in for the secret in the equivalent command (PowerShell: <c>(Read-Host -AsSecureString)</c>).</summary>
        public string CommandPlaceholder { get; set; } = "<secret>";

        /// <summary>The fewest characters.</summary>
        public int MinLength { get; set; }

        /// <inheritdoc />
        public override string Display(object? value, PromptLabels labels) => IsEmpty(value) ? string.Empty : "••••••••";

        internal override string? Check(object? value, FormValues values, PromptLabels labels) {
            string text = value as string ?? string.Empty;
            if (text.Length > 0 && text.Length < MinLength) {
                return PromptLabels.Format(labels.MinimumLength, MinLength);
            }
            return base.Check(value, values, labels);
        }

        internal override object? ArgumentValue(object? value) => IsEmpty(value) ? null : new CommandLiteral(CommandPlaceholder);

        internal override object? ToStored(object? value) => null;

        internal override bool TryRestore(object? stored, out object? value) {
            value = null;
            return false;
        }
    }
}
