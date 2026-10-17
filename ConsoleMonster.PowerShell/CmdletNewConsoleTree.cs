using System.Collections;
using System.Management.Automation;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// <para type="synopsis">Creates a console tree from nested hashtables.</para>
    /// <para type="description">Each key becomes a node. A nested hashtable (or ordered dictionary) becomes child nodes; an array becomes leaf nodes; any other value is shown as a muted note after the key. Use [ordered] to keep order.</para>
    /// <example>
    ///   <para>A DNS delegation tree</para>
    ///   <code>New-ConsoleTree -Title 'evotec.xyz' -Data ([ordered] @{ NS = @('dana.ns.cloudflare.com', 'kirk.ns.cloudflare.com'); MX = [ordered] @{ 'mail.protection.outlook.com' = '52.101.68.17' } }) | Write-ConsoleWidget</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.New, "ConsoleTree")]
    [OutputType(typeof(Tree))]
    public class CmdletNewConsoleTree : PSCmdlet {
        /// <summary><para type="description">Nested hashtables describing the tree.</para></summary>
        [Parameter(Position = 0, Mandatory = true)]
        public IDictionary Data { get; set; } = null!;

        /// <summary><para type="description">The root label; without it the top-level keys are the roots.</para></summary>
        [Parameter]
        public object? Title { get; set; }

        /// <inheritdoc />
        protected override void ProcessRecord() {
            Tree tree = new Tree(PSText.Markup(Title));
            Fill(tree.Root, Data);
            Emit(tree);
        }

        /// <summary>Emits the tree (Write-ConsoleTree overrides this to write it).</summary>
        /// <param name="tree">The tree.</param>
        protected virtual void Emit(Tree tree) => WriteObject(tree);

        private static void Fill(TreeNode parent, IDictionary data) {
            foreach (DictionaryEntry entry in data) {
                string key = Markup.Escape(PSWidgets.ToText(entry.Key));
                object? value = entry.Value is PSObject ps ? ps.BaseObject : entry.Value;
                if (value is IDictionary nested) {
                    Fill(parent.Add(key), nested);
                } else if (value is IEnumerable list && !(value is string)) {
                    TreeNode node = parent.Add(key);
                    foreach (object? item in list) {
                        object? raw = item is PSObject itemPs ? itemPs.BaseObject : item;
                        if (raw is IDictionary itemDictionary) {
                            Fill(node, itemDictionary);
                        } else {
                            node.Add(Markup.Escape(PSWidgets.ToText(raw)));
                        }
                    }
                } else {
                    parent.Add(key, null, value == null ? null : PSWidgets.ToText(value));
                }
            }
        }
    }
}
