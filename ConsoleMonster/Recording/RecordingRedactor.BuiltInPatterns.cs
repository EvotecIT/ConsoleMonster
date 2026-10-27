using System;
using System.Text.RegularExpressions;

namespace ConsoleMonster.Sessions {
    internal sealed partial class RecordingRedactor {
        private const int PatternTimeoutMilliseconds = 100;
        private const string BearerPattern = @"(?i)\bbearer\s+[^\s;""'\x00-\x1f]+";
        private const string CredentialPattern = @"(?i)\b(?:password|pwd|api[_-]?key|access[_-]?token|client[_-]?secret|token)\s*[=:]\s*(?:""[^""\x00-\x1f]*""?|'[^'\x00-\x1f]*'?|[^;""'\x00-\x1f]+)";
        private const string TokenPattern = @"\b(?:gh[pousr]_[A-Za-z0-9_]+|github_pat_[A-Za-z0-9_]+|sk-[A-Za-z0-9_-]{8,}|AKIA[A-Z0-9]{16})\b";
        private const string PendingKeySuffix = @"(?:\s*(?:[=:]\s*(?:""[^""\x00-\x1f]*|'[^'\x00-\x1f]*|[^;""'\x00-\x1f]*))?)?\z";
        private const string PendingBearerPattern = @"(?i)\bbearer(?:\s+[^\s;""'\x00-\x1f]*)?\z";
        private const string PendingPasswordPattern = @"(?i)\bpassword" + PendingKeySuffix;
        private const string PendingPwdPattern = @"(?i)\bpwd" + PendingKeySuffix;
        private const string PendingApiKeyPattern = @"(?i)\bapi[_-]?key" + PendingKeySuffix;
        private const string PendingAccessTokenPattern = @"(?i)\baccess[_-]?token" + PendingKeySuffix;
        private const string PendingClientSecretPattern = @"(?i)\bclient[_-]?secret" + PendingKeySuffix;
        private const string PendingTokenPattern = @"(?i)\btoken" + PendingKeySuffix;
        private const string PendingGitHubShortPattern = @"(?i)\bgh[pousr]_[A-Za-z0-9_]*\z";
        private const string PendingGitHubPatPattern = @"(?i)\bgithub_pat_[A-Za-z0-9_]*\z";
        private const string PendingSkPattern = @"(?i)\bsk-[A-Za-z0-9_-]*\z";

        private static readonly Regex[] BuiltInPatterns = { BearerRegex(), CredentialRegex(), TokenRegex() };
        // A fixed keyword prefix lets each engine skip safe text. Keep one deadline across all candidate
        // positions for a rule; restarting Match for every candidate would allow quadratic scans to run unbounded.
        private static readonly Regex[] PendingCredentialPatterns = {
            PendingBearerRegex(),
            PendingPasswordRegex(),
            PendingPwdRegex(),
            PendingApiKeyRegex(),
            PendingAccessTokenRegex(),
            PendingClientSecretRegex(),
            PendingTokenRegex(),
            PendingGitHubShortRegex(),
            PendingGitHubPatRegex(),
            PendingSkRegex(),
        };

#if NET8_0_OR_GREATER
        [GeneratedRegex(BearerPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex BearerRegex();

        // The optional closing quote has the same range as the old quote-or-control/end lookahead:
        // the preceding character class can stop only at a quote, a control, or the end of the record.
        private static Regex CredentialRegex() => new Regex(CredentialPattern,
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(PatternTimeoutMilliseconds));

        [GeneratedRegex(TokenPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex TokenRegex();

        [GeneratedRegex(PendingBearerPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingBearerRegex();

        [GeneratedRegex(PendingPasswordPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingPasswordRegex();

        [GeneratedRegex(PendingPwdPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingPwdRegex();

        [GeneratedRegex(PendingApiKeyPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingApiKeyRegex();

        [GeneratedRegex(PendingAccessTokenPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingAccessTokenRegex();

        [GeneratedRegex(PendingClientSecretPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingClientSecretRegex();

        [GeneratedRegex(PendingTokenPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingTokenRegex();

        [GeneratedRegex(PendingGitHubShortPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingGitHubShortRegex();

        [GeneratedRegex(PendingGitHubPatPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingGitHubPatRegex();

        [GeneratedRegex(PendingSkPattern, RegexOptions.CultureInvariant, PatternTimeoutMilliseconds)]
        private static partial Regex PendingSkRegex();

#else
        private static Regex BearerRegex() => CreateBuiltInRegex(BearerPattern);
        private static Regex CredentialRegex() => CreateBuiltInRegex(CredentialPattern);
        private static Regex TokenRegex() => CreateBuiltInRegex(TokenPattern);
        private static Regex PendingBearerRegex() => CreateBuiltInRegex(PendingBearerPattern);
        private static Regex PendingPasswordRegex() => CreateBuiltInRegex(PendingPasswordPattern);
        private static Regex PendingPwdRegex() => CreateBuiltInRegex(PendingPwdPattern);
        private static Regex PendingApiKeyRegex() => CreateBuiltInRegex(PendingApiKeyPattern);
        private static Regex PendingAccessTokenRegex() => CreateBuiltInRegex(PendingAccessTokenPattern);
        private static Regex PendingClientSecretRegex() => CreateBuiltInRegex(PendingClientSecretPattern);
        private static Regex PendingTokenRegex() => CreateBuiltInRegex(PendingTokenPattern);
        private static Regex PendingGitHubShortRegex() => CreateBuiltInRegex(PendingGitHubShortPattern);
        private static Regex PendingGitHubPatRegex() => CreateBuiltInRegex(PendingGitHubPatPattern);
        private static Regex PendingSkRegex() => CreateBuiltInRegex(PendingSkPattern);

        private static Regex CreateBuiltInRegex(string pattern) => new Regex(pattern,
            RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromMilliseconds(PatternTimeoutMilliseconds));
#endif
    }
}
