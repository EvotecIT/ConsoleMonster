using System;
using System.Globalization;
using System.Resources;

namespace ConsoleMonster.Markdown {
    /// <summary>Owns adapter labels and diagnostics independently of the dependency-free rendering core.</summary>
    internal static class MarkdownMessages {
        private static readonly ResourceManager Resources = new ResourceManager("ConsoleMonster.Markdown.MarkdownMessages", typeof(MarkdownMessages).Assembly);

        internal static string Get(string key) => Resources.GetString(key, CultureInfo.CurrentUICulture) ?? throw new InvalidOperationException(key);

        internal static string Format(string key, string value) => string.Format(CultureInfo.CurrentCulture, Get(key), value);
    }
}
