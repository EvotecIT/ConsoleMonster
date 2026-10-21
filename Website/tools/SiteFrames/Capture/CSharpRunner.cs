using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ConsoleMonster;
using ConsoleMonster.Canvas;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SiteFrames.Capture {
    /// <summary>
    /// Runs the C# programs the site shows. Each is a complete <c>Program.cs</c> (its <c>using</c> lines and top-level
    /// statements) under <c>Website/examples/</c>, compiled here exactly as written against this repository's ConsoleMonster
    /// and run inside a <see cref="RecordingTarget"/> of a fixed size, colour depth, glyph tier and theme. What it wrote is
    /// replayed into a screen, so the frame on the page is what that program printed.
    /// <para>
    /// Only the driver lives here: the terminal it writes to and, for prompts, the scripted keys (through
    /// <see cref="InteractiveOptions.Default"/>). Neither is ever part of the shown code. A program that does not compile, has
    /// a warning or an unused <c>using</c>, fails the render.
    /// </para>
    /// </summary>
    internal static class CSharpRunner {
        private static readonly Dictionary<string, Assembly> Compiled = new Dictionary<string, Assembly>(StringComparer.Ordinal);

        // The global usings a `dotnet new console` project gets from ImplicitUsings, so a shown program compiles here exactly
        // as it would in the reader's project.
        private const string ImplicitUsings = "global using global::System;\nglobal using global::System.Collections.Generic;\nglobal using global::System.IO;\nglobal using global::System.Linq;\nglobal using global::System.Net.Http;\nglobal using global::System.Threading;\nglobal using global::System.Threading.Tasks;\n";
        private static List<MetadataReference>? _references;

        /// <summary>
        /// The assemblies a shown program may use: the shared .NET framework and ConsoleMonster, the same as a reader's project
        /// with the package. The tool's own dependencies (ConsoleMonster.Examples, Roslyn, SiteFrames) are left out, so a
        /// program can never lean on something a reader does not have.
        /// </summary>
        private static List<MetadataReference> References {
            get {
                if (_references == null) {
                    _references = new List<MetadataReference>();
                    string framework = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                    string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
                    foreach (string path in tpa.Split(Path.PathSeparator)) {
                        if (string.Equals(Path.GetDirectoryName(path), framework, StringComparison.OrdinalIgnoreCase)) {
                            _references.Add(MetadataReference.CreateFromFile(path));
                        }
                    }
                    _references.Add(MetadataReference.CreateFromFile(typeof(Terminal).Assembly.Location));
                }
                return _references;
            }
        }

        /// <summary>Compiles a program once per render; throws with the compiler's messages when it is not clean.</summary>
        public static Assembly Compile(string path) {
            string code = Site.ReadText(path);
            if (Compiled.TryGetValue(path + "\n" + code, out Assembly? cached)) {
                return cached;
            }
            // C# 12 with the .NET 8 SDK's warning level: what a reader's `dotnet new console` project on .NET 8 compiles.
            CSharpParseOptions parse = new CSharpParseOptions(LanguageVersion.CSharp12);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code, parse, path);
            SyntaxTree implicitUsings = CSharpSyntaxTree.ParseText(ImplicitUsings, parse, "GlobalUsings.g.cs");
            CSharpCompilation compilation = CSharpCompilation.Create(
                "SiteExample_" + Path.GetFileNameWithoutExtension(path).Replace('-', '_'),
                new[] { tree, implicitUsings },
                References,
                new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable, optimizationLevel: OptimizationLevel.Release, warningLevel: 8));
            using (MemoryStream stream = new MemoryStream()) {
                Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
                // An unused using (CS8019) is reported by the compilation, not the emit; a reader should not copy dead lines.
                List<Diagnostic> problems = result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning)
                    .Concat(compilation.GetDiagnostics().Where(d => d.Id == "CS8019" && d.Location.SourceTree == tree))
                    .ToList();
                if (!result.Success || problems.Count > 0) {
                    throw new InvalidOperationException(Path.GetFileName(path) + " is not a clean program:\n" + string.Join("\n", problems.Select(Describe)));
                }
                Assembly assembly = Assembly.Load(stream.ToArray());
                Compiled[path + "\n" + code] = assembly;
                return assembly;
            }
        }

        private static string Describe(Diagnostic d) {
            int line = d.Location.GetLineSpan().StartLinePosition.Line + 1;
            return "  line " + line.ToString(CultureInfo.InvariantCulture) + ": " + d.Id + " " + d.GetMessage(CultureInfo.InvariantCulture);
        }

        /// <summary>Runs a program on a fixed terminal and returns the rows it printed.</summary>
        public static CellBuffer Run(string path, int width, ColorDepth depth, GlyphTier glyphs, ITheme theme, int height = 80) {
            RecordingTarget target = Execute(path, Profile(width, height, depth, glyphs), theme, null);
            return Parity.Transcript(System.Text.Encoding.UTF8.GetBytes(target.Output), width, height);
        }

        public static TerminalProfile Profile(int width, int height, ColorDepth depth, GlyphTier glyphs) =>
            TerminalProfile.Fixed(width, height, depth, glyphs).WithCulture(Site.Culture).WithLanguage(Site.Culture);

        /// <summary>
        /// Runs a program against a recording target and returns it. <paramref name="interactive"/> drives prompts with scripted
        /// keys; without it, prompts are non-interactive, so a program that asks fails instead of waiting for the build's
        /// console. The program must end on its own: running out of keys while a prompt waits is a failure.
        /// </summary>
        public static RecordingTarget Execute(string path, TerminalProfile profile, ITheme theme, InteractiveOptions? interactive) {
            Assembly assembly = Compile(path);
            MethodInfo entry = assembly.EntryPoint ?? throw new InvalidOperationException(path + " has no entry point.");
            RecordingTarget target = new RecordingTarget(profile) { Theme = theme };
            InteractiveOptions previous = InteractiveOptions.Default;
            // The program writes through ConsoleMonster only; anything it sends to the real console (Console.Write) would be
            // missing from the frame, and it must never read the build's input.
            TextWriter stdout = Console.Out;
            TextWriter stderr = Console.Error;
            TextReader stdin = Console.In;
            StringWriter stray = new StringWriter(CultureInfo.InvariantCulture);
            try {
                InteractiveOptions.Default = interactive ?? new InteractiveOptions { NonInteractive = true };
                Console.SetOut(stray);
                Console.SetError(stray);
                Console.SetIn(TextReader.Null);
                using (Terminal.Use(target)) {
                    // A program with top-level await still has a synchronous entry point that waits for it.
                    entry.Invoke(null, entry.GetParameters().Length == 0 ? null : new object[] { Array.Empty<string>() });
                }
            } catch (TargetInvocationException ex) when (ex.InnerException != null) {
                throw new InvalidOperationException(Path.GetFileName(path) + " failed: " + ex.InnerException.Message, ex.InnerException);
            } finally {
                Console.SetOut(stdout);
                Console.SetError(stderr);
                Console.SetIn(stdin);
                InteractiveOptions.Default = previous;
            }
            if (stray.GetStringBuilder().Length > 0) {
                throw new InvalidOperationException(Path.GetFileName(path) + " wrote to the console directly instead of through ConsoleMonster: " + stray);
            }
            return target;
        }
    }
}
