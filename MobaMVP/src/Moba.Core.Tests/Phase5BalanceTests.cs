using Moba.Core;
using Xunit;

namespace Moba.Core.Tests;

public class Phase5BalanceTests
{
    [Fact]
    public void UncontestedTowerBurn_WithinReasonableWindow()
    {
        // ~hero DPS 55/0.75≈73, 3 minions * 22/0.95≈69 → ~142 raw DPS
        var seconds = BalanceRules.EstimateTowerBurnSeconds(
            BuildingRules.TowerMaxHealth,
            BuildingRules.TowerArmor,
            heroDps: 73f,
            minionDps: 23f,
            minionCount: 3);
        Assert.True(BalanceRules.IsBurnWithinPushWindow(seconds), $"burn={seconds:F0}s");
        Assert.InRange(seconds, 15f, 900f);
    }

    [Fact]
    public void CrystalHarderThanTower()
    {
        Assert.True(BuildingRules.CrystalMaxHealth > BuildingRules.TowerMaxHealth);
    }

    [Fact]
    public void WaveCadence_SupportsMidGamePressure()
    {
        Assert.InRange(LaneWaveRules.WaveIntervalSeconds, 15f, 30f);
        Assert.True(LaneWaveRules.MinionAttack >= 18f);
    }
}
