using System;
using System.IO;

namespace ConsoleMonster {
    public static partial class ContinuousIntegration {
        // Provider paths name checkout files; terminal labels name files relative to the caller's current directory.
        private static string? AnnotationPath(string? file, ContinuousIntegrationProvider provider, string? directory, string? resolvedFile) {
            if (string.IsNullOrEmpty(file)) { return file; }
            string given = file!.Replace('\\', '/');
            string? root = provider == ContinuousIntegrationProvider.GitHub ? Environment.GetEnvironmentVariable("GITHUB_WORKSPACE")
                : provider == ContinuousIntegrationProvider.Azure ? Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY")
                : provider == ContinuousIntegrationProvider.GitLab ? Environment.GetEnvironmentVariable("CI_PROJECT_DIR")
                : directory ?? Environment.CurrentDirectory;
            if (string.IsNullOrWhiteSpace(root) || (resolvedFile == null && Text.DisplayPath.IsForeign(file))) { return given; }
            try {
                string full = (resolvedFile ?? FullAnnotationPath(file, directory)).Replace('\\', '/');
                string basis = FullAnnotationPath(root!, directory).Replace('\\', '/').TrimEnd('/') + "/";
                StringComparison comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (full.StartsWith(basis, comparison)) { return full.Substring(basis.Length); }
                if (provider == ContinuousIntegrationProvider.None) {
                    Uri relative = new Uri(basis).MakeRelativeUri(new Uri(full));
                    if (!relative.IsAbsoluteUri) { return Uri.UnescapeDataString(relative.ToString()); }
                }
            } catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is UriFormatException) {
                // Keep an unresolvable or foreign path as data; the protocol still escapes it.
            }
            return given;
        }

        private static string FullAnnotationPath(string file, string? directory) {
            try {
                return Text.DisplayPath.IsForeign(file) ? file : Path.GetFullPath(Path.Combine(directory ?? Environment.CurrentDirectory, file.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
            } catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) {
                return file;
            }
        }
    }
}
