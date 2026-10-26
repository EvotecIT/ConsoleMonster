using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;

namespace ConsoleMonster.PowerShell {
    // $using:name for script blocks that run in module-owned runspaces (Invoke-ConsoleRun -Parallel, Invoke-ConsoleDashboard):
    // each reference becomes a local variable whose value is read from the caller now, as ForEach-Object -Parallel does.
    internal static class PSUsing {
        internal static string Rewrite(ScriptBlock script, SessionState caller, IDictionary values) {
            Ast body = script.Ast;
            string text = body.Extent.Text;
            int origin = body.Extent.StartOffset;
            List<UsingExpressionAst> found = body.FindAll(ast => ast is UsingExpressionAst, true).Cast<UsingExpressionAst>().OrderByDescending(ast => ast.Extent.StartOffset).ToList();
            var rewritten = new StringBuilder(text);
            foreach (UsingExpressionAst use in found) {
                // $using:name, or a member/index chain on it ($using:servers.Count, $using:map['web01']).
                Ast target = use.SubExpression;
                while (target is MemberExpressionAst member) { target = member.Expression; }
                while (target is IndexExpressionAst index) { target = index.Target; while (target is MemberExpressionAst inner) { target = inner.Expression; } }
                if (!(target is VariableExpressionAst variable)) {
                    throw new PSArgumentException("Only $using:name (with optional .Member or [index]) is supported in script blocks that run in their own runspaces: " + use.Extent.Text);
                }
                string name = variable.VariablePath.UserPath;
                string local = "__cmUsing_" + name.Replace(':', '_');
                values[local] = caller.PSVariable.GetValue(name);
                rewritten.Remove(variable.Extent.StartOffset - origin, variable.Extent.Text.Length).Insert(variable.Extent.StartOffset - origin, "$" + local);
            }
            return rewritten.ToString().Trim();
        }
    }
}
