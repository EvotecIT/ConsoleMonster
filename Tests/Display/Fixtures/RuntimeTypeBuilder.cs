using System;
using System.Collections.Generic;
using ConsoleMonster.Layout;
using ConsoleMonster.PowerShell.Display;
using ConsoleMonster.Widgets;

namespace DisplayContract {
    public static class RuntimeTypeBuilder {
        public static Func<IReadOnlyList<object>, DefaultDisplayContext, IWidget> Create<T>() where T : class {
            return (objects, context) => {
                T value = (T)objects[0];
                return TextBlock.Plain(value.GetType().Assembly.GetName().Name);
            };
        }
    }
}
