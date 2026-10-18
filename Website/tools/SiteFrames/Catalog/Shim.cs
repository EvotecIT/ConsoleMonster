using System.Collections.Generic;
using ConsoleMonster.Layout;

namespace SiteFrames.Catalog {
    /// <summary>
    /// Stands in for <c>ConsoleMonster.Terminal</c> inside the catalog, so each example's code reads exactly as a program would
    /// write it (<c>Terminal.Write(widget)</c>) while the site tool collects the widget and renders it at every tier.
    /// </summary>
    internal static class Terminal {
        private static readonly List<IWidget> Written = new List<IWidget>();

        public static void Write(IWidget widget) => Written.Add(widget);

        /// <summary>Runs an example and returns what it wrote (several writes become rows).</summary>
        public static IWidget Capture(System.Action example) {
            Written.Clear();
            example();
            IWidget result = Written.Count == 1 ? Written[0] : new Rows(Written.ToArray());
            Written.Clear();
            return result;
        }
    }
}
