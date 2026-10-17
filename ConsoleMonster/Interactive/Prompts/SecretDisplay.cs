using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>How a secret is shown while typed.</summary>
    public enum SecretDisplay {
        /// <summary>One mask character per typed character.</summary>
        Masked,

        /// <summary>Nothing at all (not even the length).</summary>
        Hidden
    }
}
