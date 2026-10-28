using System;
using System.Collections.Generic;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;

namespace ConsoleMonster.Presenters {
    /// <summary>
    /// Enum and text values that read as a status (Running, Stopped, Healthy, Failed, Degraded...) shown with a status
    /// marker and colour, keeping the value's own word: <c>✓ Running</c>, <c>○ Stopped</c>, <c>✗ Failed</c>. Words it does not
    /// know show as plain text.
    /// </summary>
    public sealed class StatusEnumPresenter : ICellPresenter {
        private static readonly Dictionary<string, StatusKind> Words = new Dictionary<string, StatusKind>(StringComparer.OrdinalIgnoreCase) {
            ["running"] = StatusKind.Pass, ["started"] = StatusKind.Pass, ["enabled"] = StatusKind.Pass, ["online"] = StatusKind.Pass,
            ["up"] = StatusKind.Pass, ["healthy"] = StatusKind.Pass, ["ok"] = StatusKind.Pass, ["success"] = StatusKind.Pass,
            ["succeeded"] = StatusKind.Pass, ["completed"] = StatusKind.Pass, ["active"] = StatusKind.Pass, ["connected"] = StatusKind.Pass,
            ["ready"] = StatusKind.Pass, ["passed"] = StatusKind.Pass, ["pass"] = StatusKind.Pass, ["true"] = StatusKind.Pass,
            ["stopped"] = StatusKind.Skip, ["disabled"] = StatusKind.Skip, ["inactive"] = StatusKind.Skip, ["skipped"] = StatusKind.Skip,
            ["none"] = StatusKind.Skip, ["notstarted"] = StatusKind.Skip, ["idle"] = StatusKind.Skip,
            ["failed"] = StatusKind.Fail, ["error"] = StatusKind.Fail, ["offline"] = StatusKind.Fail, ["down"] = StatusKind.Fail,
            ["unhealthy"] = StatusKind.Fail, ["critical"] = StatusKind.Fail, ["disconnected"] = StatusKind.Fail, ["fail"] = StatusKind.Fail,
            ["false"] = StatusKind.Fail,
            ["warning"] = StatusKind.Warn, ["degraded"] = StatusKind.Warn, ["paused"] = StatusKind.Warn, ["pending"] = StatusKind.Warn,
            ["startpending"] = StatusKind.Warn, ["stoppending"] = StatusKind.Warn, ["continuepending"] = StatusKind.Warn,
            ["pausepending"] = StatusKind.Warn, ["suspended"] = StatusKind.Warn, ["unknown"] = StatusKind.Warn,
            // Container, pod and service lifecycle words (Docker, Kubernetes, systemd).
            ["restarting"] = StatusKind.Warn, ["starting"] = StatusKind.Warn, ["stopping"] = StatusKind.Warn, ["removing"] = StatusKind.Warn,
            ["terminating"] = StatusKind.Warn, ["reloading"] = StatusKind.Warn, ["activating"] = StatusKind.Warn,
            ["deactivating"] = StatusKind.Warn, ["exited"] = StatusKind.Skip, ["created"] = StatusKind.Skip, ["dead"] = StatusKind.Fail,
            ["crashloopbackoff"] = StatusKind.Fail, ["oomkilled"] = StatusKind.Fail, ["imagepullbackoff"] = StatusKind.Fail,
            // PowerShell's own words (streams, event levels, test results).
            ["warn"] = StatusKind.Warn, ["failure"] = StatusKind.Fail, ["skip"] = StatusKind.Skip,
            ["info"] = StatusKind.Info, ["information"] = StatusKind.Info, ["informational"] = StatusKind.Info,
            ["verbose"] = StatusKind.Info, ["debug"] = StatusKind.Info
        };

        /// <summary>Whether a word reads as a status.</summary>
        /// <param name="word">The word (an enum name or text).</param>
        /// <param name="status">The status it reads as.</param>
        /// <returns>True when known.</returns>
        public static bool TryRead(string? word, out StatusKind status) {
            status = StatusKind.Info;
            return word != null && Words.TryGetValue(word.Replace(" ", string.Empty), out status);
        }

        /// <summary>Whether an enum type reads as a status: at least one of its names is a known status word.</summary>
        /// <param name="type">The enum type.</param>
        /// <returns>True when it looks like a status.</returns>
        public static bool LooksLikeStatus(Type type) {
            if (type == null || !type.IsEnum) {
                return false;
            }
            foreach (string name in Enum.GetNames(type)) {
                if (TryRead(name, out _)) {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public PresentedValue Present(object? value, PresenterContext context) {
            string text = PresenterValues.ToText(value, context.Culture);
            if (text.Length == 0) {
                return new PresentedValue(StyledLine.Empty) { IsEmpty = true };
            }
            if (!TryRead(text, out StatusKind status)) {
                return new PresentedValue(StyledLine.From(text, context.Render.Style(ThemeToken.Text)));
            }
            Style style = context.Render.Style(status);
            string marker = context.Render.Glyphs.StatusMarker(status);
            return new PresentedValue(new StyledLine(new[] { new TextSpan(marker + " ", style), new TextSpan(text, status == StatusKind.Skip ? context.Render.Style(ThemeToken.Muted) : style) })) { Role = status.ToToken() };
        }
    }
}
