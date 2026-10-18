using System;

namespace ConsoleMonster {
    /// <summary>
    /// Leaves a widget property (or a whole type) out of the generated fluent methods. Used where a property is managed
    /// through dedicated methods and a plain setter or <c>Add</c> would bypass them. The fluent coverage test honours it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false)]
    internal sealed class NoFluentAttribute : Attribute {
    }
}
