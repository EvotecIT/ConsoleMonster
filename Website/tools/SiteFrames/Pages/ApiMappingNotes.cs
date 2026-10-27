using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Generates the three-style translation aid from resolved program symbols and the parity contract.</summary>
    internal static partial class ApiMappingNotes {
        private static readonly string[] Preferred = { "Expand", "Title", "Content", "Border", "Label", "Value", "Total", "WidgetId", "HeaderRight", "Caption", "Role" };

        public static JsonArray For(Site site, string initializer, string fluent, params string[] companions) =>
            FromSource(site, initializer, Site.ReadText(fluent), companions);

        public static JsonArray ForPair(Site site, string initializer, string fluent, string powerShell, params string[] companions) =>
            FromSource(site, initializer, Site.ReadText(fluent), companions, CommandsFor(site, powerShell));

        public static JsonArray FromSource(Site site, string initializer, string fluentSource, string[]? companions = null, HashSet<string>? commands = null) {
            companions ??= Array.Empty<string>();
            var order = CSharpRunner.ConfiguredMembersFromSource(initializer, fluentSource, companions)
                .Concat(CSharpRunner.ConfiguredMembers(initializer, companions)).Distinct(StringComparer.Ordinal).ToArray();
            var used = order.ToHashSet(StringComparer.Ordinal);
            return FromMembers(site, initializer, used, commands, order);
        }

        public static JsonArray ForProject(Site site, string initializer, string project, string fluent, HashSet<string>? commands = null) {
            var order = CSharpRunner.ConfiguredProjectMembers(initializer, project, fluent)
                .Concat(CSharpRunner.ConfiguredProjectMembers(initializer, project, Site.ReadText(initializer))).Distinct(StringComparer.Ordinal).ToArray();
            var used = order.ToHashSet(StringComparer.Ordinal);
            return FromMembers(site, initializer, used, commands, order);
        }

        private static JsonArray FromMembers(Site site, string initializer, HashSet<string> used, HashSet<string>? commands, string[] order) {
            JsonObject map = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Repository, "Tests", "Api", "round2-parity.json")))!.AsObject();
            var rows = new Dictionary<string, (string Command, JsonObject Binding)>(StringComparer.Ordinal);
            foreach (JsonNode? commandNode in map["Commands"]!.AsArray()) {
                JsonObject command = commandNode!.AsObject();
                string name = command["Command"]!.GetValue<string>();
                foreach (JsonNode? bindingNode in command["Bindings"]!.AsArray().Concat(command["SetBindings"]?.AsArray() ?? new JsonArray())) {
                    JsonObject binding = bindingNode!.AsObject();
                    if (binding["MemberKind"]!.GetValue<string>() != "Property") { continue; }
                    string member = binding["Member"]!.GetValue<string>();
                    string key = binding["CSharpType"]!.GetValue<string>() + "." + member;
                    if (!used.Contains(key)) { continue; }
                    int score = Preference(name, binding, commands);
                    if (!rows.TryGetValue(key, out var existing) || score > Preference(existing.Command, existing.Binding, commands)) {
                        rows[key] = (name, binding);
                    }
                }
            }
            foreach (JsonNode? routeNode in map["RelatedBindings"]!.AsArray()) {
                JsonObject route = routeNode!.AsObject();
                string key = route["MemberId"]!.GetValue<string>();
                if (!used.Contains(key) || rows.ContainsKey(key)) { continue; }
                string commandName = route["Command"]!.GetValue<string>();
                JsonObject command = map["Commands"]!.AsArray().Select(node => node!.AsObject())
                    .Single(node => node["Command"]!.GetValue<string>() == commandName);
                JsonObject source = command["Bindings"]!.AsArray().Select(node => node!.AsObject())
                    .Single(node => node["Parameter"]!.GetValue<string>() == route["Parameter"]!.GetValue<string>());
                JsonObject binding = (JsonObject)source.DeepClone();
                binding["CSharpType"] = route["CSharpType"]!.GetValue<string>();
                binding["Member"] = route["Member"]!.GetValue<string>();
                binding["CSharpTypeName"] = route["CSharpTypeName"]!.GetValue<string>();
                binding["Reason"] = route["Reason"]!.GetValue<string>();
                if (route["RecordField"] != null) { binding["RecordField"] = route["RecordField"]!.GetValue<string>(); }
                if (route["RecordKind"] != null) { binding["RecordKind"] = route["RecordKind"]!.GetValue<string>(); }
                rows[key] = (commandName, binding);
            }
            JsonArray result = new JsonArray();
            var positions = order.Select((key, index) => (key, index)).ToDictionary(pair => pair.key, pair => pair.index, StringComparer.Ordinal);
            int Position(string key) => positions.TryGetValue(key, out int index) ? index : int.MaxValue;
            var factoryPositions = new Dictionary<string, int>(StringComparer.Ordinal);
            JsonObject entryRule = map["FluentGrammar"]!["Entry"]!.AsObject();
            foreach (JsonNode? factoryNode in entryRule["Factories"]!.AsArray()) {
                JsonObject factory = factoryNode!.AsObject();
                string noun = factory["Noun"]!.GetValue<string>();
                if (noun == "Text" && (used.Contains("M:ConsoleMonster.Ui.Markup") || used.Contains("M:ConsoleMonster.Layout.ContentBuilder.Markup"))) { continue; }
                string command = factory["Command"]!.GetValue<string>();
                string uiId = "M:" + entryRule["FactoryType"]!.GetValue<string>() + "." + noun;
                string nestedId = "M:ConsoleMonster.Layout.ContentBuilder." + noun;
                string? constructed = factory["InitializerTypes"]!.AsArray().Select(node => node!.GetValue<string>()).Where(type => used.Contains("C:" + type)).OrderBy(type => Position("C:" + type)).FirstOrDefault();
                string methodId = used.Contains(uiId) ? uiId : used.Contains(nestedId) ? nestedId : "C:" + constructed;
                if (!used.Contains(methodId) || commands != null && !commands.Any(value => value == command || value.StartsWith(command + " -", StringComparison.Ordinal))) { continue; }
                string type = factory["InitializerTypes"]!.AsArray().Select(node => node!.GetValue<string>()).Where(type => used.Contains("C:" + type)).OrderBy(type => Position("C:" + type)).FirstOrDefault() ?? factory["ReturnType"]!.GetValue<string>();
                string initializerType = type.Substring(type.LastIndexOf('.') + 1);
                string? reason = factory["Reason"]?.GetValue<string>();
                if (factory["ContentBuilder"]!.GetValue<bool>()) { reason = (reason == null ? string.Empty : reason + " ") + "Inside Content: " + entryRule["NestedPattern"]!.GetValue<string>().Replace("{Noun}", noun, StringComparison.Ordinal).Replace("{Parameter}", CSharpRunner.BuilderParameter(noun), StringComparison.Ordinal) + "."; }
                factoryPositions[methodId] = Math.Min(Position(methodId), Position("C:" + type));
                bool tableRowsUsed = noun == "Table" && used.Contains("M:ConsoleMonster.Widgets.Table.For");
                if (tableRowsUsed) { factoryPositions[methodId] = Math.Min(factoryPositions[methodId], Position("M:ConsoleMonster.Widgets.Table.For")); }
                JsonObject? discriminator = factory["Discriminators"]?.AsArray().Select(node => node!.AsObject())
                    .FirstOrDefault(rule => rule["InitializerType"]!.GetValue<string>() == type);
                string pattern = tableRowsUsed ? "Ui.Table(rows)" : factory["Pattern"]!.GetValue<string>();
                if (tableRowsUsed) { command += " -InputObject $rows -Property …"; }
                if (discriminator != null) {
                    bool flag = discriminator["Switch"]?.GetValue<bool>() == true;
                    command += " -" + discriminator["Parameter"]!.GetValue<string>() + (flag ? string.Empty : " " + discriminator["Value"]!.GetValue<string>());
                    pattern += "." + discriminator["Member"]!.GetValue<string>() + "(" + (flag ? string.Empty : discriminator["EnumType"]!.GetValue<string>() + "." + discriminator["Value"]!.GetValue<string>()) + ")";
                }
                result.Add(new JsonObject { ["powershell"] = command + (discriminator == null ? KindArgument(map, command, initializerType) : string.Empty), ["csharp"] = noun == "Table" && used.Contains("M:ConsoleMonster.Widgets.Table.For") ? "Table.For(rows)" : "new " + initializerType + "(…)", ["fluent"] = pattern, ["memberId"] = methodId, ["note"] = reason });
            }
            JsonObject markup = map["FluentGrammar"]!["Markup"]!.AsObject();
            if (used.Contains("M:ConsoleMonster.Ui.Markup") || used.Contains("M:ConsoleMonster.Layout.ContentBuilder.Markup")) {
                result.Add(new JsonObject {
                    ["powershell"] = markup["PowerShellPattern"]!.GetValue<string>(),
                    ["csharp"] = markup["InitializerPattern"]!.GetValue<string>(),
                    ["fluent"] = markup["FluentPattern"]!.GetValue<string>() + "; " + markup["NestedPattern"]!.GetValue<string>(),
                    ["memberId"] = used.Contains("M:ConsoleMonster.Ui.Markup") ? "M:ConsoleMonster.Ui.Markup" : "M:ConsoleMonster.Layout.ContentBuilder.Markup", ["note"] = markup["Reason"]!.GetValue<string>()
                });
            }
            var contentCommands = new HashSet<string>(StringComparer.Ordinal);
            JsonObject contentRule = map["FluentGrammar"]!["Content"]!.AsObject();
            foreach (JsonNode? contentNode in contentRule["Bindings"]!.AsArray()) {
                JsonObject binding = contentNode!.AsObject();
                string methodId = "M:" + binding["FluentType"]!.GetValue<string>() + ".Content";
                string command = binding["Command"]!.GetValue<string>();
                if (!used.Contains(methodId) || commands != null && !commands.Contains(command + " -Content")) { continue; }
                contentCommands.Add(command);
                result.Add(new JsonObject {
                    ["powershell"] = command + " " + contentRule["PowerShellPattern"]!.GetValue<string>(),
                    ["csharp"] = binding["InitializerPattern"]!.GetValue<string>(),
                    ["fluent"] = contentRule["FluentPattern"]!.GetValue<string>(), ["memberId"] = methodId
                });
            }
            foreach (JsonNode? optionNode in map["FluentGrammar"]!["OperationOptions"]!.AsArray()) {
                JsonObject operation = optionNode!.AsObject();
                string owner = operation["CSharpType"]!.GetValue<string>();
                string operationId = "M:" + owner + "." + operation["Member"]!.GetValue<string>();
                string optionsType = operation["OptionsType"]!.GetValue<string>();
                var optionRows = rows.Values.Where(row => row.Binding["CSharpType"]!.GetValue<string>() == optionsType)
                    .OrderBy(row => Rank(row.Binding["Member"]!.GetValue<string>())).Take(2).ToArray();
                if (!used.Contains(operationId) || optionRows.Length == 0) { continue; }
                string chain = "options => options" + string.Concat(optionRows.Select(row => {
                    string member = row.Binding["Member"]!.GetValue<string>();
                    string type = row.Binding["CSharpTypeName"]!.GetValue<string>();
                    if (type.StartsWith("System.Action", StringComparison.Ordinal) || type.StartsWith("System.Func", StringComparison.Ordinal)) {
                        JsonObject exception = map["FluentExceptions"]!.AsArray().Single(rule => rule!["Kind"]!.GetValue<string>() == "DelegateProperty")!.AsObject();
                        return exception["Pattern"]!.GetValue<string>().Replace("{Member}", member, StringComparison.Ordinal);
                    }
                    string fluentMember = type == "System.Boolean" && member.Length > 2 && member.StartsWith("Is", StringComparison.Ordinal) && char.IsUpper(member[2]) ? member.Substring(2) : member;
                    return "." + fluentMember + "(…)";
                }));
                result.Add(new JsonObject {
                    ["powershell"] = optionRows[0].Command + string.Concat(optionRows.Select(row => " -" + row.Binding["Parameter"]!.GetValue<string>() + " …")),
                    ["csharp"] = "new " + optionsType.Substring(optionsType.LastIndexOf('.') + 1) + " { … }",
                    ["fluent"] = OperationPattern(operation, chain),
                    ["memberId"] = operationId, ["note"] = operation["Reason"]!.GetValue<string>()
                });
                break;
            }
            foreach (var row in rows.Where(pair => pair.Value.Binding["Parameter"]!.GetValue<string>() != "Content" || !contentCommands.Contains(pair.Value.Command))
                .OrderBy(pair => Rank(pair.Value.Binding["Member"]!.GetValue<string>())).ThenBy(pair => pair.Key, StringComparer.Ordinal)) {
                JsonObject binding = row.Value.Binding;
                string parameter = binding["Parameter"]!.GetValue<string>();
                if (parameter == "Type" && result.Any(note => note!["powershell"]!.GetValue<string>().StartsWith(row.Value.Command + " -Type ", StringComparison.Ordinal))) { continue; }
                string member = binding["Member"]!.GetValue<string>();
                string conversion = binding["Conversion"]!.GetValue<string>();
                bool isSwitch = binding["ParameterType"]!.GetValue<string>() == "System.Management.Automation.SwitchParameter";
                string value = conversion == "BooleanInversion" ? "false" : isSwitch && binding["CSharpTypeName"]?.GetValue<string>() == "System.Boolean" ? "true" : conversion == "SecondsToTimeSpan" ? "TimeSpan.FromSeconds(…)"
                    : conversion == "MillisecondsToTimeSpan" ? "TimeSpan.FromMilliseconds(…)" : "…";
                string? recordField = binding["RecordField"]?.GetValue<string>();
                string recordKind = binding["RecordKind"] == null ? string.Empty : "Kind = '" + binding["RecordKind"]!.GetValue<string>() + "'; ";
                string? note = conversion == "Identity" ? null : binding["Reason"]?.GetValue<string>() ?? conversion;
                string? valueType = binding["CSharpTypeName"]?.GetValue<string>();
                bool callback = valueType?.StartsWith("System.Action", StringComparison.Ordinal) == true || valueType?.StartsWith("System.Func", StringComparison.Ordinal) == true;
                JsonObject? callbackRule = callback ? map["FluentExceptions"]!.AsArray().Single(rule => rule!["Kind"]!.GetValue<string>() == "DelegateProperty")!.AsObject() : null;
                string? collectionAdd = binding["FluentMember"]?.GetValue<string>();
                bool contentBuilder = parameter == "Content" && used.Any(key => key.StartsWith("M:", StringComparison.Ordinal) && key.EndsWith("Fluent.Content", StringComparison.Ordinal));
                bool boolSwitch = isSwitch && valueType == "System.Boolean";
                string fluentMember = valueType == "System.Boolean" && member.Length > 2 && member.StartsWith("Is", StringComparison.Ordinal) && char.IsUpper(member[2]) ? member.Substring(2) : member;
                if (callbackRule != null) { note = (note == null ? string.Empty : note + " ") + callbackRule["Reason"]!.GetValue<string>(); }
                if (valueType != null && typeof(ConsoleMonster.Terminal).Assembly.GetType(valueType)?.IsEnum == true) {
                    string owner = binding["CSharpType"]!.GetValue<string>();
                    string typedNote = owner.Substring(owner.LastIndexOf('.') + 1) + "." + member + " uses " + valueType + ".";
                    note = note == null ? typedNote : note + " " + typedNote;
                }
                result.Add(new JsonObject {
                    ["powershell"] = recordField == null ? row.Value.Command + (binding["ParameterSet"]?.GetValue<string>() == "Run" ? " -Run …" : string.Empty) + (contentBuilder ? " " + map["FluentGrammar"]!["Content"]!["PowerShellPattern"]!.GetValue<string>() : " -" + parameter + (isSwitch ? string.Empty : " …"))
                        : "@{ " + recordKind + recordField + " = … } | " + row.Value.Command,
                    ["csharp"] = member + " = " + (collectionAdd == null ? value : "{ " + value + " }"),
                    ["fluent"] = contentBuilder ? map["FluentGrammar"]!["Content"]!["FluentPattern"]!.GetValue<string>()
                        : boolSwitch ? map["FluentGrammar"]!["Boolean"]![conversion == "BooleanInversion" ? "DisabledPattern" : "EnabledPattern"]!.GetValue<string>().Replace("{Member}", fluentMember, StringComparison.Ordinal)
                        : collectionAdd != null ? "." + collectionAdd + "(" + value + ")"
                        : callbackRule == null ? "." + fluentMember + "(" + value + ")" : callbackRule["Pattern"]!.GetValue<string>().Replace("{Member}", member, StringComparison.Ordinal),
                    ["memberId"] = row.Key,
                    ["note"] = note
                });
            }
            JsonObject tableRows = map["FluentGrammar"]!["TableRows"]!.AsObject();
            string rowFactory = "M:" + tableRows["CSharpType"]!.GetValue<string>() + "." + tableRows["Member"]!.GetValue<string>();
            if (used.Contains(rowFactory) && !result.Any(row => row!["memberId"]!.GetValue<string>() == "M:ConsoleMonster.Ui.Table")) {
                result.Add(new JsonObject {
                    ["powershell"] = "New-ConsoleTable -InputObject $rows -Property …",
                    ["csharp"] = tableRows["Pattern"]!.GetValue<string>(), ["fluent"] = tableRows["FluentPattern"]!.GetValue<string>(),
                    ["memberId"] = rowFactory, ["note"] = tableRows["Reason"]!.GetValue<string>()
                });
            }
            foreach (JsonNode? commandNode in map["Commands"]!.AsArray()) {
                JsonObject command = commandNode!.AsObject();
                foreach (JsonNode? bindingNode in command["Bindings"]!.AsArray().Concat(command["SetBindings"]?.AsArray() ?? new JsonArray())) {
                    JsonObject binding = bindingNode!.AsObject();
                    string? argument = binding["Argument"]?.GetValue<string>();
                    if (argument == null || binding["MemberKind"]!.GetValue<string>() is not ("Method" or "Constructor")) { continue; }
                    string owner = binding["CSharpType"]!.GetValue<string>();
                    string method = binding["Member"]!.GetValue<string>();
                    string key = "A:" + owner + "." + method + "." + argument;
                    if (!used.Contains(key)) { continue; }
                    bool isSwitch = binding["ParameterType"]!.GetValue<string>() == "System.Management.Automation.SwitchParameter";
                    string conversion = binding["Conversion"]!.GetValue<string>();
                    string value = conversion == "BooleanInversion" ? "false" : isSwitch ? "true"
                        : conversion == "SecondsToTimeSpan" ? "TimeSpan.FromSeconds(…)" : "…";
                    string call = owner.Substring(owner.LastIndexOf('.') + 1);
                    call = method == ".ctor" ? "new " + call : call + "." + method;
                    call += "(…)";
                    result.Add(new JsonObject {
                        ["powershell"] = command["Command"]!.GetValue<string>() + " -" + binding["Parameter"]!.GetValue<string>() + (isSwitch ? string.Empty : " …"),
                        ["csharp"] = call,
                        ["fluent"] = call,
                        ["memberId"] = key,
                        ["note"] = (binding["Reason"]?.GetValue<string>() ?? string.Empty) + " The " + argument + " argument receives " + value + "."
                    });
                }
            }
            foreach (JsonNode? endingNode in map["Endings"]!.AsArray()) {
                JsonObject ending = endingNode!.AsObject();
                string owner = ending["CSharpType"]!.GetValue<string>();
                string method = ending["Member"]!.GetValue<string>();
                string fluentOwner = ending["FluentType"]!.GetValue<string>();
                string fluentMethod = ending["FluentMember"]!.GetValue<string>();
                string key = "M:" + owner + "." + method;
                if (!used.Contains(key) && !used.Contains("M:" + fluentOwner + "." + fluentMethod)) { continue; }
                string? argument = ending["FluentArgument"]?.GetValue<string>();
                result.Add(new JsonObject {
                    ["powershell"] = "| " + ending["Command"]!.GetValue<string>() + (argument == null ? string.Empty : " -" + char.ToUpperInvariant(argument[0]) + argument.Substring(1) + " …"),
                    ["csharp"] = owner.Substring(owner.LastIndexOf('.') + 1) + "." + method + "(widget" + (argument == null ? string.Empty : ", …") + ")",
                    ["fluent"] = "." + fluentMethod + "(" + (argument == null ? string.Empty : "…") + ")",
                    ["memberId"] = used.Contains(key) ? key : "M:" + fluentOwner + "." + fluentMethod
                });
            }
            foreach (JsonNode? operationNode in map["Operations"]?.AsArray() ?? new JsonArray()) {
                JsonObject operation = operationNode!.AsObject();
                string owner = operation["CSharpType"]!.GetValue<string>();
                string member = operation["Member"]!.GetValue<string>();
                string key = "M:" + owner + "." + member;
                if (!used.Contains(key)) { continue; }
                string shortOwner = owner.Substring(owner.LastIndexOf('.') + 1);
                result.Add(new JsonObject {
                    ["powershell"] = "… | " + operation["Command"]!.GetValue<string>() + " -" + operation["DiscriminatorParameter"]!.GetValue<string>() + " " + operation["DiscriminatorValue"]!.GetValue<string>(),
                    ["csharp"] = shortOwner + "." + member + "(…)",
                    ["fluent"] = shortOwner + "." + operation["FluentFactory"]!.GetValue<string>() + "(…)." + operation["FluentMember"]!.GetValue<string>() + "()",
                    ["memberId"] = key,
                    ["note"] = operation["Reason"]!.GetValue<string>()
                });
            }
            foreach (JsonNode? externalNode in map["ExternalOperations"]?.AsArray() ?? new JsonArray()) {
                JsonObject external = externalNode!.AsObject();
                string owner = external["CSharpType"]!.GetValue<string>();
                string method = external["Member"]!.GetValue<string>();
                string key = "M:" + owner + "." + method;
                if (!used.Contains(key)) { continue; }
                string call = owner.Substring(owner.LastIndexOf('.') + 1) + "." + method + "(…)";
                result.Add(new JsonObject {
                    ["powershell"] = external["Command"]!.GetValue<string>() + " …",
                    ["csharp"] = call, ["fluent"] = call, ["memberId"] = key,
                    ["note"] = external["Reason"]!.GetValue<string>()
                });
            }
            foreach (JsonNode? integrationNode in map["IntegrationOptions"]?.AsArray() ?? new JsonArray()) {
                JsonObject integration = integrationNode!.AsObject();
                string owner = integration["CSharpType"]!.GetValue<string>();
                foreach (JsonNode? memberNode in integration["Members"]!.AsArray()) {
                    string member = memberNode!.GetValue<string>();
                    string key = owner + "." + member;
                    if (!used.Contains(key)) { continue; }
                    result.Add(new JsonObject {
                        ["powershell"] = "dotnet run", ["csharp"] = member + " = …", ["fluent"] = "." + member + "(…)",
                        ["memberId"] = key, ["note"] = integration["Reason"]!.GetValue<string>()
                    });
                }
            }
            foreach (JsonNode? integrationNode in map["IntegrationOperations"]?.AsArray() ?? new JsonArray()) {
                JsonObject integration = integrationNode!.AsObject();
                string owner = integration["CSharpType"]!.GetValue<string>();
                string member = integration["Member"]!.GetValue<string>();
                string key = "M:" + owner + "." + member;
                if (!used.Contains(key)) { continue; }
                string call = owner.Substring(owner.LastIndexOf('.') + 1) + "." + member + "(…)";
                result.Add(new JsonObject {
                    ["powershell"] = "dotnet run", ["csharp"] = call, ["fluent"] = call, ["memberId"] = key,
                    ["note"] = integration["Reason"]!.GetValue<string>()
                });
            }
            if (result.Count == 0) { throw new InvalidDataException("No parity mapping is available for " + initializer + "."); }
            int SourcePosition(JsonNode? row) {
                string key = row!["memberId"]!.GetValue<string>();
                return factoryPositions.TryGetValue(key, out int index) ? index : Position(key);
            }
            // Reserve the compact aid for construction, nesting, markup and
            // switches before filling it with scalar options. Display order is
            // still the program's order, never this selection preference.
            var required = new HashSet<string>(StringComparer.Ordinal);
            void RequireFirst(Func<JsonNode?, bool> predicate) {
                var row = result.Where(predicate).OrderBy(SourcePosition).FirstOrDefault();
                if (row != null) { required.Add(row["memberId"]!.GetValue<string>()); }
            }
            RequireFirst(row => factoryPositions.ContainsKey(row!["memberId"]!.GetValue<string>()));
            RequireFirst(row => row!["memberId"]!.GetValue<string>().EndsWith(".Content", StringComparison.Ordinal));
            RequireFirst(row => row!["memberId"]!.GetValue<string>().EndsWith(".Markup", StringComparison.Ordinal));
            RequireFirst(row => row!["csharp"]!.GetValue<string>().EndsWith(" = true", StringComparison.Ordinal) || row["csharp"]!.GetValue<string>().EndsWith(" = false", StringComparison.Ordinal));
            foreach (var row in result.Where(row => factoryPositions.ContainsKey(row!["memberId"]!.GetValue<string>()) && row["powershell"]!.GetValue<string>().Contains(" -Type ", StringComparison.Ordinal))) { required.Add(row!["memberId"]!.GetValue<string>()); }
            bool Essential(JsonNode? row) => required.Contains(row!["memberId"]!.GetValue<string>());
            return new JsonArray(result.OrderBy(row => Essential(row) ? 0 : 1).ThenBy(SourcePosition)
                .Take(6).OrderBy(SourcePosition).Select(row => row!.DeepClone()).ToArray());
        }

        // Resolve the configure overload itself: app state and export format follow
        // its callback, whereas the ordinary options overload puts them before it.
        private static string OperationPattern(JsonObject operation, string configure) {
            Type owner = typeof(ConsoleMonster.Terminal).Assembly.GetType(operation["CSharpType"]!.GetValue<string>())!;
            Type options = owner.Assembly.GetType(operation["OptionsType"]!.GetValue<string>())!;
            var method = owner.GetMethods().Single(candidate => candidate.Name == operation["Member"]!.GetValue<string>()
                && candidate.GetParameters().Any(parameter => parameter.ParameterType.IsGenericType
                    && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Action<>)
                    && parameter.ParameterType.GetGenericArguments()[0] == options));
            bool extension = method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false);
            var parameters = method.GetParameters().Skip(extension ? 1 : 0)
                .Where(parameter => parameter.ParameterType != typeof(System.Threading.CancellationToken));
            string receiver = extension || owner == typeof(ConsoleMonster.UiConstruction) ? "widget"
                : method.IsStatic ? owner.Name : char.ToLowerInvariant(owner.Name[0]) + owner.Name.Substring(1);
            return receiver + "." + method.Name + "(" + string.Join(", ", parameters.Select(parameter =>
                parameter.Name == operation["Argument"]!.GetValue<string>() ? configure : parameter.Name)) + ")";
        }

        // One command can build several widget types (New-ConsoleChart builds a Sparkline with -Type Sparkline). The row names
        // the -Type value whose enum name starts the constructed type's name, so the C# cell is not read as the default kind.
        private static string KindArgument(JsonObject map, string command, string initializerType) {
            JsonObject? binding = map["Commands"]!.AsArray().Select(node => node!.AsObject())
                .Where(node => node["Command"]!.GetValue<string>() == command)
                .SelectMany(node => node["Bindings"]!.AsArray()).Select(node => node!.AsObject())
                .FirstOrDefault(node => node["Parameter"]!.GetValue<string>() == "Type");
            Type? kinds = binding?["CSharpTypeName"] == null ? null : typeof(ConsoleMonster.Terminal).Assembly.GetType(binding["CSharpTypeName"]!.GetValue<string>());
            if (kinds?.IsEnum != true) { return string.Empty; }
            string? kind = Enum.GetNames(kinds).Where(name => initializerType.StartsWith(name, StringComparison.Ordinal)).OrderByDescending(name => name.Length).FirstOrDefault();
            return kind == null ? string.Empty : " -Type " + kind;
        }

        private static int Preference(string command, JsonObject binding, HashSet<string>? commands) =>
            (commands?.Contains(command + " -" + binding["Parameter"]!.GetValue<string>()) == true ? 4 : 0)
            + (commands?.Contains(command) == true ? 2 : 0)
            + (command.StartsWith("New-", StringComparison.Ordinal) ? 1 : 0);

        private static int Rank(string member) {
            int index = Array.IndexOf(Preferred, member);
            return index < 0 ? Preferred.Length : index;
        }
    }
}
