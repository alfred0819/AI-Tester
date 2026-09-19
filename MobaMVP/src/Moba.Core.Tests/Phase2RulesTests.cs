using Moba.Core;
using Xunit;

namespace Moba.Core.Tests;

public class Phase2RulesTests
{
    [Fact]
    public void CrystalDestroyed_OppositeTeamWins()
    {
        Assert.Equal(TeamId.Red, MatchWinRules.WinnerWhenCrystalDestroyed(TeamId.Blue));
        Assert.Equal(TeamId.Blue, MatchWinRules.WinnerWhenCrystalDestroyed(TeamId.Red));
        Assert.Equal(TeamId.None, MatchWinRules.WinnerWhenCrystalDestroyed(TeamId.None));
    }

    [Fact]
    public void Wallet_AddAndSpend()
    {
        var wallet = new PlayerWallet(0);
        wallet.Add(EconomyRules.GoldPerMinion);
        Assert.Equal(EconomyRules.GoldPerMinion, wallet.Gold);
        Assert.True(wallet.TrySpend(10));
        Assert.Equal(EconomyRules.GoldPerMinion - 10, wallet.Gold);
        Assert.False(wallet.TrySpend(9999));
    }

    [Fact]
    public void Bounty_MatchesKind()
    {
        Assert.Equal(EconomyRules.GoldPerMinion, EconomyRules.BountyFor(CombatTargetKind.Minion));
        Assert.Equal(EconomyRules.GoldPerTower, EconomyRules.BountyFor(CombatTargetKind.Tower));
        Assert.Equal(EconomyRules.GoldPerHeroKill, EconomyRules.BountyFor(CombatTargetKind.Hero));
        Assert.Equal(0, EconomyRules.BountyFor(CombatTargetKind.Crystal));
    }

    [Fact]
    public void WaveRules_PositiveIntervals()
    {
        Assert.True(LaneWaveRules.WaveIntervalSeconds > 0f);
        Assert.True(LaneWaveRules.MeleePerWave >= 1);
        Assert.True(BuildingRules.TowerMaxHealth > BuildingRules.CrystalMaxHealth * 0.1f);
    }
}
