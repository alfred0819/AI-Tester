using System.Collections.Generic;
using Moba.Core;
using Xunit;

namespace Moba.Core.Tests;

public class Phase6SettleTests
{
    [Fact]
    public void Scoreboard_FormatsKdaGoldAndDuration()
    {
        var entries = new List<ScoreboardEntry>
        {
            new("磐卫", TeamId.Blue, 2, 1, 3, 900, false),
            new("影袭", TeamId.Red, 1, 2, 0, 700, true)
        };
        var text = MatchScoreboard.FormatResult("蓝方胜利！", 125f, entries);
        Assert.Contains("02:05", text);
        Assert.Contains("2/1/3", text);
        Assert.Contains("金900", text);
        Assert.Contains("[Bot]", text);
    }

    [Fact]
    public void PlayerCombatRecord_TracksKda()
    {
        var r = new PlayerCombatRecord();
        r.RegisterKill();
        r.RegisterKill();
        r.RegisterDeath();
        r.RegisterAssist();
        Assert.Equal("2/1/1", r.FormatKda());
    }

    [Fact]
    public void AssistTracker_CreditsWithinWindowExcludingKiller()
    {
        var tracker = new AssistTracker();
        tracker.RecordHit(10, 1f);
        tracker.RecordHit(20, 2f);
        tracker.RecordHit(30, 20f); // stale
        var assists = tracker.CollectAssists(killerId: 10, nowSeconds: 8f, window: 10f);
        Assert.Contains(20UL, assists);
        Assert.DoesNotContain(10UL, assists);
        Assert.DoesNotContain(30UL, assists);
    }
}

public class Phase6RecallTests
{
    [Fact]
    public void Recall_CompletesAfterChannel()
    {
        var state = new HeroCombatState();
        state.ApplyHero(HeroId.PanWei);
        Assert.True(state.TryStartRecall());
        Assert.True(state.IsRecalling);
        Assert.False(state.CanAct);

        var done = false;
        var t = 0f;
        while (t < RecallRules.ChannelSeconds + 0.5f)
        {
            t += 0.5f;
            if (state.TickRecall(0.5f))
            {
                done = true;
                break;
            }
        }

        Assert.True(done);
        Assert.False(state.IsRecalling);
    }

    [Fact]
    public void Recall_CancelOnDamagePath()
    {
        var channel = new RecallChannelState();
        Assert.True(channel.TryStart(4f));
        channel.Cancel();
        Assert.False(channel.IsChanneling);
        Assert.False(channel.Tick(1f));
    }

    [Fact]
    public void Recall_BlocksActionsUntilFinishedOrCancelled()
    {
        var state = new HeroCombatState();
        state.ApplyHero(HeroId.YingXi);
        Assert.True(state.TryStartRecall());
        Assert.False(state.TryStartBasicAttack());
        Assert.False(state.TryStartSkill(SkillSlot.One));
        state.CancelRecall();
        Assert.True(state.TryStartBasicAttack());
    }
}

public class Phase6DisconnectTests
{
    [Fact]
    public void OneVsOne_EndsMatchWhenPeerDrops()
    {
        var r = DisconnectPolicy.Resolve(GameModeKind.OneVsOne, matchInProgress: true, disconnectedIsHost: false);
        Assert.Equal(DisconnectResolution.EndMatchRemainingWins, r);
        Assert.Equal(TeamId.Blue, DisconnectPolicy.WinnerWhenOpponentLeft(TeamId.Blue));
    }

    [Fact]
    public void ThreeVsThree_SpawnsReplacementBot()
    {
        var r = DisconnectPolicy.Resolve(GameModeKind.ThreeVsThree, matchInProgress: true, disconnectedIsHost: false);
        Assert.Equal(DisconnectResolution.SpawnReplacementBot, r);
    }

    [Fact]
    public void HostDropOrLobby_NoResolution()
    {
        Assert.Equal(
            DisconnectResolution.None,
            DisconnectPolicy.Resolve(GameModeKind.OneVsOne, matchInProgress: true, disconnectedIsHost: true));
        Assert.Equal(
            DisconnectResolution.None,
            DisconnectPolicy.Resolve(GameModeKind.OneVsOne, matchInProgress: false, disconnectedIsHost: false));
    }
}
