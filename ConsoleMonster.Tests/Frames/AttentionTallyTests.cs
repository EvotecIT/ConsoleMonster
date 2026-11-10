using System;
using System.Globalization;
using System.Linq;
using ConsoleMonster.Frames;
using ConsoleMonster.Testing;
using ConsoleMonster.Views;
using Xunit;

namespace ConsoleMonster.Tests.Frames;

public sealed class AttentionTallyTests {
    private static string Render(DashboardFrame frame, int width = 100, ConsoleViewLength length = ConsoleViewLength.Standard) {
        frame.Scale = FrameScale.Of(width, 60, length);
        return TestTerminal.RenderText(frame, width, 0, global::ConsoleMonster.Capabilities.GlyphTier.Full, CultureInfo.InvariantCulture);
    }

    [Fact]
    public void ManyFindingsWithOneReason_ShowTheTotalAFewNamesAndHowManyMore() {
        AttentionTally tally = new AttentionTally();
        for (int index = 1; index <= 10_000; index++) tally.Add(HealthState.Warning, "never logged on", "user" + index);

        string text = Render(new DashboardFrame("Users").AttentionOf("account").Attention(tally));

        Assert.Contains("10,000 accounts", text);
        Assert.Contains("user1, user2, user3, user4, user5, +9,995 more", text);
        Assert.DoesNotContain("user6", text);
        Assert.DoesNotContain("set $ConsoleViewLengthPreference", text);
    }

    [Fact]
    public void ReasonsWithFewFindings_StayIndividualItemsAndCountTowardsTheTotal() {
        AttentionTally tally = new AttentionTally();
        tally.Add(HealthState.Failing, "password not required", "svc-legacy");
        for (int index = 1; index <= 7; index++) tally.Add(HealthState.Warning, "idle", "idle" + index);
        tally.Add(HealthState.Healthy, "fine", "ok");

        string text = Render(new DashboardFrame("Users").AttentionOf("account").Attention(tally));

        Assert.Contains("8 accounts", text);
        Assert.True(text.IndexOf("svc-legacy", StringComparison.Ordinal) < text.IndexOf("7 accounts", StringComparison.Ordinal));
        Assert.DoesNotContain("fine", text);
        Assert.Equal(8, tally.Count);
    }

    [Fact]
    public void Group_CountBelowItsNames_IsRejected() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AttentionGroup(HealthState.Warning, "reason", 1, new[] { "a", "b" }));
    }
}
