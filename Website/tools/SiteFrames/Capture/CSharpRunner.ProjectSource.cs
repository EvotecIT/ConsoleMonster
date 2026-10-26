using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        /// <summary>Compiles a project's generated source against that project's existing package and project references.</summary>
        public static void CheckProjectSources(string path, string project, IReadOnlyDictionary<string, string> sources, params string[] companions) =>
            Emit(path, sources[path], companions: companions, sourceOverrides: sources, references: ProjectReferences(project));

        public static IReadOnlyDictionary<string, string> FluentProjectSources(string path, string project, params string[] companions) =>
            FluentSourcesCore(path, companions, ProjectReferences(project));

        /// <summary>Reads public integration options from the compiled add-on metadata used by the example.</summary>
        public static IReadOnlyList<string> ProjectOptionMembers(string path, string project, string typeName) {
            Compilation compilation = CreateCompilation(path, Site.ReadText(path), references: ProjectReferences(project));
            INamedTypeSymbol type = compilation.GetTypeByMetadataName(typeName) ?? throw new InvalidOperationException("Unknown integration option type: " + typeName);
            return type.GetMembers().OfType<IPropertySymbol>().Where(property => property.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                .Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        }

        private static IReadOnlyList<MetadataReference> ProjectReferences(string project) {
            string directory = Path.GetDirectoryName(project)!;
            string configuration = new FileInfo(typeof(CSharpRunner).Assembly.Location).Directory!.Parent!.Name;
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            void Add(string path) {
                if (!File.Exists(path)) { throw new FileNotFoundException("Build the example project before checking its shown sources: " + project, path); }
                paths.TryAdd(Path.GetFileName(path), path);
            }
            foreach (MetadataReference reference in References) { if (reference.Display != null) { Add(reference.Display); } }
            JsonObject assets = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "obj", "project.assets.json")))!.AsObject();
            JsonObject target = assets["targets"]!["net8.0"]!.AsObject();
            foreach (var library in target) {
                string? relative = assets["libraries"]![library.Key]?["path"]?.GetValue<string>();
                if (relative == null || assets["libraries"]![library.Key]!["type"]?.GetValue<string>() != "package" || library.Value?["compile"] is not JsonObject compile) { continue; }
                foreach (string file in compile.Select(pair => pair.Key).Where(file => file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))) {
                    string resolved = assets["packageFolders"]!.AsObject().Select(folder => Path.Combine(folder.Key, relative, file)).First(File.Exists);
                    Add(resolved);
                }
            }
            foreach (XElement reference in XDocument.Load(project).Descendants("ProjectReference")) {
                string included = Path.GetFullPath(Path.Combine(directory, reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar)));
                Add(Path.Combine(Path.GetDirectoryName(included)!, "bin", configuration, "net8.0", Path.GetFileNameWithoutExtension(included) + ".dll"));
            }
            return paths.Values.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        }
    }
}
