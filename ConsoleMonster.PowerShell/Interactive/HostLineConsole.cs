using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>Line prompts through a PowerShell host (remoting, the ISE): ReadLine, ReadLineAsSecureString and PromptForChoice.</summary>
    internal sealed class HostLineConsole : ILineConsole {
        private readonly PSHostUserInterface _ui;

        public HostLineConsole(PSHostUserInterface ui) {
            _ui = ui;
        }

        public void Write(string text) => _ui.Write(text);

        public void WriteLine(string text) => _ui.WriteLine(text);

        public string? ReadLine() => _ui.ReadLine();

        public string? ReadSecret() {
            SecureString? secure = _ui.ReadLineAsSecureString();
            if (secure == null) {
                return null;
            }
            IntPtr pointer = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secure);
            try {
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(pointer);
            } finally {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(pointer);
            }
        }

        public int? Choose(string caption, string message, IReadOnlyList<string> choices, int defaultIndex) {
            Collection<ChoiceDescription> descriptions = new Collection<ChoiceDescription>();
            foreach (string choice in choices) {
                descriptions.Add(new ChoiceDescription(choice));
            }
            return _ui.PromptForChoice(caption, message, descriptions, defaultIndex);
        }
    }
}
