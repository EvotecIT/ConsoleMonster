using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ConsoleMonster.PowerShell.Display;

internal static class DisplayData {
    internal static bool IsScalar(object value) {
        Type type = value.GetType();
        return value is string || type.IsPrimitive || type.IsEnum || value is decimal || value is DateTime
            || value is DateTimeOffset || value is TimeSpan || value is Guid;
    }
    // Read only materialized fields and compiler-generated auto-properties. Never call arbitrary or lazy getters.
    internal static object? Read(object value, string name) {
        Type type = value.GetType();
        FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
        if (field != null) { return field.GetValue(value); }
        PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property == null || property.GetIndexParameters().Length != 0 || property.GetGetMethod() == null) { return null; }
        FieldInfo? backing = property.DeclaringType?.GetField("<" + property.Name + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        return backing != null && backing.IsDefined(typeof(CompilerGeneratedAttribute), false) ? backing.GetValue(value) : null;
    }
    internal static string Text(object? value, CultureInfo? culture = null) {
        if (value == null) { return string.Empty; }
        Type type = value.GetType();
        if (value is string text) { return text; }
        if (IsScalar(value)) {
            return Convert.ToString(value, culture ?? CultureInfo.InvariantCulture) ?? string.Empty;
        }
        return type.Name; // Do not invoke an application-defined ToString.
    }
    internal static string Scope(object value, IReadOnlyList<string> properties) {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        foreach (string name in properties) {
            object? raw = Read(value, name);
            string text = Text(raw);
            string type = raw?.GetType().FullName ?? string.Empty;
            result.Append(type.Length).Append(':').Append(type).Append(text.Length).Append(':').Append(text);
        }
        return result.ToString();
    }
    internal static long Estimate(object value, long ceiling) {
        HashSet<object> seen = new HashSet<object>(ReferenceComparer.Instance);
        int visited = 0;
        return Estimate(value, ceiling, seen, ref visited, 0);
    }
    private static long Estimate(object? value, long ceiling, HashSet<object> seen, ref int visited, int depth) {
        if (value == null) { return 8; }
        if (++visited > 8192 || depth > 12) { return ceiling + 1; }
        if (value is string text) { return 24L + text.Length * 2L; }
        Type type = value.GetType();
        if (IsScalar(value)) { return 64; }
        if (!seen.Add(value)) { return 8; }
        long size = 64;
        // Only arrays have a guaranteed non-executing enumerator. Other containers are inspected through fields.
        if (value is Array array) {
            foreach (object? item in array) {
                size += Estimate(item, ceiling, seen, ref visited, depth + 1);
                if (size > ceiling) { return ceiling + 1; }
            }
        } else {
            for (Type? current = type; current != null; current = current.BaseType) {
                foreach (FieldInfo field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                    size += Estimate(field.GetValue(value), ceiling, seen, ref visited, depth + 1);
                    if (size > ceiling) { return ceiling + 1; }
                }
            }
        }
        return size;
    }
    internal sealed class ReferenceComparer : IEqualityComparer<object> {
        internal static readonly ReferenceComparer Instance = new ReferenceComparer();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
