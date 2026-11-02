using System;
using System.Reflection;
using ConsoleMonster.Localization;

namespace ConsoleMonster.Values {
    /// <summary>Separates quiet ordinary values from explicit comparison markers.</summary>
    internal static class ValueDisplay {
        internal static string? Marker(object? value) => value == null || value is DBNull || value is Missing ? "—"
            : value is string text && text.Length == 0 ? string.Empty : null;

        internal static string? ComparisonMarker(object? value, WidgetLabels labels) => value is Missing ? labels.ChangesMissing
            : value == null || value is DBNull ? "(null)"
            : value is string text && text.Length == 0 ? labels.ChangesEmpty : null;
    }
}
