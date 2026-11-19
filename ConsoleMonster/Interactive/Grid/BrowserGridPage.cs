using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Interactive {
    /// <summary>Builds the self-contained browser grid page: rows are embedded as JSON and nothing loads from elsewhere.</summary>
    internal static class BrowserGridPage {
        private sealed class Cell {
            public object? Value;
            public string Text = string.Empty;
            public string? Role;
            public bool? Right;
        }

        public static string Build(Grid grid, GridOptions options, CancellationToken cancellationToken) {
            InteractiveOptions interactive = options.Interactive ?? InteractiveOptions.Default;
            // Presenters write for a wide, full-colour, full-glyph screen; the page keeps their text and role. The culture
            // follows the resolved interaction profile, as in the screen grid, unless the grid's presentation sets one.
            TerminalProfile profile = TerminalProfile.Fixed(200, 50, ColorDepth.TrueColor, GlyphTier.Full).WithCulture(interactive.ResolveProfile().Culture);
            RenderContext render = Terminal.CreateContext(profile, interactive.Theme ?? Terminal.Theme);
            PresenterContext presenter = grid.Presentation.CreateContext(render);
            int columns = grid.Columns.Count;
            object?[][] values = ResolveValues(grid, cancellationToken);
            Cell[][] cells = new Cell[values.Length][];
            for (int r = 0; r < values.Length; r++) {
                cells[r] = new Cell[columns];
                for (int c = 0; c < columns; c++) {
                    cells[r][c] = Present(grid.Columns[c], values[r][c], presenter);
                }
            }

            StringBuilder json = new StringBuilder();
            json.Append("{\"title\":").Append(Quote(grid.Title));
            json.Append(",\"mode\":").Append(Quote(grid.OutputMode.ToString()));
            json.Append(",\"filter\":").Append(Quote(options.Filter ?? string.Empty));
            json.Append(",\"sort\":").Append((options.SortColumn >= 0 && options.SortColumn < columns ? options.SortColumn : -1).ToString(CultureInfo.InvariantCulture));
            json.Append(",\"descending\":").Append(options.SortDescending ? "true" : "false");
            json.Append(",\"columns\":[");
            int[][] ranks = new int[columns][];
            for (int c = 0; c < columns; c++) {
                TableColumn column = grid.Columns[c];
                string header = Markup.Strip(column.Header);
                bool numeric = false, other = false;
                foreach (Cell[] row in cells) {
                    if (row[c].Right == true) { numeric = true; } else if (row[c].Right == false) { other = true; }
                }
                // A column without an explicit alignment is right-aligned when every non-empty cell is a number.
                HorizontalAlignment? alignment = column.Alignment ?? (numeric && !other ? HorizontalAlignment.Right : (HorizontalAlignment?)null);
                if (c > 0) { json.Append(','); }
                json.Append("{\"name\":").Append(Quote(header))
                    .Append(",\"hidden\":").Append(Contains(options.HiddenColumns, header) ? "true" : "false")
                    .Append(",\"align\":").Append(Quote(Align(alignment))).Append('}');
                ranks[c] = Ranks(cells, c);
            }
            json.Append("],\"rows\":[");
            for (int r = 0; r < cells.Length; r++) {
                if (r > 0) { json.Append(','); }
                json.Append('[');
                for (int c = 0; c < columns; c++) {
                    if (c > 0) { json.Append(','); }
                    Cell cell = cells[r][c];
                    json.Append("{\"t\":").Append(Quote(cell.Text)).Append(",\"k\":").Append(ranks[c][r].ToString(CultureInfo.InvariantCulture));
                    if (cell.Role != null) { json.Append(",\"r\":").Append(Quote(cell.Role)); }
                    json.Append('}');
                }
                json.Append(']');
            }
            json.Append("]}");
            return Template
                .Replace("__TITLE__", WebUtility.HtmlEncode(grid.Title))
                .Replace("__DATA__", json.ToString());
        }

        // Lazy cells are computed before the page is built: the page is a snapshot, so a placeholder would never update.
        private static object?[][] ResolveValues(Grid grid, CancellationToken cancellationToken) {
            int columns = grid.Columns.Count;
            object?[][] values = new object?[grid.Rows.Count][];
            List<Task> pending = new List<Task>();
            for (int r = 0; r < grid.Rows.Count; r++) {
                values[r] = new object?[columns];
                for (int c = 0; c < columns; c++) {
                    object? value = c < grid.Rows[r].Values.Count ? grid.Rows[r].Values[c] : null;
                    if (value is DataGridCell lazy) { pending.Add(lazy.Start(cancellationToken)); }
                    values[r][c] = value;
                }
            }
            if (pending.Count > 0) {
                try {
                    Task.WaitAll(pending.ToArray(), cancellationToken);
                } catch (AggregateException) {
                    // A failed cell shows its placeholder, as it does in the screen grid.
                }
            }
            for (int r = 0; r < values.Length; r++) {
                for (int c = 0; c < columns; c++) {
                    if (values[r][c] is DataGridCell lazy) { values[r][c] = lazy.Peek(); }
                }
            }
            return values;
        }

        // The shared presentation step, including the column's tone rules (FailAbove, WarnBelow, Role).
        private static Cell Present(TableColumn column, object? value, PresenterContext presenter) {
            PresentedValue presented = CellPresenters.Present(value, column.Presenter, column.RoleRules.Count > 0 ? new List<RoleRule>(column.RoleRules) : null, presenter);
            Cell cell = new Cell { Value = value, Text = presented.Text, Role = presented.Role.HasValue ? Role(presented.Role.Value) : null };
            if (cell.Text.Length > 0) {
                cell.Right = presented.Alignment == HorizontalAlignment.Right || (presented.Alignment == null && IsNumber(value));
            }
            return cell;
        }

        // Ranks follow the screen grid's comparison: empty values last, numbers numerically, values of one comparable type
        // by their own order, and the rest by ordinal, case-insensitive text. Equal values share a rank, so the page keeps
        // the original row order for them.
        private static int[] Ranks(Cell[][] cells, int column) {
            int[] order = new int[cells.Length];
            for (int i = 0; i < order.Length; i++) { order[i] = i; }
            Comparison<int> compare = (a, b) => Compare(cells[a][column], cells[b][column]);
            Array.Sort(order, (a, b) => { int result = compare(a, b); return result != 0 ? result : a.CompareTo(b); });
            int[] ranks = new int[cells.Length];
            int rank = 0;
            for (int i = 0; i < order.Length; i++) {
                if (i > 0 && compare(order[i - 1], order[i]) != 0) { rank++; }
                ranks[order[i]] = rank;
            }
            return ranks;
        }

        private static int Compare(Cell a, Cell b) {
            object? x = a.Value, y = b.Value;
            bool emptyX = PresenterValues.IsEmpty(x), emptyY = PresenterValues.IsEmpty(y);
            if (emptyX || emptyY) { return emptyX == emptyY ? 0 : emptyX ? 1 : -1; }
            if (!(x is string) && !(y is string) && PresenterValues.TryGetNumber(x, out double dx) && PresenterValues.TryGetNumber(y, out double dy)) { return dx.CompareTo(dy); }
            if (!(x is string) && x is IComparable comparable && y != null && x.GetType() == y.GetType()) { return comparable.CompareTo(y); }
            return string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumber(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

        private static string? Role(ThemeToken token) => token switch {
            ThemeToken.Pass => "pass",
            ThemeToken.Fail => "fail",
            ThemeToken.Warn => "warn",
            ThemeToken.Info => "info",
            ThemeToken.Muted or ThemeToken.Subtle => "muted",
            ThemeToken.Accent => "accent",
            _ => null
        };

        private static string Align(HorizontalAlignment? alignment) => alignment switch {
            HorizontalAlignment.Right => "right",
            HorizontalAlignment.Center => "center",
            _ => "left"
        };

        private static bool Contains(IList<string> headers, string header) {
            foreach (string candidate in headers) {
                if (string.Equals(candidate, header, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        /// <summary>A JSON string that is also safe inside an inline script element.</summary>
        internal static string Quote(string value) {
            StringBuilder builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (char ch in value) {
                switch (ch) {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '<': builder.Append("\\u003c"); break;
                    case '>': builder.Append("\\u003e"); break;
                    case '&': builder.Append("\\u0026"); break;
                    default:
                        // Control characters and the JavaScript line terminators U+2028 and U+2029 become escapes.
                        if (ch < ' ' || ch == (char)0x2028 || ch == (char)0x2029) {
                            builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        } else {
                            builder.Append(ch);
                        }
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }

        private const string Template = @"<!doctype html><html lang=""en""><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>__TITLE__</title>
<style>
:root{color-scheme:light dark;--bg:#ffffff;--fg:#1f2328;--muted:#656d76;--line:#d0d7de;--head:#f6f8fa;--sel:#ddf4ff;--focus:#0969da;--pass:#1a7f37;--fail:#cf222e;--warn:#9a6700;--info:#0969da;--accent:#8250df}
@media (prefers-color-scheme:dark){:root{--bg:#0d1117;--fg:#e6edf3;--muted:#8d96a0;--line:#30363d;--head:#161b22;--sel:#12324f;--focus:#4493f8;--pass:#3fb950;--fail:#f85149;--warn:#d29922;--info:#4493f8;--accent:#a371f7}}
*{box-sizing:border-box}html,body{height:100%}body{margin:0;font:14px/1.4 system-ui,-apple-system,""Segoe UI"",sans-serif;background:var(--bg);color:var(--fg);display:flex;flex-direction:column}
header,footer{display:flex;gap:12px;align-items:center;padding:10px 16px;border-color:var(--line);border-style:solid;border-width:0}
header{border-bottom-width:1px}footer{border-top-width:1px}
h1{font-size:16px;margin:0;flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
input{font:inherit;padding:6px 10px;border:1px solid var(--line);border-radius:6px;background:var(--bg);color:var(--fg);width:min(280px,45vw)}
main{flex:1;overflow:auto}table{border-collapse:collapse;width:100%}
th,td{padding:6px 12px;border-bottom:1px solid var(--line);text-align:left;white-space:nowrap}
th{position:sticky;top:0;background:var(--head);cursor:pointer;user-select:none;font-weight:600}
th:hover,th:focus-visible{color:var(--focus);outline:none}th[aria-sort=ascending]::after{content:"" \2191""}th[aria-sort=descending]::after{content:"" \2193""}
td.right,th.right{text-align:right;font-variant-numeric:tabular-nums}td.center,th.center{text-align:center}
tbody tr{cursor:default}tbody tr[aria-selected=true] td{background:var(--sel)}tbody tr.focus td:first-child{box-shadow:inset 3px 0 0 var(--focus)}
.pass{color:var(--pass)}.fail{color:var(--fail)}.warn{color:var(--warn)}.info{color:var(--info)}.muted{color:var(--muted)}.accent{color:var(--accent)}
footer span{flex:1;color:var(--muted)}footer span.error{color:var(--fail)}
button{font:inherit;padding:6px 16px;border-radius:6px;border:1px solid var(--line);background:var(--head);color:var(--fg);cursor:pointer}
button.primary{background:var(--focus);border-color:var(--focus);color:#fff}button:focus-visible{outline:2px solid var(--focus);outline-offset:2px}
.done{margin:auto;color:var(--muted);padding:24px;text-align:center}
</style></head><body>
<header><h1 id=""title""></h1><input id=""filter"" type=""search"" placeholder=""Filter"" aria-label=""Filter rows"" autocomplete=""off""></header>
<main><table aria-multiselectable=""true""><thead><tr id=""head""></tr></thead><tbody id=""body""></tbody></table></main>
<footer><span id=""status"" role=""status""></span><button id=""cancel"" type=""button"">Cancel</button><button id=""ok"" type=""button"" class=""primary"">OK</button></footer>
<script>
const d=__DATA__;let sort=d.sort,desc=d.descending,picked=new Set(),shown=[],focus=-1,anchor=-1,sending=false,sent=false,failure='';
const $=id=>document.getElementById(id),cols=d.columns.map((c,i)=>({...c,i})).filter(c=>!c.hidden),single=d.mode==='Single',multiple=d.mode==='Multiple';
$('title').textContent=d.title;document.title=d.title;$('filter').value=d.filter;
if(d.mode==='None'){$('ok').textContent='Close';$('cancel').hidden=true}
for(const c of cols){const th=document.createElement('th');th.textContent=c.name;th.tabIndex=0;if(c.align!=='left')th.className=c.align;
th.onclick=()=>{desc=sort===c.i?!desc:false;sort=c.i;draw()};th.onkeydown=e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();th.click()}};$('head').appendChild(th)}
const order=(a,b)=>{if(sort<0)return a-b;const k=d.rows[a][sort].k-d.rows[b][sort].k;return k===0?a-b:(desc?-k:k)};
function draw(){const words=$('filter').value.toLowerCase().split(/[\s,]+/).filter(Boolean);
shown=d.rows.map((r,i)=>i).filter(i=>words.every(w=>cols.some(c=>d.rows[i][c.i].t.toLowerCase().includes(w)))).sort(order);
if(!shown.includes(focus))focus=shown.length?shown[0]:-1;
[...$('head').children].forEach((th,k)=>th.setAttribute('aria-sort',cols[k].i===sort?(desc?'descending':'ascending'):'none'));
const body=$('body');body.replaceChildren();
for(const i of shown){const tr=document.createElement('tr');tr.setAttribute('aria-selected',single?i===focus:picked.has(i));if(i===focus)tr.className='focus';
for(const c of cols){const v=d.rows[i][c.i],td=document.createElement('td');td.textContent=v.t;const cls=[c.align!=='left'?c.align:'',v.r||''].join(' ').trim();if(cls)td.className=cls;tr.appendChild(td)}
tr.onclick=e=>choose(i,e.ctrlKey||e.metaKey,e.shiftKey);tr.ondblclick=()=>{if(d.mode!=='None'){focus=i;if(multiple)picked=new Set([i]);answer(true)}};body.appendChild(tr)}
const count=single?(focus>=0?1:0):picked.size,s=$('status');s.className=failure?'error':'';s.textContent=failure||(shown.length+' of '+d.rows.length+' rows'+(multiple&&count?' \u00b7 '+count+' selected':''))}
function choose(i,toggle,range){focus=i;if(!multiple){draw();return}
const from=shown.indexOf(anchor),to=shown.indexOf(i);
if(range&&from>=0&&to>=0){for(let k=Math.min(from,to);k<=Math.max(from,to);k++)picked.add(shown[k])}
else if(toggle){picked.has(i)?picked.delete(i):picked.add(i);anchor=i}else{picked=new Set([i]);anchor=i}draw()}
// As in the screen grid: marked rows in the current sort order, otherwise the focused row.
function chosen(){if(d.mode==='None')return[];if(multiple&&picked.size)return d.rows.map((r,i)=>i).filter(i=>picked.has(i)).sort(order);return focus>=0?[focus]:[]}
async function answer(ok){if(sending||sent)return;sending=true;failure='';const rows=ok?chosen():[];
let reply;try{reply=await fetch('answer',{method:'POST',body:(ok?'ok:'+rows.join(','):'cancel')})}catch(e){reply=null}
sending=false;if(!reply||!reply.ok){failure='PowerShell did not receive the answer. Try again, or press Ctrl+C in PowerShell.';draw();return}
sent=true;document.body.innerHTML='';const p=document.createElement('p');p.className='done';
p.textContent=ok?(d.mode==='None'?'Closed.':'Sent '+rows.length+' row(s) back to PowerShell.')+' You can close this tab.':'Cancelled. You can close this tab.';document.body.appendChild(p)}
$('filter').oninput=draw;$('ok').onclick=()=>answer(true);$('cancel').onclick=()=>answer(false);
document.addEventListener('keydown',e=>{if(sent)return;const tag=e.target.tagName;
if(e.key==='Escape'){answer(false)}else if(e.key==='Enter'&&tag!=='TH'&&tag!=='BUTTON'){answer(true)}
else if(e.key==='ArrowDown'||e.key==='ArrowUp'){e.preventDefault();const p=shown.indexOf(focus),n=Math.max(0,Math.min(shown.length-1,p+(e.key==='ArrowDown'?1:-1)));focus=shown[n]??-1;draw();document.querySelector('tr.focus')?.scrollIntoView({block:'nearest'})}
else if(e.key===' '&&tag!=='INPUT'&&tag!=='BUTTON'&&tag!=='TH'&&focus>=0){e.preventDefault();choose(focus,true,false)}});
window.addEventListener('pagehide',()=>{if(!sent&&!sending){sent=true;navigator.sendBeacon('answer','cancel')}});
draw();$('filter').focus();
</script></body></html>";
    }
}
