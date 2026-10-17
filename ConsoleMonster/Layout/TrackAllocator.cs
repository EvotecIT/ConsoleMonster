using System;
using System.Collections.Generic;

namespace ConsoleMonster.Layout {
    /// <summary>
    /// Distributes a length (width for columns, height for rows) between tracks with <see cref="LayoutSize"/>
    /// specifications. Fixed tracks first, then auto tracks in order, then fractions share what is left. When the
    /// space is too small, fractions shrink to their minimum, then auto tracks shrink from the last one.
    /// </summary>
    internal static class TrackAllocator {
        public static int[] Allocate(int available, IReadOnlyList<LayoutSize> specs, int gap, Func<int, int, int> measureAuto, bool fractionsAsAuto) {
            int count = specs.Count;
            int[] sizes = new int[count];
            if (count == 0) {
                return sizes;
            }
            int space = Math.Max(0, available - (gap * (count - 1)));
            int used = 0;
            for (int i = 0; i < count; i++) {
                if (specs[i].Kind == LayoutSizeKind.Fixed) {
                    sizes[i] = (int)specs[i].Value;
                    used += sizes[i];
                }
            }
            for (int i = 0; i < count; i++) {
                LayoutSize spec = specs[i];
                if (spec.Kind == LayoutSizeKind.Auto || (fractionsAsAuto && spec.Kind == LayoutSizeKind.Fraction)) {
                    int remaining = Math.Max(0, space - used);
                    int desired = Math.Max(spec.Min, measureAuto(i, remaining));
                    sizes[i] = Math.Min(desired, Math.Max(remaining, spec.Min));
                    used += sizes[i];
                }
            }
            if (!fractionsAsAuto) {
                double weights = 0;
                for (int i = 0; i < count; i++) {
                    if (specs[i].Kind == LayoutSizeKind.Fraction) {
                        weights += specs[i].Value;
                    }
                }
                if (weights > 0) {
                    int left = Math.Max(0, space - used);
                    int assigned = 0;
                    int lastFraction = -1;
                    for (int i = 0; i < count; i++) {
                        if (specs[i].Kind != LayoutSizeKind.Fraction) {
                            continue;
                        }
                        int share = (int)Math.Floor(left * (specs[i].Value / weights));
                        sizes[i] = Math.Max(specs[i].Min, share);
                        assigned += sizes[i];
                        lastFraction = i;
                    }
                    if (lastFraction >= 0 && assigned < left) {
                        sizes[lastFraction] += left - assigned;
                    }
                    used += Math.Max(assigned, left);
                    used = Sum(sizes);
                }
            }
            int overflow = Sum(sizes) - space;
            if (overflow > 0) {
                for (int i = count - 1; i >= 0 && overflow > 0; i--) {
                    if (specs[i].Kind == LayoutSizeKind.Fraction) {
                        int reducible = sizes[i] - Math.Min(sizes[i], specs[i].Min);
                        int take = Math.Min(reducible, overflow);
                        sizes[i] -= take;
                        overflow -= take;
                    }
                }
                for (int i = count - 1; i >= 0 && overflow > 0; i--) {
                    if (specs[i].Kind != LayoutSizeKind.Fixed) {
                        int take = Math.Min(sizes[i], overflow);
                        sizes[i] -= take;
                        overflow -= take;
                    }
                }
                for (int i = count - 1; i >= 0 && overflow > 0; i--) {
                    int take = Math.Min(sizes[i], overflow);
                    sizes[i] -= take;
                    overflow -= take;
                }
            }
            return sizes;
        }

        public static int Sum(int[] values) {
            int total = 0;
            foreach (int v in values) {
                total += v;
            }
            return total;
        }
    }
}
