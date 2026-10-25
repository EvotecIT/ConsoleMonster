using System;
using System.Collections.Generic;
using System.IO;
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ConsoleMonster.Data;
using ConsoleMonster.Export;
using ConsoleMonster.Widgets;

// Run the published native executable: a normal managed test cannot prove the linker kept these paths working.
internal static class Program {
    private sealed class Server {
        public string Name { get; set; } = "web-01";
        public int Count { get; set; } = 42;
    }

    private static int Main() {
        var table = Table.For(new[] { new Server() });
        table.Column(server => server.Name).Column(server => server.Count);
        Require(ConsoleExporter.ToText(table), "web-01", "42");

        // Also exercise inferred columns, whose property preservation is part of Table<T>'s contract.
        Require(ConsoleExporter.ToText(Table.For(new[] { new Server() })), "web-01", "42");
        Require(ConsoleExporter.ToText(new InspectorView(InspectPreservedServer())), "web-01", "42");

        using JsonDocument json = JsonDocument.Parse("{\"Name\":\"json-server\",\"Count\":23}");
        Require(ConsoleExporter.ToText(new InspectorView(InspectWithoutReflection(json.RootElement))), "json-server", "23");
        var pairs = new[] { new KeyValuePair<string, object>("Name", "pair-server") };
        Require(ConsoleExporter.ToText(new InspectorView(InspectWithoutReflection(pairs))), "Name", "pair-server");
        Require(ConsoleExporter.ToHtml(table), "web-01");
        Require(ConsoleExporter.ToSvg(table), "web-01");
        GraphicsSmoke.Run();
        Panel panel = new Panel().Title("Build").Content(content => content.Text("All checks passed"));
        Require(ConsoleExporter.ToText(panel), "Build", "All checks passed");
        var profile = TerminalProfile.Fixed(60, 12);
        var target = new RecordingTarget(profile);
        panel.Show(profile, target.Writer);
        Require(target.Output, "Build", "All checks passed");
        string path = Path.Combine(Path.GetTempPath(), "consolemonster-aot-" + Guid.NewGuid().ToString("N") + ".txt");
        try {
            panel.Export(path, "Text", new ExportOptions { Width = 60 });
            Require(File.ReadAllText(path), "Build", "All checks passed");
        } finally { File.Delete(path); }
        Console.WriteLine("Native AOT smoke passed: typed and inferred tables, inspection, fluent content, Show/Export endings and text/HTML/SVG exports.");
        return 0;
    }

    // Inspect warns for arbitrary objects even when reflection is disabled. This call supplies only JSON and
    // string/object pairs and explicitly disables the reflection fallback; no application properties are reflected.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only built-in JSON and dictionary member sources are used, with UseReflection=false.")]
    private static InspectNode InspectWithoutReflection(object value) =>
        ObjectInspector.Inspect(value, new InspectorOptions { UseReflection = false });

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Server))]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This fixture explicitly preserves all Server properties for reflective inspection.")]
    private static InspectNode InspectPreservedServer() => ObjectInspector.Inspect(new Server());

    private static void Require(string output, params string[] values) {
        foreach (string value in values)
            if (!output.Contains(value, StringComparison.Ordinal))
                throw new InvalidOperationException("Native output is missing: " + value);
    }
}
