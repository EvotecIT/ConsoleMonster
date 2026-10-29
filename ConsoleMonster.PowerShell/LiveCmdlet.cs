using System.Collections.Concurrent;
using System.Linq;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Base for cmdlets that update a running dashboard. The session is taken from <c>-Session</c>, then from the
    /// <c>$ConsoleSession</c> variable that <c>Invoke-ConsoleDashboard</c> defines in the runspaces it owns, then from the
    /// most recently started session in the process.
    /// </summary>
    public abstract class LiveCmdlet : PSCmdlet {
        // Unknown target names already reported per session, so a loop with a typo warns once, not per update.
        private static readonly ConditionalWeakTable<LiveSession, ConcurrentDictionary<string, bool>> Warned = new ConditionalWeakTable<LiveSession, ConcurrentDictionary<string, bool>>();
        private bool _noSessionReported;

        /// <summary>
        /// <para type="description">The live session to update. Defaults to $ConsoleSession or the running dashboard.</para>
        /// </summary>
        [Parameter]
        public LiveSession? Session { get; set; }

        /// <summary>
        /// Posts an event to the resolved session; writes an error when no dashboard is running and a warning (once per
        /// name and session) when the dashboard has no widget with the target name.
        /// </summary>
        /// <param name="liveEvent">The event.</param>
        protected void Send(LiveEvent liveEvent) {
            LiveSession? session = ResolveSession();
            if (session == null) {
                if (!_noSessionReported) {
                    _noSessionReported = true;
                    WriteError(new ErrorRecord(new PSInvalidOperationException("No console dashboard is running. Use Invoke-ConsoleDashboard first."), "ConsoleMonsterNoSession", ErrorCategory.InvalidOperation, null));
                }
                return;
            }
            if (liveEvent.WidgetId != LiveSession.MarkerTarget && !session.TryGetTarget(liveEvent.WidgetId, out _) && Warned.GetOrCreateValue(session).TryAdd(liveEvent.WidgetId, true)) {
                WriteWarning(UnknownTargetMessage(session, liveEvent.WidgetId, null));
            }
            session.Post(liveEvent);
        }

        /// <summary>The session updates go to: -Session, then $ConsoleSession, then the running dashboard; null when none runs.</summary>
        /// <returns>The session, or null.</returns>
        protected LiveSession? ResolveSession() {
            LiveSession? session = Session;
            if (session == null) {
                object? variable = SessionState.PSVariable.GetValue("ConsoleSession");
                session = (variable is PSObject ps ? ps.BaseObject : variable) as LiveSession;
            }
            return session ?? LiveSession.Current;
        }

        /// <summary>The warning for updates sent to a widget name the dashboard does not have.</summary>
        /// <param name="session">The session.</param>
        /// <param name="target">The unknown name.</param>
        /// <param name="count">How many updates were ignored, when known.</param>
        /// <returns>The message.</returns>
        internal static string UnknownTargetMessage(LiveSession session, string target, long? count) {
            string ignored = count.HasValue ? count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + " update(s)" : "updates";
            if (target == "$log") {
                return "The console dashboard has no log region (New-ConsoleLogRegion); " + ignored + " for the log were ignored.";
            }
            string names = string.Join(", ", session.TargetNames.OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase));
            return "The console dashboard has no widget named '" + target + "'; " + ignored + " for it were ignored. Widget names: " + (names.Length == 0 ? "(none)" : names) + ".";
        }
    }
}
