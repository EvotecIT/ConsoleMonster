using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
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
        public static string Build(Grid grid, GridOptions options) {
            CultureInfo culture = grid.Presentation.Culture ?? CultureInfo.CurrentCulture;
            // Presenters write for a wide, full-colour, full-glyph screen; the page keeps their text, number and role.
            RenderContext render = Terminal.CreateContext(TerminalProfile.Fixed(200, 50, ColorDepth.TrueColor, GlyphTier.Full), (options.Interactive?.Theme) ?? Terminal.Theme);
            PresenterContext presenter = grid.Presentation.CreateContext(render);
            StringBuilder json = new StringBuilder();
            json.Append("{\"title\":").Append(Quote(grid.Title));
            json.Append(",\"mode\":").Append(Quote(grid.OutputMode.ToString()));
            json.Append(",\"filter\":").Append(Quote(options.Filter ?? string.Empty));
            json.Append(",\"sort\":").Append(options.SortColumn.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"descending\":").Append(options.SortDescending ? "true" : "false");
            // Rows first: a column without an explicit alignment is right-aligned when every non-empty cell is a number.
            StringBuilder rows = new StringBuilder();
            bool[] numeric = new bool[grid.Columns.Count];
            bool[] other = new bool[grid.Columns.Count];
            for (int r = 0; r < grid.Rows.Count; r++) {
                GridRow row = grid.Rows[r];
                if (r > 0) { rows.Append(','); }
                rows.Append('[');
                for (int c = 0; c < grid.Columns.Count; c++) {
                    if (c > 0) { rows.Append(','); }
                    object? value = c < row.Values.Count ? row.Values[c] : null;
                    if (value is DataGridCell lazy) { value = lazy.Peek(); }
                    bool? right = AppendCell(rows, grid.Columns[c], value, presenter, culture);
                    if (right == true) { numeric[c] = true; } else if (right == false) { other[c] = true; }
                }
                rows.Append(']');
            }
            json.Append(",\"columns\":[");
            for (int c = 0; c < grid.Columns.Count; c++) {
                TableColumn column = grid.Columns[c];
                string header = Markup.Strip(column.Header);
                bool hidden = Contains(options.HiddenColumns, header);
                HorizontalAlignment? alignment = column.Alignment ?? (numeric[c] && !other[c] ? HorizontalAlignment.Right : (HorizontalAlignment?)null);
                if (c > 0) { json.Append(','); }
                json.Append("{\"name\":").Append(Quote(header))
                    .Append(",\"hidden\":").Append(hidden ? "true" : "false")
                    .Append(",\"align\":").Append(Quote(Align(alignment))).Append('}');
            }
            json.Append("],\"rows\":[").Append(rows).Append("]}");
            return Template
                .Replace("__TITLE__", WebUtility.HtmlEncode(grid.Title))
                .Replace("__DATA__", json.ToString());
        }

        /// <summary>Writes one cell; returns whether it reads as a number (null for an empty cell).</summary>
        private static bool? AppendCell(StringBuilder json, TableColumn column, object? value, PresenterContext presenter, CultureInfo culture) {
            string text;
            double? number = null;
            string? role = null;
            bool right;
            if (column.Presenter != null && value != null) {
                PresentedValue presented = column.Presenter.Present(value, presenter);
                text = presented.Text;
                number = presented.Number;
                role = presented.Role.HasValue ? Role(presented.Role.Value) : null;
                right = presented.Alignment == HorizontalAlignment.Right || (presented.Alignment == null && presented.Number.HasValue);
            } else {
                text = PresenterValues.ToText(value, culture);
                right = IsNumber(value);
            }
            number ??= SortNumber(value);
            json.Append("{\"t\":").Append(Quote(text));
            if (number.HasValue && !double.IsNaN(number.Value) && !double.IsInfinity(number.Value)) {
                json.Append(",\"n\":").Append(number.Value.ToString("R", CultureInfo.InvariantCulture));
            }
            if (role != null) { json.Append(",\"r\":").Append(Quote(role)); }
            json.Append('}');
            return text.Length == 0 ? (bool?)null : right;
        }

        private static bool IsNumber(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

        /// <summary>Numbers sort numerically and dates chronologically; other values sort as text.</summary>
        private static double? SortNumber(object? value) {
            switch (value) {
                case null: return null;
                case DateTime time: return time.Ticks;
                case DateTimeOffset offset: return offset.UtcTicks;
                case TimeSpan span: return span.Ticks;
                case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                default: return null;
            }
        }

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
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '<': builder.Append("\\u003c"); break;
                    case '>': builder.Append("\\u003e"); break;
                    case '&': builder.Append("\\u0026"); break;
                                                            default:
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
footer span{flex:1;color:var(--muted)}
button{font:inherit;padding:6px 16px;border-radius:6px;border:1px solid var(--line);background:var(--head);color:var(--fg);cursor:pointer}
button.primary{background:var(--focus);border-color:var(--focus);color:#fff}button:focus-visible{outline:2px solid var(--focus);outline-offset:2px}
.done{margin:auto;color:var(--muted);padding:24px;text-align:center}
</style></head><body>
<header><h1 id=""title""></h1><input id=""filter"" type=""search"" placeholder=""Filter"" aria-label=""Filter rows"" autocomplete=""off""></header>
<main><table aria-multiselectable=""true""><thead><tr id=""head""></tr></thead><tbody id=""body""></tbody></table></main>
<footer><span id=""status"" role=""status""></span><button id=""cancel"" type=""button"">Cancel</button><button id=""ok"" type=""button"" class=""primary"">OK</button></footer>
<script>
const d=__DATA__;let sort=d.sort,desc=d.descending,picked=new Set(),shown=[],focus=-1,sent=false;
const $=id=>document.getElementById(id),cols=d.columns.map((c,i)=>({...c,i})).filter(c=>!c.hidden);
$('title').textContent=d.title;document.title=d.title;$('filter').value=d.filter;
if(d.mode==='None'){$('ok').textContent='Close';$('cancel').hidden=true}
for(const c of cols){const th=document.createElement('th');th.textContent=c.name;th.tabIndex=0;if(c.align!=='left')th.className=c.align;
th.onclick=()=>{desc=sort===c.i?!desc:false;sort=c.i;draw()};th.onkeydown=e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();th.click()}};$('head').appendChild(th)}
function cmp(a,b){const x=d.rows[a][sort],y=d.rows[b][sort];
if(x.n!==undefined&&y.n!==undefined)return x.n-y.n;if(x.n!==undefined)return -1;if(y.n!==undefined)return 1;return x.t.localeCompare(y.t,undefined,{numeric:true,sensitivity:'base'})}
function draw(){const words=$('filter').value.toLowerCase().split(/\s+/).filter(Boolean);
shown=d.rows.map((r,i)=>i).filter(i=>words.every(w=>cols.some(c=>d.rows[i][c.i].t.toLowerCase().includes(w))));
if(sort>=0){shown.sort((a,b)=>desc?cmp(b,a):cmp(a,b))}
[...$('head').children].forEach((th,k)=>th.setAttribute('aria-sort',cols[k].i===sort?(desc?'descending':'ascending'):'none'));
const body=$('body');body.replaceChildren();
shown.forEach((i,pos)=>{const tr=document.createElement('tr');tr.setAttribute('aria-selected',picked.has(i));if(i===focus)tr.className='focus';
for(const c of cols){const v=d.rows[i][c.i],td=document.createElement('td');td.textContent=v.t;const cls=[c.align!=='left'?c.align:'',v.r||''].join(' ').trim();if(cls)td.className=cls;tr.appendChild(td)}
tr.onclick=e=>choose(i,e.ctrlKey||e.metaKey,e.shiftKey);tr.ondblclick=()=>{if(d.mode!=='None'){picked=new Set([i]);answer(true)}};body.appendChild(tr)});
$('status').textContent=shown.length+' of '+d.rows.length+' rows'+(picked.size?' · '+picked.size+' selected':'')}
function choose(i,toggle,range){focus=i;if(d.mode==='None'){draw();return}
if(d.mode==='Multiple'&&range&&picked.size){const a=shown.indexOf([...picked].pop()),b=shown.indexOf(i);for(let k=Math.min(a,b);k<=Math.max(a,b);k++)picked.add(shown[k])}
else if(d.mode==='Multiple'&&toggle){picked.has(i)?picked.delete(i):picked.add(i)}else picked=new Set([i]);draw()}
async function answer(ok){if(sent)return;sent=true;const rows=ok?(picked.size?[...picked]:(focus>=0&&d.mode!=='None'?[focus]:[])):[];
try{await fetch('answer',{method:'POST',body:(ok?'ok:'+rows.join(','):'cancel')})}catch(e){}
document.body.innerHTML='';const p=document.createElement('p');p.className='done';
p.textContent=ok?(d.mode==='None'?'Closed.':'Sent '+rows.length+' row(s) back to PowerShell.')+' You can close this tab.':'Cancelled. You can close this tab.';document.body.appendChild(p)}
$('filter').oninput=draw;$('ok').onclick=()=>answer(true);$('cancel').onclick=()=>answer(false);
document.addEventListener('keydown',e=>{if(sent)return;
if(e.key==='Escape'){answer(false)}else if(e.key==='Enter'&&e.target.tagName!=='TH'&&e.target.tagName!=='BUTTON'){answer(true)}
else if(e.key==='ArrowDown'||e.key==='ArrowUp'){e.preventDefault();const p=shown.indexOf(focus),n=Math.max(0,Math.min(shown.length-1,p+(e.key==='ArrowDown'?1:-1)));focus=shown[n]??-1;draw();document.querySelector('tr.focus')?.scrollIntoView({block:'nearest'})}
else if(e.key===' '&&e.target.tagName!=='INPUT'&&e.target.tagName!=='BUTTON'&&focus>=0){e.preventDefault();choose(focus,true,false)}});
window.addEventListener('pagehide',()=>{if(!sent){sent=true;navigator.sendBeacon('answer','cancel')}});
draw();$('filter').focus();
</script></body></html>";
    }
}
