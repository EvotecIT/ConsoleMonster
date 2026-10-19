using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ConsoleMonster.Widgets {
    public partial class Table {
        // The columns as added, without preparing (Table<T> reads them while it prepares).
        private protected IReadOnlyList<TableColumn> ColumnDefinitions => _columns;
    }

    /// <summary>
    /// A <see cref="Table"/> whose rows are read from typed items (<see cref="Table.For{T}(IEnumerable{T})"/>): each
    /// column has a value selector, and every table option and fluent method applies as usual. Without columns, every
    /// public property of <typeparamref name="T"/> is a column, read without reflection over anything else. The items are
    /// read when the table is first laid out and after <see cref="Refresh"/>.
    /// <code>
    /// Table.For(servers)
    ///     .WithTitle("Servers")
    ///     .Column(s => s.Name, c => c.WithIsKey())
    ///     .Column(s => s.Ram, c => c.WithPresenter(CellPresenters.Bytes))
    ///     .WithTotals();
    /// </code>
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public sealed class Table<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T> : Table {
        private readonly IEnumerable<T> _items;
        private readonly Dictionary<TableColumn, Func<T, object?>> _values = new Dictionary<TableColumn, Func<T, object?>>();
        private readonly List<TableColumn> _inferred = new List<TableColumn>();
        private bool _dirty = true;
        private bool _preparing;

        internal Table(IEnumerable<T> items) {
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <summary>The items the rows are read from.</summary>
        public IEnumerable<T> Items => _items;

        /// <summary>
        /// Adds a column read from each item. The header comes from the expression (<c>s =&gt; s.Name</c> is "Name",
        /// <c>s =&gt; s.Address.City</c> is "City"); set another with <c>c =&gt; c.WithHeader("...")</c>.
        /// </summary>
        /// <param name="value">Reads the value from an item.</param>
        /// <param name="configure">Configures the column (priority, presenter, overflow...), or null.</param>
        /// <param name="expression">Filled in by the compiler from <paramref name="value"/>; leave it out.</param>
        /// <returns>This table.</returns>
        public Table<T> Column(Func<T, object?> value, Action<TableColumn>? configure = null, [CallerArgumentExpression(nameof(value))] string? expression = null) {
            return Column(Text.Markup.Escape(HeaderFrom(expression, ColumnDefinitions.Count - _inferred.Count)), value, configure);
        }

        /// <summary>Adds a column read from each item, with a header.</summary>
        /// <param name="header">The header (markup).</param>
        /// <param name="value">Reads the value from an item.</param>
        /// <param name="configure">Configures the column, or null.</param>
        /// <returns>This table.</returns>
        public Table<T> Column(string header, Func<T, object?> value, Action<TableColumn>? configure = null) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            // Property columns inferred by an earlier read give way to the explicit ones.
            foreach (TableColumn inferred in _inferred) {
                RemoveColumn(inferred);
                _values.Remove(inferred);
            }
            _inferred.Clear();
            TableColumn column = new TableColumn(header ?? string.Empty);
            configure?.Invoke(column);
            _values[column] = value;
            AddColumn(column);
            _dirty = true;
            return this;
        }

        /// <summary>Reads the items again at the next layout (after the collection changed).</summary>
        /// <returns>This table.</returns>
        public Table<T> Refresh() {
            _dirty = true;
            return this;
        }

        private protected override void Prepare() {
            if (_dirty && !_preparing) {
                _preparing = true;
                try {
                    if (ColumnDefinitions.Count == 0) {
                        AddPropertyColumns();
                    }
                    ClearRows();
                    IReadOnlyList<TableColumn> columns = ColumnDefinitions;
                    foreach (T item in _items) {
                        object?[] row = new object?[columns.Count];
                        for (int c = 0; c < columns.Count; c++) {
                            row[c] = _values.TryGetValue(columns[c], out Func<T, object?>? read) ? read(item) : null;
                        }
                        AddValues(row);
                    }
                    _dirty = false;
                } finally {
                    _preparing = false;
                }
            }
            base.Prepare();
        }

        // Every readable public instance property, in declaration order; a simple item type (string, number, date, Guid,
        // a nullable of one) is one column.
        private void AddPropertyColumns() {
            Type type = typeof(T);
            Type simple = Nullable.GetUnderlyingType(type) ?? type;
            if (simple.IsPrimitive || simple.IsEnum || simple == typeof(string) || simple == typeof(decimal) || simple == typeof(DateTime) || simple == typeof(DateTimeOffset)
                || simple == typeof(TimeSpan) || simple == typeof(Guid) || simple == typeof(Uri) || simple == typeof(Version)) {
                TableColumn single = new TableColumn(simple.Name);
                _values[single] = item => item;
                _inferred.Add(single);
                AddColumn(single);
                return;
            }
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Data.PropertyOrder.Sort(properties);
            foreach (PropertyInfo property in properties) {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) {
                    continue;
                }
                PropertyInfo read = property;
                TableColumn column = new TableColumn(property.Name);
                _values[column] = item => item == null ? null : read.GetValue(item, null);
                _inferred.Add(column);
                AddColumn(column);
            }
        }

        /// <summary>The header for a selector expression: the last member of a member chain, else the body.</summary>
        internal static string HeaderFrom(string? expression, int index) {
            if (string.IsNullOrWhiteSpace(expression)) {
                return "Column " + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            string text = expression!.Trim();
            int arrow = text.IndexOf("=>", StringComparison.Ordinal);
            if (arrow < 0) {
                return text;
            }
            string parameter = text.Substring(0, arrow).Trim().TrimStart('(').TrimEnd(')').Trim();
            string body = text.Substring(arrow + 2).Trim();
            if (parameter.Length > 0 && body.StartsWith(parameter + ".", StringComparison.Ordinal)) {
                body = body.Substring(parameter.Length + 1);
            }
            string chain = body.Replace("?.", ".").Replace("!", string.Empty);
            foreach (char c in chain) {
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '.')) {
                    return body;
                }
            }
            return chain.Substring(chain.LastIndexOf('.') + 1);
        }
    }
}

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices {
    /// <summary>The compiler fills the parameter with the text of another argument (C# 10; built in from .NET 5).</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute {
        public CallerArgumentExpressionAttribute(string parameterName) {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }
    }
}
#endif
