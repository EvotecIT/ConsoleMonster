using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Generates the three-style translation aid from resolved program symbols and the parity contract.</summary>
    internal static partial class ApiMappingNotes {
        private static readonly string[] Preferred = { "Title", "Border", "Content", "Label", "Value", "Total", "WidgetId", "HeaderRight", "Caption", "Role" };

        public static JsonArray For(Site site, string initializer, string fluent, params string[] companions) =>
            FromSource(site, initializer, Site.ReadText(fluent), companions);

        public static JsonArray ForPair(Site site, string initializer, string fluent, string powerShell, params string[] companions) =>
            FromSource(site, initializer, Site.ReadText(fluent), companions, CommandsFor(site, powerShell));

        public static JsonArray FromSource(Site site, string initializer, string fluentSource, string[]? companions = null, HashSet<string>? commands = null) {
            companions ??= Array.Empty<string>();
            var used = new HashSet<string>(CSharpRunner.ConfiguredMembers(initializer, companions), StringComparer.Ordinal);
            used.UnionWith(CSharpRunner.ConfiguredMembersFromSource(initializer, fluentSource, companions));
            return FromMembers(site, initializer, used, commands);
        }

        public static JsonArray ForProject(Site site, string initializer, string project, string fluent, HashSet<string>? commands = null) {
            var used = new HashSet<string>(CSharpRunner.ConfiguredProjectMembers(initializer, project, Site.ReadText(initializer)), StringComparer.Ordinal);
            used.UnionWith(CSharpRunner.ConfiguredProjectMembers(initializer, project, fluent));
            return FromMembers(site, initializer, used, commands);
        }

        private static JsonArray FromMembers(Site site, string initializer, HashSet<string> used, HashSet<string>? commands) {
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
            foreach (var row in rows.OrderBy(pair => Rank(pair.Value.Binding["Member"]!.GetValue<string>())).ThenBy(pair => pair.Key, StringComparer.Ordinal).Take(6)) {
                JsonObject binding = row.Value.Binding;
                string parameter = binding["Parameter"]!.GetValue<string>();
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
                if (callbackRule != null) { note = (note == null ? string.Empty : note + " ") + callbackRule["Reason"]!.GetValue<string>(); }
                if (valueType != null && typeof(ConsoleMonster.Terminal).Assembly.GetType(valueType)?.IsEnum == true) {
                    string owner = binding["CSharpType"]!.GetValue<string>();
                    string typedNote = owner.Substring(owner.LastIndexOf('.') + 1) + "." + member + " uses " + valueType + ".";
                    note = note == null ? typedNote : note + " " + typedNote;
                }
                result.Add(new JsonObject {
                    ["powershell"] = recordField == null ? row.Value.Command + (binding["ParameterSet"]?.GetValue<string>() == "Run" ? " -Run …" : string.Empty) + " -" + parameter + (isSwitch ? string.Empty : " …")
                        : "@{ " + recordKind + recordField + " = … } | " + row.Value.Command,
                    ["csharp"] = member + " = " + (collectionAdd == null ? value : "{ " + value + " }"),
                    ["fluent"] = collectionAdd != null ? "." + collectionAdd + "(" + value + ")"
                        : callbackRule == null ? "." + member + "(" + value + ")" : callbackRule["Pattern"]!.GetValue<string>().Replace("{Member}", member, StringComparison.Ordinal),
                    ["memberId"] = row.Key,
                    ["note"] = note
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
                    if (result.Count >= 6 || !used.Contains(key)) { continue; }
                    bool isSwitch = binding["ParameterType"]!.GetValue<string>() == "System.Management.Automation.SwitchParameter";
                    string conversion = binding["Conversion"]!.GetValue<string>();
                    string value = conversion == "BooleanInversion" ? "false" : isSwitch ? "true"
                        : conversion == "SecondsToTimeSpan" ? "TimeSpan.FromSeconds(…)" : "…";
                    string call = owner.Substring(owner.LastIndexOf('.') + 1);
                    call = method == ".ctor" ? "new " + call : call + "." + method;
                    call += "(" + argument + ": " + value + ")";
                    result.Add(new JsonObject {
                        ["powershell"] = command["Command"]!.GetValue<string>() + " -" + binding["Parameter"]!.GetValue<string>() + (isSwitch ? string.Empty : " …"),
                        ["csharp"] = call,
                        ["fluent"] = call,
                        ["memberId"] = key,
                        ["note"] = binding["Reason"]?.GetValue<string>()
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
                if (result.Count >= 6 || !used.Contains(key) && !used.Contains("M:" + fluentOwner + "." + fluentMethod)) { continue; }
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
                if (result.Count >= 6 || !used.Contains(key)) { continue; }
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
                if (result.Count >= 6 || !used.Contains(key)) { continue; }
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
                    if (result.Count >= 6 || !used.Contains(key)) { continue; }
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
                if (result.Count >= 6 || !used.Contains(key)) { continue; }
                string call = owner.Substring(owner.LastIndexOf('.') + 1) + "." + member + "(…)";
                result.Add(new JsonObject {
                    ["powershell"] = "dotnet run", ["csharp"] = call, ["fluent"] = call, ["memberId"] = key,
                    ["note"] = integration["Reason"]!.GetValue<string>()
                });
            }
            if (result.Count == 0) { throw new InvalidDataException("No parity mapping is available for " + initializer + "."); }
            return result;
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
