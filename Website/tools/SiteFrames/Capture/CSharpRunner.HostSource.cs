using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace SiteFrames.Capture {
    internal static partial class CSharpRunner {
        // Compiled cmdlet examples run in an owning PowerShell runspace, not as console entry points.
        // Read the module's existing reference contract; the site tool does not load a PowerShell runtime.
        private static bool IsHostSource(string path) => path.EndsWith(".host.cs", StringComparison.Ordinal);

        /// <summary>Reads the actual compiled example's cmdlet name for its run instructions.</summary>
        public static string HostCommand(string path) {
            Compilation compilation = CreateCompilation(path, Site.ReadText(path));
            var declaration = compilation.SyntaxTrees.First(tree => tree.FilePath == path).GetRoot().DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().Single();
            INamedTypeSymbol type = (INamedTypeSymbol)compilation.GetSemanticModel(declaration.SyntaxTree).GetDeclaredSymbol(declaration)!;
            AttributeData command = type.GetAttributes().Single(attribute => attribute.AttributeClass?.ToDisplayString() == "System.Management.Automation.CmdletAttribute");
            return command.ConstructorArguments[0].Value + "-" + command.ConstructorArguments[1].Value;
        }

        private static IReadOnlyList<MetadataReference> ReferencesFor(string path) {
            if (!IsHostSource(path)) { return References; }
            DirectoryInfo? repository = new FileInfo(path).Directory;
            while (repository != null && !File.Exists(Path.Combine(repository.FullName, "ConsoleMonster.sln"))) { repository = repository.Parent; }
            if (repository == null) { throw new InvalidOperationException("A compiled cmdlet example must belong to the repository: " + path); }
            string module = Path.Combine(repository.FullName, "ConsoleMonster.PowerShell");
            JsonObject assets = JsonNode.Parse(File.ReadAllText(Path.Combine(module, "obj", "project.assets.json")))!.AsObject();
            JsonObject target = assets["targets"]!["net8.0"]!.AsObject();
            string package = target.Select(pair => pair.Key).Single(name => name.StartsWith("PowerShellStandard.Library/", StringComparison.OrdinalIgnoreCase));
            string relative = assets["libraries"]![package]!["path"]!.GetValue<string>();
            string reference = target[package]!["compile"]!.AsObject().Select(pair => pair.Key).Single(name => name.EndsWith("/System.Management.Automation.dll", StringComparison.Ordinal));
            string automation = assets["packageFolders"]!.AsObject().Select(pair => Path.Combine(pair.Key, relative, reference)).Single(File.Exists);
            string configuration = new FileInfo(typeof(CSharpRunner).Assembly.Location).Directory!.Parent!.Name;
            return References.Concat(new[] {
                MetadataReference.CreateFromFile(Path.Combine(module, "bin", configuration, "net8.0", "ConsoleMonster.PowerShell.dll")),
                MetadataReference.CreateFromFile(automation)
            }).ToArray();
        }
    }
}
