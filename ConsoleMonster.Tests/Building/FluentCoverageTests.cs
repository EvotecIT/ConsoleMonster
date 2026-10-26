using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ConsoleMonster.Layout;
using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Building {
    /// <summary>
    /// Every public widget property has its generated fluent counterpart: <c>X</c> for an ordinary setter,
    /// <c>Configure</c> for callbacks and
    /// <c>AddX</c> for a mutable collection, on the widgets and on the option types they hold. A new property without one
    /// means the generator skipped it; mark it <c>[NoFluent]</c> only when a setter or Add would bypass the widget.
    /// </summary>
    public sealed class FluentCoverageTests {
        private static readonly Assembly Core = typeof(IWidget).Assembly;

        private static readonly MethodInfo[] Extensions = Core.GetExportedTypes()
            .Where(t => t.IsAbstract && t.IsSealed && t.Name.EndsWith("Fluent", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.IsDefined(typeof(ExtensionAttribute), false))
            .ToArray();

        public static IEnumerable<object[]> FluentTypes() => Types().Select(t => new object[] { t.FullName! });

        [Theory]
        [MemberData(nameof(FluentTypes))]
        public void EveryPublicPropertyHasAFluentMethod(string typeName) {
            Type type = Core.GetType(typeName, true)!;
            List<string> missing = new List<string>();
            foreach (PropertyInfo property in Properties(type)) {
                if (IsSettable(property) && typeof(Delegate).IsAssignableFrom(property.PropertyType)) {
                    Assert.True(Extensions.Any(method => method.Name == "Configure" && Receives(method, type)), type.Name + " needs Configure for " + property.Name);
                    Assert.DoesNotContain(Extensions, method => method.Name == property.Name && Receives(method, type));
                    continue;
                }
                if (IsSettable(property) && !Has(type, property.Name, property.PropertyType)) {
                    missing.Add(property.Name + "(" + property.PropertyType.Name + ")");
                }
                if (IsSettable(property) && property.PropertyType == typeof(bool)) {
                    Assert.Contains(Extensions, method => method.Name == property.Name && Receives(method, type) && method.GetParameters().Length == 1);
                }
                Type? item = CollectionItem(property.PropertyType);
                if (item != null) {
                    if (!Extensions.Any(m => m.Name.StartsWith("Add", StringComparison.Ordinal) && Receives(m, type) && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == item)) {
                        missing.Add("Add...(" + item.Name + ") for " + property.Name);
                    }
                    if (!Has(type, "Add" + property.Name, typeof(IEnumerable<>).MakeGenericType(item))) {
                        missing.Add("Add" + property.Name + "(IEnumerable<" + item.Name + ">)");
                    }
                }
            }
            Assert.True(missing.Count == 0, type.Name + " lacks " + string.Join(", ", missing));
        }

        [Fact]
        public void CoverageIncludesWidgetsAndTheirOptionTypes() {
            List<Type> types = Types().ToList();
            Assert.Contains(typeof(Table), types);
            Assert.Contains(typeof(TableColumn), types);
            Assert.Contains(typeof(Panel), types);
            Assert.Contains(typeof(ConsoleMonster.Interactive.TextQuestionOptions), types);
            Assert.Contains(typeof(ConsoleMonster.Live.LiveRunOptions), types);
            Assert.Contains(typeof(Dashboard), types);
            Assert.True(types.Count >= 35, "Only " + types.Count + " fluent types were found.");
        }

        [Fact]
        public void GeneratedMethods_SetAndAddAndReturnTheSameInstance() {
            HeaderBar header = new HeaderBar("Build");
            Assert.Same(header, header.Subtitle("nightly").ShowElapsed().AddCrumb("Tasks").AddCrumbs("Review", "Run"));
            Assert.Equal("nightly", header.Subtitle);
            Assert.True(header.ShowElapsed);
            Assert.Equal(new[] { "Tasks", "Review", "Run" }, header.Crumbs);
            Assert.False(header.ShowElapsed(false).ShowElapsed);
        }

        [Fact]
        public void ConfigureAssignsCallbacksWithoutInvokingThemAndRetainsGenericReceivers() {
            int calls = 0;
            var options = new SelectQuestionOptions().Configure(value => value.Preview = _ => { calls++; return new TextBlock("Ready"); });
            Assert.Equal(0, calls);
            Assert.NotNull(options.Preview!(null));
            Assert.Equal(1, calls);
            var prompt = new SelectPrompt<int>("Number", new[] { 1, 2 }).Configure(value => value.Preview = number => new TextBlock(number.ToString()));
            Assert.IsType<SelectPrompt<int>>(prompt);
            Assert.NotNull(prompt.Preview);
        }

        [Fact]
        public void GeneratedMethods_ConfigureNestedOptionsAndCreateNullableCollections() {
            Table table = new Table().Presentation(p => p.Culture = System.Globalization.CultureInfo.InvariantCulture);
            Assert.Same(System.Globalization.CultureInfo.InvariantCulture, table.Presentation.Culture);
            ProgressBar bar = new ProgressBar("copy").AddColumns(ProgressColumns.Percent(), ProgressColumns.Elapsed());
            Assert.Equal(2, bar.Columns!.Count);
        }

        [Fact]
        public void GenericSelectionPromptsKeepTheirValueAndConcreteTypes() {
            var single = new ConsoleMonster.Interactive.SelectPrompt<int>("Number", new[] { 1, 2 }).Description("Pick one").Default(2).PageSize(5);
            Assert.IsType<ConsoleMonster.Interactive.SelectPrompt<int>>(single);
            Assert.Equal(2, single.Default);
            var multiple = new ConsoleMonster.Interactive.MultiSelectPrompt<int>("Numbers", new[] { 1, 2 }).Minimum(1).Maximum(2).PageSize(5);
            Assert.IsType<ConsoleMonster.Interactive.MultiSelectPrompt<int>>(multiple);
            Assert.Equal(1, multiple.Minimum);
        }

        [Fact]
        public void GeneratedMethods_OnATypedTable_KeepItsType() {
            Table<int> table = Table.For(new[] { 1, 2 }).Title("Numbers").ShowHeader(false);
            Assert.Equal("Numbers", table.Title);
            Assert.IsType<Table<int>>(table.Column("Value", n => n));
        }

        // Widgets, then the option types their properties hold or their Add/Set methods take, as the generator walks them.
        private static IEnumerable<Type> Types() {
            List<Type> widgets = Core.GetExportedTypes().Where(t => IsCandidate(t) && (typeof(IWidget).IsAssignableFrom(t) || typeof(Field).IsAssignableFrom(t) || new[] { "Choice", "ConsoleTextLayout", "ConsoleLogFile" }.Contains(t.Name) || t.Name.EndsWith("Options", StringComparison.Ordinal) || IsPrompt(t) || new[] { "MenuItem", "MenuRunResult", "ReviewModel", "FormPreset", "MenuPreset", "ObjectExplorer", "Grid", "WidgetUpdate", "RunUpdate" }.Contains(t.Name))).OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
            HashSet<Type> seen = new HashSet<Type>(widgets);
            List<Type> result = new List<Type>(widgets);
            List<Type> frontier = widgets;
            while (frontier.Count > 0) {
                List<Type> next = new List<Type>();
                foreach (Type type in frontier) {
                    IEnumerable<Type> reached = Properties(type).SelectMany(p => new[] { p.PropertyType, CollectionItem(p.PropertyType) }).Where(t => t != null).Select(t => t!)
                        .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                            .Where(m => !m.IsSpecialName && (m.Name.StartsWith("Add", StringComparison.Ordinal) || m.Name.StartsWith("Set", StringComparison.Ordinal)))
                            .SelectMany(m => m.GetParameters().Select(p => p.ParameterType)));
                    foreach (Type candidate in reached) {
                        if (IsOptionType(candidate) && seen.Add(candidate)) {
                            next.Add(candidate);
                        }
                    }
                }
                result.AddRange(next.OrderBy(t => t.FullName, StringComparer.Ordinal));
                frontier = next;
            }
            return result;
        }

        private static bool IsPrompt(Type type) {
            for (Type? current = type.BaseType; current != null; current = current.BaseType) {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(ConsoleMonster.Interactive.Prompt<>)) { return true; }
            }
            return false;
        }

        private static bool IsCandidate(Type type) => type.IsClass && type.IsPublic && (!type.IsAbstract || type == typeof(Field)) && !type.IsGenericType && !type.IsGenericParameter && !NoFluent(type) && !typeof(Delegate).IsAssignableFrom(type);

        private static bool IsOptionType(Type type) => type.Assembly == Core && IsCandidate(type) && Properties(type).Any(p => IsSettable(p) || CollectionItem(p.PropertyType) != null);

        private static IEnumerable<PropertyInfo> Properties(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0 && !NoFluent(p) && !p.IsDefined(typeof(ObsoleteAttribute), true))
                .GroupBy(p => p.Name).Select(g => g.First());

        private static bool IsSettable(PropertyInfo property) {
            MethodInfo? setter = property.GetSetMethod();
            return setter != null && !setter.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
        }

        private static Type? CollectionItem(Type type) {
            if (type == typeof(string) || type.IsArray) {
                return null;
            }
            IEnumerable<Type> interfaces = type.IsInterface ? new[] { type }.Concat(type.GetInterfaces()) : type.GetInterfaces();
            Type? item = null;
            foreach (Type candidate in interfaces.Where(i => i.IsGenericType)) {
                Type definition = candidate.GetGenericTypeDefinition();
                if (definition == typeof(IDictionary<,>)) {
                    return null;
                }
                if (definition == typeof(ICollection<>)) {
                    item = candidate.GetGenericArguments()[0];
                }
            }
            return item;
        }

        private static bool NoFluent(MemberInfo member) => member.GetCustomAttributes(false).Any(a => a.GetType().Name == "NoFluentAttribute");

        private static bool Has(Type type, string name, Type parameter) =>
            Extensions.Any(m => m.Name == name && Receives(m, type) && m.GetParameters().Length >= 2 && m.GetParameters()[1].ParameterType == parameter);

        // The first parameter is the type itself, or a type parameter constrained to it or a base (Table's generic methods).
        private static bool Receives(MethodInfo method, Type type) {
            Type first = method.GetParameters()[0].ParameterType;
            return first == type || (first.IsGenericParameter && first.GetGenericParameterConstraints().Any(constraint => constraint.IsAssignableFrom(type)));
        }
    }
}
