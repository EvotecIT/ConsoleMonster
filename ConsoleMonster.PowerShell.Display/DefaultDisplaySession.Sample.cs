using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace ConsoleMonster.PowerShell.Display;

public sealed partial class DefaultDisplaySession {
    // Runspace callbacks own all host work. A quiet producer cannot trigger an independent timer write.
    private void Sample(PSObject value, DefaultDisplayRegistration view, List<DefaultDisplayOperation> output) {
        long estimate;
        try { estimate = DisplayData.Estimate(value.BaseObject, view.Budget.Bytes); }
        catch (Exception) { estimate = view.Budget.Bytes + 1; }
        if (estimate > view.Budget.Bytes - _bytes) {
            _nativeBatch = true;
            _nativeCount = _pending.Count + 1L;
            if (_pending.Count > 0) { output.Add(new DefaultDisplayOperation(null, _pending.ToArray(), _scope)); _pending.Clear(); }
            output.Add(new DefaultDisplayOperation(null, new[] { value }, _scope));
            return;
        }
        _pending.Add(value);
        _bytes += estimate;
        if (_pending.Count >= Math.Min(200, view.Budget.Records) || _clock.ElapsedMilliseconds >= 250) {
            output.Add(new DefaultDisplayOperation(view, _pending.ToArray(), _scope));
            _pending.Clear(); _bytes = 0; _streamStarted = true;
        }
    }
}
