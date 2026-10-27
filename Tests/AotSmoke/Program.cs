using System;
using System.Collections.Generic;
using System.IO;
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Live;
using ConsoleMonster.Interactive;
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
        var table = Ui.Table(new[] { new Server() });
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
        int previews = 0;
        var question = new SelectQuestionOptions().Configure(options => options.Preview = _ => { previews++; return new TextBlock("Preview"); });
        if (previews != 0 || question.Preview == null) { throw new InvalidOperationException("Fluent callback configuration invoked or lost the delegate."); }
        var progress = new ProgressBar("Build", "build");
        using (var session = new LiveSession(progress)) {
            ConsoleUpdate.Apply(session, "build", new WidgetUpdate { Value = 5, Total = 10, Label = "Compile" });
            if (session.EventsPosted != 2) { throw new InvalidOperationException("The native update path did not publish its validated fields."); }
            try {
                ConsoleUpdate.Apply(session, "build", new WidgetUpdate { Label = "Invalid", Detail = "Unsupported" });
                throw new InvalidOperationException("The native update path accepted an unsupported field.");
            } catch (ConsoleUpdateException) { }
            if (session.EventsPosted != 2) { throw new InvalidOperationException("The native update path partially published an invalid batch."); }
        }
        using (var session = new LiveSession(new CellMap("cells"))) {
            ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { CellCount = CellMap.MaxItems - 1 });
            try {
                ConsoleUpdate.Apply(session, "cells", new WidgetUpdate { AppendCells = true, Count = 2 });
                throw new InvalidOperationException("The native update path exceeded queued cell capacity.");
            } catch (ArgumentOutOfRangeException) { }
            if (session.EventsPosted != 1) { throw new InvalidOperationException("The native update path partially published an oversized append."); }
        }
        Panel panel = Ui.Panel().Title("Build").Content(content => content.Text("All checks passed"));
        var field = new NumberField("Attempts").Integer().Required();
        if (!field.Integer || !field.Required) { throw new InvalidOperationException("Boolean fluent switches did not enable the field options."); }
        field.Integer(false).Required(false);
        if (field.Integer || field.Required) { throw new InvalidOperationException("Explicit false did not disable the field options."); }
        Require(ConsoleExporter.ToText(Ui.Rows().Content(c => { c.Rule("Build"); c.Text("Ready"); })), "Build", "Ready");
        Require(ConsoleExporter.ToText(panel), "Build", "All checks passed");
        var calendar = Ui.Calendar().Year(2026).Month(10).Culture(System.Globalization.CultureInfo.InvariantCulture).Today(new DateTime(2026, 10, 20)).ShowLegend(false);
        Require(ConsoleExporter.ToText(calendar), "October", "2026");
        var scroll = Ui.ScrollView().MaxContentHeight(100).Content(content => content.Text("Scrollable content"));
        Require(ConsoleExporter.ToText(scroll), "Scrollable content");
        var command = Ui.Command().Key("check").Label("Run check").Configure(definition => definition.Action = (_, __) => System.Threading.Tasks.Task.CompletedTask).Build();
        var button = Ui.Button().Key("run").Command(command).Build();
        var typedProgress = Ui.Progress().WidgetId("fluent-progress").Label("Build").Value(5).Total(10).Build<ProgressBar>();
        Require(ConsoleExporter.ToText(typedProgress), "Build");
        Require(ConsoleExporter.ToText(Ui.Rows().Content(content => {
            content.Panel(panel => panel.Title("Nested").Content(children => children.Text("literal [text]")));
            content.Chart(chart => chart.Type(ChartType.Sparkline).Values(new double[] { 1, 2, 3 }));
        })), "Nested", "literal [text]");
        var appCapture = new ScreenCapture(60, 12);
        var appOptions = Ui.Options<ConsoleAppOptions>().StaticSnapshot(true).SelectionKey("picked")
            .Interactive(options => { options.Capture = appCapture; options.Profile = TerminalProfile.Fixed(60, 12); });
        appOptions.AddCommand(command);
        ConsoleApp.Run(new ScrollView().Content(content => content.Add(button)), options: appOptions);
        Require(appCapture.LastFrame!.ToPlainText(), "Run check");
        var profile = TerminalProfile.Fixed(60, 12);
        var target = new RecordingTarget(profile);
        panel.Show(profile, target.Writer);
        Require(target.Output, "Build", "All checks passed");
        string path = Path.Combine(Path.GetTempPath(), "consolemonster-aot-" + Guid.NewGuid().ToString("N") + ".txt");
        try {
            panel.Export(path, "Text", new ExportOptions { Width = 60 });
            Require(File.ReadAllText(path), "Build", "All checks passed");
        } finally { File.Delete(path); }
        RecordingSmoke.Run();
        Console.WriteLine("Native AOT smoke passed: typed and inferred tables, inspection, validated live updates, queued cell capacity, fluent content, Show/Export endings, text/HTML/SVG and PNG/GIF/APNG exports, long recording and secret redaction.");
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
