using System;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Diagrams {
    /// <summary>Eight runnable diagram examples, from a home network to use cases and a team hierarchy.</summary>
    public static partial class DiagramLadder {
        /// <summary>Selects the initializer or fluent version of a ladder level.</summary>
        public static Diagram Create(int level, bool fluent = false) {
            switch (level) {
                case 1: return fluent ? HelloFluent() : HelloInitializer();
                case 2: return fluent ? LabelsFluent() : LabelsInitializer();
                case 3: return fluent ? GroupsFluent() : GroupsInitializer();
                case 4: return fluent ? StatusFluent() : StatusInitializer();
                case 5: return fluent ? WebAppFluent() : WebAppInitializer();
                case 6: return fluent ? LivePingFluent() : LivePingInitializer();
                case 7: return fluent ? UseCaseFluent() : UseCaseInitializer();
                case 8: return fluent ? OrgChartFluent() : OrgChartInitializer();
                default: throw new ArgumentOutOfRangeException(nameof(level), "Choose a diagram ladder level from 1 to 8.");
            }
        }
    }
}
