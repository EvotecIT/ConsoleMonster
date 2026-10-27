using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ConsoleMonster.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Weekday and month names for calendars. The operating system's globalization data differs between backends (ICU on macOS
    /// and Linux, NLS or another ICU build on Windows): shortest day names are "Mo" on one and "M" on another, and German
    /// abbreviations gain a period. So the languages ConsoleMonster ships labels for (English, Polish, German, Spanish, French
    /// and Italian) use the names below. Other cultures use abbreviated names without their shared prefix, fitting whole
    /// text elements into the three-cell weekday header, and short month names without a trailing period.
    /// </summary>
    internal static class CalendarNames {
        private sealed class Names {
            public Names(string days, string months, string shortMonths) {
                Days = days.Split(' ');
                Months = months.Split(' ');
                ShortMonths = shortMonths.Split(' ');
            }

            // Sunday first, as DayOfWeek counts.
            public string[] Days { get; }
            public string[] Months { get; }
            public string[] ShortMonths { get; }
        }

        private static readonly Dictionary<string, Names> Builtin = new Dictionary<string, Names>(StringComparer.OrdinalIgnoreCase) {
            ["en"] = new Names("Su Mo Tu We Th Fr Sa",
                "January February March April May June July August September October November December",
                "Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec"),
            ["pl"] = new Names("Nd Pn Wt Śr Cz Pt So",
                "styczeń luty marzec kwiecień maj czerwiec lipiec sierpień wrzesień październik listopad grudzień",
                "sty lut mar kwi maj cze lip sie wrz paź lis gru"),
            ["de"] = new Names("So Mo Di Mi Do Fr Sa",
                "Januar Februar März April Mai Juni Juli August September Oktober November Dezember",
                "Jan Feb Mär Apr Mai Jun Jul Aug Sep Okt Nov Dez"),
            ["es"] = new Names("Do Lu Ma Mi Ju Vi Sá",
                "enero febrero marzo abril mayo junio julio agosto septiembre octubre noviembre diciembre",
                "ene feb mar abr may jun jul ago sep oct nov dic"),
            ["fr"] = new Names("Di Lu Ma Me Je Ve Sa",
                "janvier février mars avril mai juin juillet août septembre octobre novembre décembre",
                "janv févr mars avr mai juin juil août sept oct nov déc"),
            ["it"] = new Names("Do Lu Ma Me Gi Ve Sa",
                "gennaio febbraio marzo aprile maggio giugno luglio agosto settembre ottobre novembre dicembre",
                "gen feb mar apr mag giu lug ago set ott nov dic")
        };

        /// <summary>A distinct weekday label that fits the calendar's three-cell header.</summary>
        public static string Day(CultureInfo culture, DayOfWeek day) {
            if (Builtin.TryGetValue(culture.TwoLetterISOLanguageName, out Names? names)) {
                return names.Days[(int)day];
            }
            string[] abbreviated = new string[7];
            for (int index = 0; index < abbreviated.Length; index++) {
                abbreviated[index] = TrimPeriod(culture.DateTimeFormat.GetAbbreviatedDayName((DayOfWeek)index));
            }
            if (TryDayLabels(culture, abbreviated, 2, out string[] labels) || TryDayLabels(culture, abbreviated, 3, out labels)) {
                return labels[(int)day];
            }
            string[] shortest = new string[7];
            for (int index = 0; index < shortest.Length; index++) {
                shortest[index] = TrimPeriod(culture.DateTimeFormat.GetShortestDayName((DayOfWeek)index));
            }
            // Truncation may remove distinguishing letters or digits (Portuguese and Vietnamese, for example).
            // Prefer another complete localized set before handling inherently ambiguous custom culture data.
            return TryDayLabels(culture, shortest, 3, out labels) ? labels[(int)day] : Builtin["en"].Days[(int)day];
        }

        private static bool TryDayLabels(CultureInfo culture, string[] names, int count, out string[] labels) {
            int prefix = SharedPrefixLength(names);
            labels = new string[7];
            HashSet<string> distinct = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < labels.Length; index++) {
                string remainder = names[index].Substring(prefix);
                string label = TextElements(remainder, count);
                if (label.Length > 0) {
                    string initial = StringInfo.GetNextTextElement(label);
                    label = culture.TextInfo.ToUpper(initial) + label.Substring(initial.Length);
                }
                if (label.Length == 0 || DisplayWidth.Measure(label) > 3 || !distinct.Add(label)) { return false; }
                labels[index] = label;
            }
            return true;
        }

        // Compare whole text elements so a shared base character never leaves a detached combining mark.
        private static int SharedPrefixLength(string[] names) {
            int length = 0;
            while (length < names[0].Length) {
                string element = StringInfo.GetNextTextElement(names[0], length);
                for (int index = 1; index < names.Length; index++) {
                    if (length >= names[index].Length || StringInfo.GetNextTextElement(names[index], length) != element) {
                        return length;
                    }
                }
                length += element.Length;
            }
            return length;
        }

        /// <summary>The month's name, for a calendar title.</summary>
        public static string Month(CultureInfo culture, DateTimeFormatInfo gregorian, int month) =>
            Builtin.TryGetValue(culture.TwoLetterISOLanguageName, out Names? names) ? names.Months[month - 1] : gregorian.GetMonthName(month);

        /// <summary>The month's short name, without a trailing period.</summary>
        public static string ShortMonth(CultureInfo culture, DateTimeFormatInfo gregorian, int month) =>
            Builtin.TryGetValue(culture.TwoLetterISOLanguageName, out Names? names) ? names.ShortMonths[month - 1] : TrimPeriod(gregorian.GetAbbreviatedMonthName(month));

        private static string TrimPeriod(string text) => text.Trim().TrimEnd('.');

        // Keep whole text elements within the header's cell budget, including wide CJK glyphs.
        private static string TextElements(string text, int count) {
            StringBuilder result = new StringBuilder();
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text);
            for (int taken = 0; taken < count && elements.MoveNext(); taken++) {
                string element = elements.GetTextElement();
                if (DisplayWidth.Measure(result.ToString()) + DisplayWidth.Measure(element) > 3) { break; }
                result.Append(element);
            }
            return result.ToString();
        }
    }
}
