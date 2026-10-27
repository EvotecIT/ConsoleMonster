using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace SiteFrames.Pages {
    internal static partial class GalleryPage {
        // Source publication is also useful before capture workers return. It
        // deliberately leaves frames, parity results and capture stamps alone;
        // the full render/freshness gate remains mandatory before delivery.
        private static void WritePublishedSources(Site site) {
            string path = site.Data("gallery");
            JsonObject data = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
            var entries = data["demos"]!.AsArray().ToDictionary(row => row!["slug"]!.GetValue<string>(), row => row!.AsObject());
            foreach (Demo demo in Read(site)) { WriteSources(site, demo, entries[demo.Slug]); }
            site.WriteJson(path, data);
        }

        private static void WriteSources(Site site, Demo demo, JsonObject entry) {
            JsonObject powershell = Source(demo.PowerShell, demo.Slug);
            JsonObject csharp = Source(demo.CSharp, demo.Slug);
            JsonObject fluent = Source(demo.CSharpFluent, demo.Slug);
            entry["source"] = new JsonObject { ["powershell"] = powershell, ["csharp"] = csharp, ["csharpFluent"] = fluent };
            entry["powershell"] = powershell["text"]!.GetValue<string>();
            entry["csharp"] = csharp["text"]!.GetValue<string>();
            entry["csharpFluent"] = fluent["text"]!.GetValue<string>();
            entry["apiMap"] = ApiMappingNotes.ForPair(site, demo.CSharp, demo.CSharpFluent, demo.PowerShell);
            entry["powershellRepo"] = powershell["repo"]!.GetValue<string>();
            entry["csharpRepo"] = csharp["repo"]!.GetValue<string>();
            entry["csharpFluentRepo"] = fluent["repo"]!.GetValue<string>();
            entry["powershellFile"] = demo.Slug + ".ps1";
            entry["csharpFile"] = "Program.cs";
            entry["csharpFluentFile"] = "Program.Fluent.cs";
            foreach (string file in new[] { demo.Slug + ".ps1", "Program.cs", "Program.Fluent.cs" }) {
                string target = site.Static("examples/gallery/" + demo.Slug + "/" + file);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(Path.Combine(demo.Folder, file), target, true);
            }
            entry["powershellDownload"] = "/examples/gallery/" + demo.Slug + "/" + demo.Slug + ".ps1";
            entry["csharpDownload"] = "/examples/gallery/" + demo.Slug + "/Program.cs";
            entry["csharpFluentDownload"] = "/examples/gallery/" + demo.Slug + "/Program.Fluent.cs";
            string projectTarget = site.Static("examples/gallery/" + demo.Slug + "/GalleryDemo.csproj");
            File.Copy(Path.Combine(site.Website, "examples", "gallery", "GalleryDemo.csproj.template"), projectTarget, true);
            entry["projectDownload"] = "/examples/gallery/" + demo.Slug + "/GalleryDemo.csproj";
            entry["lines"] = new JsonObject {
                ["powershell"] = File.ReadAllLines(demo.PowerShell).Length,
                ["csharp"] = File.ReadAllLines(demo.CSharp).Length,
                ["csharpFluent"] = File.ReadAllLines(demo.CSharpFluent).Length
            };
        }
    }
}
