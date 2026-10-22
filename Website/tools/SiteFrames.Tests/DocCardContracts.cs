using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using SiteFrames;
using SiteFrames.Pages;
using Xunit;

namespace SiteFrames.Tests {
    /// <summary>Social artwork and themed catalogue previews must resolve to generated frame images.</summary>
    public sealed class DocCardContracts {
        [Fact]
        public void DualThemeCardProvidesSocialArtworkAndBothCataloguePreviews() {
            string root = Path.Combine(Path.GetTempPath(), "siteframes-card-" + Guid.NewGuid().ToString("N"));
            try {
                string website = Path.Combine(root, "Website");
                Directory.CreateDirectory(Path.Combine(website, "content", "docs"));
                Directory.CreateDirectory(Path.Combine(website, "content", "guides"));
                Directory.CreateDirectory(Path.Combine(website, "data"));
                File.WriteAllText(Path.Combine(root, "ConsoleMonster.sln"), string.Empty);
                File.WriteAllText(Path.Combine(website, "content", "docs", "sample.md"), "---\ngroup: samples\ncard: dual-theme\n---\n");
                JsonObject copy = new JsonObject {
                    ["docsCatalog"] = new JsonObject {
                        ["groups"] = new JsonArray(new JsonObject { ["id"] = "samples" })
                    }
                };
                Site site = new Site(website);
                File.WriteAllText(site.Data("copy"), copy.ToJsonString());
                DocCards.Offer("dual-theme", Site.Render(TextBlock.Plain("Artwork"), 20, ColorDepth.TrueColor, GlyphTier.Full, Theme.Dark), null);

                JsonObject cards = DocCards.Render(site);

                string social = site.Static("site/cards/dual-theme.svg");
                Assert.True(File.Exists(social), "The social-card pattern requires an unsuffixed SVG.");
                Assert.Equal(File.ReadAllText(site.Static("site/cards/dual-theme-dark.svg")), File.ReadAllText(social));
                Assert.Equal("svg", XDocument.Load(social).Root!.Name.LocalName);
                foreach (string theme in new[] { "dark", "light" }) {
                    string preview = "/site/cards/dual-theme-" + theme + ".svg";
                    Assert.Equal(preview, cards["dual-theme"]![theme]!.GetValue<string>());
                    Assert.True(File.Exists(site.Static(preview.Substring(1))));
                }
            } finally {
                if (Directory.Exists(root)) { Directory.Delete(root, true); }
            }
        }
    }
}
