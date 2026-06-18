using JobScanner.Domain.Enums;
using JobScanner.Domain.Matching;

namespace JobScanner.UnitTests;

public sealed class UserJobMatchStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    private static UserJobMatch NewMatch(MatchState state = MatchState.New) =>
        new()
        {
            ProfileId = 1,
            JobId = 1,
            CreatedAt = Now,
            State = state,
        };

    [Fact]
    public void Save_transitions_New_to_Saved()
    {
        var m = NewMatch();
        m.Save();
        Assert.Equal(MatchState.Saved, m.State);
    }

    [Theory]
    [InlineData(MatchState.Saved)]
    [InlineData(MatchState.Opened)]
    [InlineData(MatchState.Applied)]
    [InlineData(MatchState.Dismissed)]
    [InlineData(MatchState.Expired)]
    public void Save_is_noop_for_non_New_states(MatchState start)
    {
        var m = NewMatch(start);
        m.Save();
        Assert.Equal(start, m.State);
    }

    [Theory]
    [InlineData(MatchState.New)]
    [InlineData(MatchState.Saved)]
    [InlineData(MatchState.Expired)]
    public void Open_transitions_non_closed_states_and_stamps_OpenedAt(MatchState start)
    {
        var m = NewMatch(start);
        m.Open(Now);
        Assert.Equal(MatchState.Opened, m.State);
        Assert.Equal(Now, m.OpenedAt);
    }

    [Theory]
    [InlineData(MatchState.Applied)]
    [InlineData(MatchState.Dismissed)]
    public void Open_does_not_change_closed_states(MatchState start)
    {
        var m = NewMatch(start);
        m.Open(Now);
        Assert.Equal(start, m.State);
        Assert.Null(m.OpenedAt);
    }

    [Fact]
    public void Open_preserves_first_OpenedAt_on_repeat()
    {
        var m = NewMatch();
        var first = Now;
        var second = Now.AddHours(2);

        m.Open(first);
        m.Open(second);

        Assert.Equal(MatchState.Opened, m.State);
        Assert.Equal(first, m.OpenedAt);
    }

    [Theory]
    [InlineData(MatchState.New)]
    [InlineData(MatchState.Saved)]
    [InlineData(MatchState.Opened)]
    [InlineData(MatchState.Dismissed)]
    [InlineData(MatchState.Expired)]
    public void Apply_always_wins_and_stamps_AppliedAt(MatchState start)
    {
        var m = NewMatch(start);
        m.Apply(Now);
        Assert.Equal(MatchState.Applied, m.State);
        Assert.Equal(Now, m.AppliedAt);
        Assert.True(m.IsClosed);
    }

    [Fact]
    public void Apply_preserves_first_AppliedAt_on_repeat()
    {
        var m = NewMatch();
        var first = Now;
        m.Apply(first);
        m.Apply(Now.AddDays(3));
        Assert.Equal(first, m.AppliedAt);
    }

    [Theory]
    [InlineData(MatchState.New)]
    [InlineData(MatchState.Saved)]
    [InlineData(MatchState.Opened)]
    [InlineData(MatchState.Applied)]
    [InlineData(MatchState.Expired)]
    public void Dismiss_always_wins(MatchState start)
    {
        var m = NewMatch(start);
        m.Dismiss();
        Assert.Equal(MatchState.Dismissed, m.State);
        Assert.True(m.IsClosed);
    }

    [Fact]
    public void IsClosed_is_true_only_for_Applied_or_Dismissed()
    {
        Assert.False(NewMatch(MatchState.New).IsClosed);
        Assert.False(NewMatch(MatchState.Saved).IsClosed);
        Assert.False(NewMatch(MatchState.Opened).IsClosed);
        Assert.False(NewMatch(MatchState.Expired).IsClosed);
        Assert.True(NewMatch(MatchState.Applied).IsClosed);
        Assert.True(NewMatch(MatchState.Dismissed).IsClosed);
    }

    [Theory]
    [InlineData(MatchState.New)]
    [InlineData(MatchState.Saved)]
    [InlineData(MatchState.Opened)]
    public void Expire_transitions_open_states_to_Expired(MatchState start)
    {
        var m = NewMatch(start);
        m.Expire();
        Assert.Equal(MatchState.Expired, m.State);
    }

    [Theory]
    [InlineData(MatchState.Applied)]
    [InlineData(MatchState.Dismissed)]
    public void Expire_does_not_overwrite_user_decided_states(MatchState start)
    {
        var m = NewMatch(start);
        m.Expire();
        Assert.Equal(start, m.State);
    }
}
