using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ConsoleMonster.Widgets {
    /// <summary>
    /// Weekday and month names for calendars. The operating system's globalization data differs between backends (ICU on macOS
    /// and Linux, NLS or another ICU build on Windows): shortest day names are "Mo" on one and "M" on another, and German
    /// abbreviations gain a period. So the languages ConsoleMonster ships labels for (English, Polish, German, Spanish, French
    /// and Italian) use the names below, and any other culture uses its own data normalised the same way: two-cell weekday
    /// labels from the abbreviated names and short month names without a trailing period.
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

        /// <summary>The two-cell weekday label.</summary>
        public static string Day(CultureInfo culture, DayOfWeek day) {
            if (Builtin.TryGetValue(culture.TwoLetterISOLanguageName, out Names? names)) {
                return names.Days[(int)day];
            }
            string abbreviated = TrimPeriod(culture.DateTimeFormat.GetAbbreviatedDayName(day));
            string label = TextElements(abbreviated, 2);
            return label.Length == 0 ? TextElements(day.ToString(), 2) : char.ToUpper(label[0], culture) + label.Substring(1);
        }

        /// <summary>The month's name, for a calendar title.</summary>
        public static string Month(CultureInfo culture, DateTimeFormatInfo gregorian, int month) =>
            Builtin.TryGetValue(culture.TwoLetterISOLanguageName, out Names? names) ? names.Months[month - 1] : gregorian.GetMonthName(month);

        /// <summary>The month's short name, without a trailing period.</summary>
        public static string ShortMonth(CultureInfo culture, DateTimeFormatInfo gregorian, int month) =>
            Builtin.TryGetValue(culture.TwoLetterISOLanguageName, out Names? names) ? names.ShortMonths[month - 1] : TrimPeriod(gregorian.GetAbbreviatedMonthName(month));

        private static string TrimPeriod(string text) => text.Trim().TrimEnd('.');

        // The first count text elements, so a combining mark or surrogate pair is never split.
        private static string TextElements(string text, int count) {
            StringBuilder result = new StringBuilder();
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(text);
            for (int taken = 0; taken < count && elements.MoveNext(); taken++) {
                result.Append(elements.GetTextElement());
            }
            return result.ToString();
        }
    }
}
