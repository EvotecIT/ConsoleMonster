using System;

namespace ConsoleMonster.Interactive {
    /// <summary>Shared question construction; existing prompts own validation, rendering and input lifetimes.</summary>
    public static partial class Question {
        /// <summary>Builds ordinary, multiline or secret text from one definition.</summary>
        /// <remarks>The typed C# prompt returns a string. The PowerShell host converts secret results to SecureString unless AsPlainText is explicit.</remarks>
        public static Prompt<string> Text(TextQuestionOptions options) {
            if (options == null) { throw new ArgumentNullException(nameof(options)); }
            if (options.MinLength < 0 || options.MaxLength < options.MinLength || options.MaxLines < 1 || options.Height < 1) { throw new ArgumentOutOfRangeException(nameof(options)); }
            if (!options.AsSecret && (options.Has(nameof(options.Hidden)) || options.Has(nameof(options.AllowEmpty)))) { throw new ArgumentException("Hidden and AllowEmpty require AsSecret.", nameof(options)); }
            Prompt<string> prompt;
            if (options.AsSecret) {
                foreach (string field in new[] { nameof(options.Default), nameof(options.Multiline), nameof(options.Placeholder), nameof(options.Required), nameof(options.MaxLength), nameof(options.Pattern), nameof(options.PatternMessage), nameof(options.PrefillDefault), nameof(options.MaxLines), nameof(options.Height) }) {
                    if (options.Has(field)) { throw new ArgumentException(field + " is not supported with AsSecret.", nameof(options)); }
                }
                if (options.History.Count != 0) { throw new ArgumentException("History is not supported with AsSecret.", nameof(options)); }
                prompt = new SecretPrompt(options.Title) { Display = options.Hidden ? SecretDisplay.Hidden : SecretDisplay.Masked, Required = !options.AllowEmpty, MinLength = options.MinLength, Validator = options.Validator };
            } else if (options.Multiline) {
                foreach (string field in new[] { nameof(options.Pattern), nameof(options.PatternMessage), nameof(options.PrefillDefault) }) {
                    if (options.Has(field)) { throw new ArgumentException(field + " is not supported with Multiline.", nameof(options)); }
                }
                if (options.History.Count != 0) { throw new ArgumentException("History is not supported with Multiline.", nameof(options)); }
                prompt = new TextAreaPrompt(options.Title) { Placeholder = options.Placeholder, Required = options.Required, MinLength = options.MinLength, MaxLength = options.MaxLength, MaxLines = options.MaxLines, Height = options.Height, Validator = options.Validator };
            } else {
                foreach (string field in new[] { nameof(options.Height), nameof(options.MaxLines) }) { if (options.Has(field)) { throw new ArgumentException(field + " requires Multiline.", nameof(options)); } }
                var text = new TextPrompt(options.Title) { Placeholder = options.Placeholder, Required = options.Required, MinLength = options.MinLength, MaxLength = options.MaxLength, Pattern = options.Pattern, PatternMessage = options.PatternMessage, PrefillDefault = options.PrefillDefault, Validator = options.Validator };
                foreach (string entry in options.History) { text.History.Add(entry); }
                prompt = text;
            }
            prompt.Description = options.Description; prompt.ParameterName = options.ParameterName;
            if (!options.AsSecret && options.Has(nameof(options.Default))) { prompt.Default = options.Default ?? string.Empty; }
            return prompt;
        }
    }
}
