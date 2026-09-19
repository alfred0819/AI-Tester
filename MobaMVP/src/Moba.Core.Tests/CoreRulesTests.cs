using Moba.Core;
using Xunit;

namespace Moba.Core.Tests;

public class CoreRulesTests
{
    [Theory]
    [InlineData(GameModeKind.OneVsOne, 2, 1)]
    [InlineData(GameModeKind.ThreeVsThree, 6, 3)]
    public void GameMode_PlayerCounts(GameModeKind mode, int max, int perTeam)
    {
        Assert.Equal(max, GameModeRules.MaxPlayers(mode));
        Assert.Equal(perTeam, GameModeRules.PlayersPerTeam(mode));
    }

    [Fact]
    public void TeamAssign_1v1_SplitsBlueRed()
    {
        Assert.Equal(TeamId.Blue, TeamRules.AssignTeam(0, GameModeKind.OneVsOne));
        Assert.Equal(TeamId.Red, TeamRules.AssignTeam(1, GameModeKind.OneVsOne));
    }

    [Fact]
    public void TeamAssign_3v3_FirstThreeBlue()
    {
        Assert.Equal(TeamId.Blue, TeamRules.AssignTeam(2, GameModeKind.ThreeVsThree));
        Assert.Equal(TeamId.Red, TeamRules.AssignTeam(3, GameModeKind.ThreeVsThree));
    }

    [Fact]
    public void Damage_ArmorReducesButNotToZero()
    {
        var mitigated = DamageFormula.Physical(100f, 1f, 100f);
        Assert.Equal(50f, mitigated, 3);
        Assert.True(mitigated > 0f);
    }

    [Fact]
    public void PlayerCommand_ClampsDiagonalMove()
    {
        var cmd = new PlayerCommand { MoveX = 3f, MoveY = 4f }.ClampedMove();
        var len = MathF.Sqrt(cmd.MoveX * cmd.MoveX + cmd.MoveY * cmd.MoveY);
        Assert.InRange(len, 0.99f, 1.01f);
    }

    [Fact]
    public void CombatStats_ApplyDamageKillsAtZero()
    {
        var stats = new CombatStats { MaxHealth = 100f, Health = 100f, Armor = 0f };
        stats.ApplyPhysicalDamage(60f, 1f);
        Assert.Equal(40f, stats.Health, 2);
        stats.ApplyPhysicalDamage(100f, 1f);
        Assert.Equal(0f, stats.Health);
        Assert.False(stats.IsAlive);
    }
}
