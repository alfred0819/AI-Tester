using Moba.Core;
using Xunit;

namespace Moba.Core.Tests;

public class Phase1CombatTests
{
    [Fact]
    public void BasicAttack_AppliesPhysicalAndStartsCooldown()
    {
        var attacker = new HeroCombatState();
        var defender = new CombatStats { MaxHealth = 600f, Health = 600f, Armor = 0f };

        Assert.True(attacker.TryStartBasicAttack());
        var dmg = attacker.ResolveBasicAttack(defender);

        Assert.Equal(attacker.Stats.Attack, dmg, 2);
        Assert.Equal(600f - dmg, defender.Health, 2);
        Assert.True(attacker.AttackCooldownRemaining > 0f);
        Assert.False(attacker.TryStartBasicAttack());
    }

    [Fact]
    public void Skill_HitsWithMagicAndCooldown()
    {
        var caster = new HeroCombatState();
        caster.Stats.AbilityPower = 50f;
        var target = new CombatStats { MaxHealth = 600f, Health = 600f, MagicResist = 0f };

        Assert.True(caster.TryStartSkill());
        var dmg = caster.ResolveSkillHit(target);

        Assert.Equal(50f * Phase1HeroKit.SkillDamageRatio, dmg, 2);
        Assert.False(caster.TryStartSkill());
    }

    [Fact]
    public void Death_ThenRespawn_RestoresHealth()
    {
        var hero = new HeroCombatState();
        hero.Stats.Health = 0f;
        hero.MarkDead();

        Assert.True(hero.IsDead);
        Assert.Equal(Phase1HeroKit.BaseRespawnSeconds, hero.RespawnRemaining);

        hero.Tick(Phase1HeroKit.BaseRespawnSeconds);
        Assert.True(hero.ReadyToRespawn);

        hero.Respawn();
        Assert.False(hero.IsDead);
        Assert.Equal(hero.Stats.MaxHealth, hero.Stats.Health);
        Assert.True(hero.CanMove);
    }

    [Fact]
    public void Targeting_PicksClosestAliveInRange()
    {
        var ok = TargetingRules.TryPickClosest(
            0f, 0f, 5f,
            new[] { 10f, 2f, 3f },
            new[] { 0f, 0f, 0f },
            new[] { true, false, true },
            out var index,
            out var distance);

        Assert.True(ok);
        Assert.Equal(2, index);
        Assert.InRange(distance, 2.9f, 3.1f);
    }

    [Fact]
    public void DeadHero_CannotAttackOrCast()
    {
        var hero = new HeroCombatState();
        hero.MarkDead();
        Assert.False(hero.TryStartBasicAttack());
        Assert.False(hero.TryStartSkill());
        Assert.False(hero.CanMove);
    }
}
