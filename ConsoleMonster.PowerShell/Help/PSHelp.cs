using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using ConsoleMonster.Help;

namespace ConsoleMonster.PowerShell {
    /// <summary>
    /// Reads PowerShell's help objects (Get-Help -Full: cmdlet help files, comment-based help of functions and scripts,
    /// and the help PowerShell generates when there is none) into a <see cref="HelpTopic"/>. Only the help object's
    /// fields are read; no text is parsed as Markdown.
    /// </summary>
    internal static class PSHelp {
        private static readonly Regex Prompt = new Regex(@"^PS [^>\r\n]*>\s*", RegexOptions.CultureInvariant);
        private static readonly Regex ExampleNumber = new Regex(@"^-*\s*example\s+\d+\s*-*\s*:?\s*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static HelpTopic Read(PSObject help, CommandInfo? command) {
            string name = Text(help, "Name") ?? Text(Child(help, "details"), "name") ?? command?.Name ?? string.Empty;
            HelpTopic topic = new HelpTopic(name) {
                Synopsis = Clean(Text(help, "Synopsis")),
                CommonParameters = command != null && TakesCommonParameters(command)
            };
            foreach (string paragraph in Paragraphs(Child(help, "description"))) {
                topic.Description.Add(paragraph);
            }
            foreach (object item in Items(Child(Child(help, "syntax"), "syntaxItem"))) {
                PSObject entry = PSObject.AsPSObject(item);
                HelpSyntax syntax = new HelpSyntax(Text(entry, "name") ?? name) { CommonParameters = topic.CommonParameters };
                foreach (object p in Items(Child(entry, "parameter"))) {
                    PSObject parameter = PSObject.AsPSObject(p);
                    string? parameterName = Text(parameter, "name");
                    if (string.IsNullOrEmpty(parameterName)) {
                        continue;
                    }
                    syntax.Parameters.Add(new HelpSyntaxParameter(parameterName!) {
                        Type = ValueType(parameter),
                        Required = IsTrue(Text(parameter, "required")),
                        Positional = IsPosition(Text(parameter, "position"))
                    });
                }
                topic.Syntax.Add(syntax);
            }
            foreach (object p in Items(Child(Child(help, "parameters"), "parameter"))) {
                PSObject parameter = PSObject.AsPSObject(p);
                string? parameterName = Text(parameter, "name");
                if (string.IsNullOrEmpty(parameterName)) {
                    continue;
                }
                string? type = Clean(Text(Child(parameter, "type"), "name")) ?? ValueType(parameter);
                bool isSwitch = type != null && (type.EndsWith("SwitchParameter", StringComparison.OrdinalIgnoreCase) || string.Equals(type, "switch", StringComparison.OrdinalIgnoreCase));
                HelpParameter entry = new HelpParameter(parameterName!) {
                    Type = isSwitch ? "switch" : type,
                    Required = IsTrue(Text(parameter, "required")),
                    Position = IsPosition(Text(parameter, "position")) ? Text(parameter, "position") : null,
                    Pipeline = Pipeline(Text(parameter, "pipelineInput")),
                    // A switch is off unless given: "False" says nothing.
                    Default = isSwitch ? null : Value(Text(parameter, "defaultValue")),
                    Description = Clean(string.Join("\n\n", Paragraphs(Child(parameter, "description")).ToArray()))
                };
                string? aliases = Value(Text(parameter, "aliases"));
                if (aliases != null) {
                    foreach (string alias in aliases.Split(',')) {
                        if (alias.Trim().Length > 0) {
                            entry.Aliases.Add(alias.Trim());
                        }
                    }
                }
                topic.Parameters.Add(entry);
            }
            foreach (object e in Items(Child(Child(help, "examples"), "example"))) {
                PSObject example = PSObject.AsPSObject(e);
                string? title = Clean(Text(example, "title"));
                if (title != null) {
                    title = Clean(ExampleNumber.Replace(title.Trim('-', ' '), string.Empty).Trim('-', ' '));
                }
                // Windows PowerShell help files start the code with the prompt ("PS C:\>"); the code is what to type.
                string code = Prompt.Replace((Text(example, "code") ?? string.Empty).Trim(), string.Empty, 1).Trim();
                topic.Examples.Add(new HelpExample(title, code) {
                    Remarks = Clean(string.Join("\n\n", Paragraphs(Child(example, "remarks")).ToArray()))
                });
            }
            Types(Child(Child(help, "inputTypes"), "inputType"), topic.Inputs);
            Types(Child(Child(help, "returnValues"), "returnValue"), topic.Outputs);
            foreach (object alert in Items(Child(Child(help, "alertSet"), "alert"))) {
                foreach (string paragraph in Paragraphs(alert)) {
                    topic.Notes.Add(paragraph);
                }
            }
            foreach (object l in Items(Child(Child(help, "relatedLinks"), "navigationLink"))) {
                PSObject link = PSObject.AsPSObject(l);
                string? text = Clean(Text(link, "linkText"));
                string? uri = Clean(Text(link, "uri"));
                if (text != null || uri != null) {
                    topic.Links.Add(new HelpLink(text, uri));
                }
            }
            // PowerShell 7 without Update-Help has no descriptions or examples, only what it can read from the command.
            bool described = topic.Description.Count > 0 || topic.Examples.Count > 0;
            foreach (HelpParameter parameter in topic.Parameters) {
                described |= !string.IsNullOrEmpty(parameter.Description);
            }
            string synopsis = topic.Synopsis ?? string.Empty;
            if (!described && (synopsis.Length == 0 || synopsis.StartsWith(topic.Name + " ", StringComparison.OrdinalIgnoreCase)) && command is CmdletInfo) {
                topic.Notice = "Partial help: run Update-Help for descriptions and examples.";
            }
            return topic;
        }

