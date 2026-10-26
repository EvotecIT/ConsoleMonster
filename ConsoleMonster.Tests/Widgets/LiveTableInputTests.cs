using System;
using System.Collections.Generic;
using ConsoleMonster.Widgets;
using Xunit;

namespace ConsoleMonster.Tests.Widgets {
    public sealed class LiveTableInputTests {
        [Fact]
        public void InputRows_UseStableKeysAndColumnKeysRatherThanDisplayHeaders() {
            var model = new Table().AddColumn(new TableColumn("Display name") { Key = "Name" }).AddColumn(new TableColumn("Count"));
            var table = new LiveTable(model) { KeyProperty = "Id" };
            table.Add(new Dictionary<string, object?> { ["Id"] = "one", ["Name"] = "first", ["Count"] = 2 });
            table.Add(new Dictionary<string, object?> { ["Id"] = "one", ["Name"] = "changed", ["Count"] = 5 });
            Assert.Equal(new[] { "one" }, table.Keys);
            Assert.Throws<ArgumentException>(() => table.Add(new Dictionary<string, object?> { ["Name"] = "no key" }));
            Assert.Equal(new[] { "one" }, table.Keys);
        }
    }
}
