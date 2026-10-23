using System;
using System.Reflection;
using System.Threading.Tasks;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Canvas;
using ConsoleMonster.Layout;
using ConsoleMonster.Output;
using ConsoleMonster.Styling;
using Xunit;

namespace ConsoleMonster.Tests.Output {
    [Collection("Live sessions")]
    public sealed class ProfilePolicyThemeTests {
        private sealed class Probe : IWidget {
            public ITheme? Theme { get; private set; }
            public Size Measure(Constraints constraints, RenderContext context) => new Size(1, 1);
            public void Render(Region region, RenderContext context) { Theme = context.Theme; }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ProfilePolicy_DefaultTargetThemeFollowsTheAdjustedProfile(bool explicitTheme) {
            RecordingTarget target = new RecordingTarget(TerminalProfile.Fixed(30, 10, ColorDepth.None, GlyphTier.Full)
                .WithThemePreference(ThemePreference.Dark)) { Theme = explicitTheme ? Theme.Dark : null };
            using (Terminal.Use(target)) {
                using (Terminal.UseProfilePolicy(p => p.WithThemePreference(ThemePreference.Light))) {
                    Probe probe = new Probe();
                    Terminal.Write(probe);
                    Assert.Same(explicitTheme ? Theme.Dark : Theme.Light, probe.Theme);
                }
                Assert.Same(Theme.Dark, Terminal.Theme);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ProfilePolicy_ConsoleThemeDoesNotLeakAcrossNestedScopes(bool readBeforePolicy) {
            // The public console theme setter intentionally has no null/reset operation. Restore its pre-test state.
            FieldInfo themeSlot = typeof(Terminal).GetField("_theme", BindingFlags.Static | BindingFlags.NonPublic)!;
            object? saved = themeSlot.GetValue(null);
            try {
                themeSlot.SetValue(null, null);
                Terminal.Profile = TerminalProfile.Fixed(30, 10, ColorDepth.None, GlyphTier.Full, OutputMode.Stream)
                    .WithThemePreference(ThemePreference.Auto);
                using (Terminal.Use(ConsoleTarget.Instance)) {
                    if (readBeforePolicy) Assert.Same(Theme.Dark, Terminal.Theme);
                    using (Terminal.UseProfilePolicy(p => p.WithThemePreference(ThemePreference.Light))) {
                        Probe probe = new Probe();
                        Terminal.Write(probe, null, new StringTerminalWriter());
                        Assert.Same(Theme.Light, probe.Theme);
                        using (Terminal.UseProfilePolicy(p => p.WithThemePreference(ThemePreference.Dark))) {
                            Assert.Same(Theme.Dark, Terminal.Theme);
                        }
                        Assert.Same(Theme.Light, Terminal.Theme);
                    }
                    Assert.Same(Theme.Dark, Terminal.Theme);
                    Terminal.Theme = Theme.Dark;
                    using (Terminal.UseProfilePolicy(p => p.WithThemePreference(ThemePreference.Light))) {
                        Assert.Same(Theme.Dark, Terminal.Theme);
                    }
                }
            } finally {
                themeSlot.SetValue(null, saved);
                Terminal.Refresh();
            }
        }

        [Fact]
        public async Task ProfilePolicy_TargetThemesRemainLocalToConcurrentFlows() {
            await Task.WhenAll(Check(ThemePreference.Light, Theme.Light), Check(ThemePreference.Dark, Theme.Dark));
        }

        private static async Task Check(ThemePreference preference, ITheme expected) {
            RecordingTarget target = new RecordingTarget(TerminalProfile.Fixed(30, 10, ColorDepth.None, GlyphTier.Full)) { Theme = null };
            using (Terminal.Use(target))
            using (Terminal.UseProfilePolicy(p => p.WithThemePreference(preference))) {
                await Task.Yield();
                Probe probe = new Probe();
                Terminal.Write(probe);
                Assert.Same(expected, probe.Theme);
            }
        }
    }
}
