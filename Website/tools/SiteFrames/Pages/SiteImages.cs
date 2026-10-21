using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;

namespace SiteFrames.Pages {
    /// <summary>
    /// Pages for the raster images the site needs (the social card and the touch icon), written to <c>Website/_temp/images/</c>.
    /// <c>build/Render-SiteImages.ps1</c> screenshots them with headless Edge into <c>static/</c>. The card shows the last
    /// frame of the home page's replay, rendered by the same converter as every other frame.
    /// </summary>
    internal static class SiteImages {
        public static void Render(Site site) {
            string folder = Path.Combine(site.Website, "_temp", "images");
            Directory.CreateDirectory(folder);
            string fonts = new Uri(Path.Combine(site.Website, "static", "fonts") + Path.DirectorySeparatorChar).AbsoluteUri;
            string theme = Path.Combine(site.Website, "themes", "consolemonster", "assets");
            string css = File.ReadAllText(Path.Combine(theme, "tokens.css")).Replace("url(/fonts/", "url(" + fonts)
                + File.ReadAllText(Path.Combine(theme, "base.css")) + site.Styles.Css();
            JsonObject live = (JsonObject)JsonNode.Parse(File.ReadAllText(site.Data("live")))!;
            string poster = live["assessment"]!["poster"]!["dark"]!.GetValue<string>();
            string logo = File.ReadAllText(Path.Combine(site.Website, "themes", "consolemonster", "partials", "logo.html"));

            StringBuilder card = new StringBuilder();
            card.Append("<!doctype html><html data-theme=\"dark\"><head><meta charset=\"utf-8\"><style>").Append(css).Append(@"
html,body{margin:0;width:1200px;height:630px;overflow:hidden;background:#0d121b}
.card{position:relative;width:1200px;height:630px;background-image:radial-gradient(rgba(148,163,184,.1) 1px,transparent 1.2px);background-size:14px 24px}
.text{position:absolute;left:64px;top:60px;width:470px}
.brand{display:flex;align-items:center;gap:16px;font:700 30px/1 var(--cm-mono);color:#f8fafc}
.brand svg{width:56px;height:56px}
h1{margin:44px 0 0;font:700 58px/1.04 var(--cm-display);letter-spacing:-.02em;color:#f8fafc}
p{margin:26px 0 0;font:400 23px/1.45 var(--cm-font);color:#c3cad6}
.shot{position:absolute;left:560px;top:70px;width:900px;border-radius:12px;overflow:hidden;box-shadow:0 0 0 1px #273041,0 30px 80px -20px rgba(0,0,0,.9)}
.shot .cm-screen{font-size:10.416667px!important;min-width:0;width:max-content}
</style></head><body><div class=""card""><div class=""text""><div class=""brand"">").Append(logo).Append(@"<span>ConsoleMonster</span></div>
<h1>Every cell accounted for.</h1><p>Tables, trees, charts and live dashboards for .NET and PowerShell 5.1 and 7. No dependencies.</p></div>
<div class=""shot""><pre class=""cm-screen cm-screen--dark"" style=""--cols:120"">").Append(poster).Append("</pre></div></div></body></html>");
            File.WriteAllText(Path.Combine(folder, "social-card.html"), card.ToString(), new UTF8Encoding(false));

            string icon = "<!doctype html><html><head><meta charset=\"utf-8\"><style>html,body{margin:0;width:180px;height:180px;overflow:hidden;background:#0d121b}svg{display:block;width:180px;height:180px}</style></head><body>"
                + logo.Replace("rx=\"8\"", "rx=\"0\"") + "</body></html>";
            File.WriteAllText(Path.Combine(folder, "touch-icon.html"), icon, new UTF8Encoding(false));
        }
    }
}