        private static bool TakesCommonParameters(CommandInfo command) {
            CommandInfo resolved = command is AliasInfo alias && alias.ResolvedCommand != null ? alias.ResolvedCommand : command;
            try {
                return resolved.Parameters != null && resolved.Parameters.ContainsKey("Verbose");
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                // A command whose metadata cannot be read (a script with parse errors): no common parameters shown.
                return false;
            }
        }

        private static void Types(object? items, IList<string> into) {
            foreach (object t in Items(items)) {
                PSObject entry = PSObject.AsPSObject(t);
                string? type = Clean(Text(Child(entry, "type"), "name"));
                if (type == null) {
                    continue;
                }
                // Several types can share one entry, one per line.
                string note = string.Join(" ", Paragraphs(Child(entry, "description")).ToArray());
                foreach (string line in type.Replace("\r\n", "\n").Split('\n')) {
                    if (line.Trim().Length > 0) {
                        into.Add(line.Trim() + (note.Length > 0 ? "\t" + note : string.Empty));
                    }
                }
            }
        }

        // The value type of a syntax or parameter entry: parameterValue as text (MAML keeps it in #text), null for a switch.
        private static string? ValueType(PSObject parameter) {
            object? value = Member(parameter, "parameterValue");
            string? text = value is PSObject wrapped && wrapped.BaseObject is PSCustomObject ? Text(wrapped, "#text") : Convert.ToString(value is PSObject p ? p.BaseObject : value, CultureInfo.InvariantCulture);
            text = Clean(text);
            if (text == null || text.EndsWith("SwitchParameter", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }
            return text;
        }

        private static string? Pipeline(string? value) {
            if (value == null || !IsTrue(value)) {
                return null;
            }
            int open = value.IndexOf('(');
            int close = value.LastIndexOf(')');
            return open >= 0 && close > open ? value.Substring(open + 1, close - open - 1).Trim() : "yes";
        }

        private static bool IsTrue(string? value) => value != null && value.TrimStart().StartsWith("true", StringComparison.OrdinalIgnoreCase);

        private static bool IsPosition(string? value) => value != null && value.Trim().Length > 0 && char.IsDigit(value.Trim()[0]);

        // "None", "" and similar placeholders mean no value.
        private static string? Value(string? value) {
            string? clean = Clean(value);
            return clean == null || string.Equals(clean, "None", StringComparison.OrdinalIgnoreCase) ? null : clean;
        }

        private static string? Clean(string? value) {
            if (value == null) {
                return null;
            }
            string trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        // The Text of each paragraph object (MAML description arrays), or a plain string.
        private static List<string> Paragraphs(object? value) {
            List<string> result = new List<string>();
            foreach (object item in Items(value)) {
                object? raw = item is PSObject ps ? ps.BaseObject : item;
                string? text = raw is string s ? s : Text(PSObject.AsPSObject(item), "Text");
                text = Clean(text);
                if (text != null) {
                    result.Add(text.Replace("\r\n", "\n"));
                }
            }
            return result;
        }

        private static IEnumerable<object> Items(object? value) {
            object? raw = value is PSObject ps ? ps.BaseObject : value;
            if (raw == null) {
                yield break;
            }
            if (raw is IEnumerable enumerable && !(raw is string)) {
                foreach (object? item in enumerable) {
                    if (item != null) {
                        yield return item;
                    }
                }
                yield break;
            }
            yield return value!;
        }

        private static object? Child(object? value, string name) => value == null ? null : Member(PSObject.AsPSObject(value), name);

        private static object? Member(PSObject? value, string name) {
            if (value == null) {
                return null;
            }
            try {
                return value.Properties[name]?.Value;
            } catch (Exception ex) when (!(ex is PipelineStoppedException)) {
                return null;
            }
        }

        private static string? Text(object? value, string name) {
            object? member = value == null ? null : Member(PSObject.AsPSObject(value), name);
            if (member == null) {
                return null;
            }
            object raw = member is PSObject ps ? ps.BaseObject : member;
            if (raw is string text) {
                return text;
            }
            if (raw is IEnumerable enumerable) {
                StringBuilder sb = new StringBuilder();
                foreach (object? item in enumerable) {
                    string? part = item is string s ? s : Text(item, "Text") ?? Convert.ToString(item, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(part)) {
                        sb.Append(sb.Length > 0 ? "\n" : string.Empty).Append(part);
                    }
                }
                return sb.ToString();
            }
            return Convert.ToString(raw, CultureInfo.InvariantCulture);
        }
    }
}
