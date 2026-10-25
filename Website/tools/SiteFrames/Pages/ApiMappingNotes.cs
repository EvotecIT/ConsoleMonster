using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using SiteFrames.Capture;

namespace SiteFrames.Pages {
    /// <summary>Generates the three-style translation aid from resolved program symbols and the parity contract.</summary>
    internal static class ApiMappingNotes {
        private static readonly string[] Preferred = { "Title", "Border", "Content", "Label", "Value", "Total", "WidgetId", "HeaderRight", "Caption", "Role" };

        public static JsonArray For(Site site, string initializer, string fluent, params string[] companions) =>
            FromSource(site, initializer, Site.ReadText(fluent), companions);

        public static JsonArray FromSource(Site site, string initializer, string fluentSource, params string[] companions) {
            JsonObject map = JsonNode.Parse(File.ReadAllText(Path.Combine(site.Repository, "Tests", "Api", "round2-parity.json")))!.AsObject();
            var used = new HashSet<string>(CSharpRunner.ConfiguredMembers(initializer, companions), StringComparer.Ordinal);
            used.UnionWith(CSharpRunner.ConfiguredMembersFromSource(initializer, fluentSource, companions));
            var rows = new Dictionary<string, (string Command, JsonObject Binding)>(StringComparer.Ordinal);
            foreach (JsonNode? commandNode in map["Commands"]!.AsArray()) {
                JsonObject command = commandNode!.AsObject();
                string name = command["Command"]!.GetValue<string>();
                foreach (JsonNode? bindingNode in command["Bindings"]!.AsArray()) {
                    JsonObject binding = bindingNode!.AsObject();
                    if (binding["MemberKind"]!.GetValue<string>() != "Property") { continue; }
                    // Delegate properties shadow property-named extension calls; their grammar is a separate decision.
                    string valueType = binding["CSharpTypeName"]?.GetValue<string>() ?? string.Empty;
                    if (valueType.StartsWith("System.Action", StringComparison.Ordinal) || valueType.StartsWith("System.Func", StringComparison.Ordinal)) { continue; }
                    string member = binding["Member"]!.GetValue<string>();
                    string key = binding["CSharpType"]!.GetValue<string>() + "." + member;
                    if (!used.Contains(key)) { continue; }
                    if (!rows.TryGetValue(key, out var existing) || name.StartsWith("New-", StringComparison.Ordinal) && !existing.Command.StartsWith("New-", StringComparison.Ordinal)) {
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
                binding["Member"] = route["Member"]!.GetValue<string>();
                binding["CSharpTypeName"] = route["CSharpTypeName"]!.GetValue<string>();
                binding["Reason"] = route["Reason"]!.GetValue<string>();
                if (route["RecordField"] != null) { binding["RecordField"] = route["RecordField"]!.GetValue<string>(); }
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
                result.Add(new JsonObject {
                    ["powershell"] = recordField == null ? row.Value.Command + " -" + parameter + (isSwitch ? string.Empty : " …")
                        : "@{ " + recordField + " = … } | " + row.Value.Command,
                    ["csharp"] = member + " = " + value,
                    ["fluent"] = "." + member + "(" + value + ")",
                    ["memberId"] = row.Key,
                    ["note"] = conversion == "Identity" ? null : binding["Reason"]?.GetValue<string>() ?? conversion
                });
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
            if (result.Count == 0) { throw new InvalidDataException("No parity mapping is available for " + initializer + "."); }
            return result;
        }

        private static int Rank(string member) {
            int index = Array.IndexOf(Preferred, member);
            return index < 0 ? Preferred.Length : index;
        }
    }
}
