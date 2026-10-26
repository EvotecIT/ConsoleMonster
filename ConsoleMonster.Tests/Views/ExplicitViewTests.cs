using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.Testing;
using ConsoleMonster.Views;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Views {
    public sealed class ExplicitViewTests {
        [Fact]
        public void Json_UsesTheSharedCodeOptionsForTextAndStructuredInput() {
            var options = new ConsoleViewOptions { View = ConsoleViewKind.Json, LineNumbers = true, FirstLine = 4, Title = "Result" };
            CodeView structured = Assert.IsType<CodeView>(ConsoleView.Auto(new Dictionary<string, object?> { ["Result"] = 42 }, options));
            Assert.True(structured.LineNumbers);
            Assert.Equal(4, structured.FirstLine);
            Assert.Equal("Result", structured.Title);
            Assert.Contains("42", structured.Text);
            CodeView text = Assert.IsType<CodeView>(ConsoleView.Auto("{\"Result\":42}", options));
            Assert.Equal(text.Text, structured.Text);
        }

        [Fact]
        public void ExplicitList_ListsDictionaryKeysAndHonorsPropertySelection() {
            var record = new Dictionary<string, object?> { ["Name"] = "build", ["Result"] = 42, ["Empty"] = null };
            IWidget widget = ConsoleView.Auto(record, new ConsoleViewOptions { View = ConsoleViewKind.List, Property = new[] { "Result" } });
            string text = TestTerminal.RenderText(widget, 80);
            Assert.Contains("Result", text);
            Assert.Contains("42", text);
            Assert.DoesNotContain("build", text);
            Assert.DoesNotContain("Empty", text);
        }

        [Fact]
        public void Errors_KeepPresentationOptionsAndMultipleInputOrder() {
            IWidget widget = ConsoleView.Auto(new object?[] { new InvalidOperationException("first"), new ExceptionInfo("Failure", "second") },
                new ConsoleViewOptions { View = ConsoleViewKind.Error, ShowStack = false, ShowSource = false });
            Rows rows = Assert.IsType<Rows>(widget);
            Assert.Equal(2, rows.Children.Count);
            Assert.False(Assert.IsType<ExceptionView>(rows.Children[0]).ShowStack);
            string text = TestTerminal.RenderText(rows, 80);
            Assert.True(text.IndexOf("first", StringComparison.Ordinal) < text.IndexOf("second", StringComparison.Ordinal));
        }

        [Fact]
        public void Markdown_RequiresAnExplicitAddOnAndNamesItsPackage() {
            var options = new ConsoleViewOptions { View = ConsoleViewKind.Markdown };
            Assert.Contains("ConsoleMonster.Markdown", Assert.Throws<InvalidOperationException>(() => ConsoleView.Auto("# Build", options)).Message);
            options.Provider = new Provider();
            Assert.Equal("rendered", TestTerminal.RenderText(ConsoleView.Auto("# Build", options), 80).Trim());
        }

        private sealed class Provider : IConsoleViewProvider {
            public IWidget Create(IReadOnlyList<object?> items, ConsoleViewOptions options) => new TextBlock("rendered");
        }
    }
}
