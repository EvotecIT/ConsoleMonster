using System;
using System.Reflection;
using ConsoleMonster.Localization;

namespace ConsoleMonster.Values {
    /// <summary>Distinguishes absent fields, null values and empty strings at the shared display boundary.</summary>
    internal static class ValueDisplay {
        internal static string? Marker(object? value, WidgetLabels labels) => value is Missing ? labels.ChangesMissing
            : value == null || value is DBNull ? "(null)"
            : value is string text && text.Length == 0 ? labels.ChangesEmpty : null;
    }
}
