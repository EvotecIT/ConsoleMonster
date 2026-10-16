using System.Management.Automation;
using ConsoleMonster.Live;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Base for cmdlets that update a running dashboard. The session is taken from <c>-Session</c>, then from the
    /// <c>$ConsoleSession</c> variable that <c>Start-ConsoleDashboard</c> defines in the runspaces it owns, then from the
    /// most recently started session in the process.
    /// </summary>
    public abstract class LiveCmdlet : PSCmdlet {
        /// <summary>
        /// <para type="description">The live session to update. Defaults to $ConsoleSession or the running dashboard.</para>
        /// </summary>
        [Parameter]
        public LiveSession? Session { get; set; }

        /// <summary>Posts an event to the resolved session; writes an error when no dashboard is running.</summary>
        /// <param name="liveEvent">The event.</param>
        protected void Send(LiveEvent liveEvent) {
            LiveSession? session = Session;
            if (session == null) {
                object? variable = SessionState.PSVariable.GetValue("ConsoleSession");
                session = (variable is PSObject ps ? ps.BaseObject : variable) as LiveSession;
            }
            session ??= LiveSession.Current;
            if (session == null) {
                WriteError(new ErrorRecord(new PSInvalidOperationException("No console dashboard is running. Use Start-ConsoleDashboard first."), "ConsoleMonsterNoSession", ErrorCategory.InvalidOperation, null));
                return;
            }
            session.Post(liveEvent);
        }
    }
}
