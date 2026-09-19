using Moba.Core;
using Xunit;

namespace Moba.Core.Tests;

public class Phase4BotTests
{
    [Fact]
    public void BotsNeeded_FillsToMax()
    {
        Assert.Equal(5, BotBrain.BotsNeeded(1, GameModeKind.ThreeVsThree));
        Assert.Equal(1, BotBrain.BotsNeeded(1, GameModeKind.OneVsOne));
        Assert.Equal(0, BotBrain.BotsNeeded(6, GameModeKind.ThreeVsThree));
    }

    [Fact]
    public void SpawnIndex_WrapsPerTeam()
    {
        Assert.Equal(0, BotBrain.SpawnIndexInTeam(0, GameModeKind.ThreeVsThree));
        Assert.Equal(2, BotBrain.SpawnIndexInTeam(2, GameModeKind.ThreeVsThree));
        Assert.Equal(0, BotBrain.SpawnIndexInTeam(3, GameModeKind.ThreeVsThree));
        Assert.Equal(1, BotBrain.SpawnIndexInTeam(4, GameModeKind.ThreeVsThree));
    }

    [Fact]
    public void Decide_RetreatWhenLowHp()
    {
        var p = BasePerception();
        p.HealthRatio = 0.2f;
        p.FountainX = 0f;
        p.FountainZ = -40f;
        p.SelfZ = 0f;
        var action = BotBrain.Decide(p, out var mx, out var mz);
        Assert.Equal(BotActionKind.Retreat, action);
        Assert.True(mz < 0f);
    }

    [Fact]
    public void Decide_AttackOrSkillInRange()
    {
        var p = BasePerception();
        p.HasEnemyInAttackRange = true;
        p.Skill1Ready = true;
        Assert.Equal(BotActionKind.CastSkill1, BotBrain.Decide(p, out _, out _));

        p.Skill1Ready = false;
        Assert.Equal(BotActionKind.Attack, BotBrain.Decide(p, out _, out _));
    }

    [Fact]
    public void Decide_MoveDownLaneWhenNoEnemy()
    {
        var p = BasePerception();
        p.LaneTargetZ = 40f;
        p.SelfZ = -20f;
        var action = BotBrain.Decide(p, out _, out var mz);
        Assert.Equal(BotActionKind.Move, action);
        Assert.True(mz > 0f);
    }

    private static BotPerception BasePerception()
    {
        return new BotPerception
        {
            CanAct = true,
            HealthRatio = 1f,
            AttackRange = 2.5f,
            AggroRange = 7f,
            SelfX = 0f,
            SelfZ = 0f,
            FountainX = 0f,
            FountainZ = -40f,
            LaneTargetX = 0f,
            LaneTargetZ = 40f
        };
    }
}
