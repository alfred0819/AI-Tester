using Moba.Core;
using Xunit;

namespace Moba.Core.Tests;

public class Phase3HeroItemTests
{
    [Fact]
    public void HeroCatalog_HasFourHeroes()
    {
        Assert.Equal(4, HeroCatalog.All.Count);
        Assert.Equal("磐卫", HeroCatalog.Get(HeroId.PanWei).DisplayName);
        Assert.NotNull(HeroCatalog.Get(HeroId.YingXi).Skill3);
    }

    [Fact]
    public void ApplyHero_SetsBaseStats()
    {
        var state = new HeroCombatState();
        state.ApplyHero(HeroId.JinFa);
        Assert.Equal(HeroId.JinFa, state.HeroId);
        Assert.Equal(70f, state.Stats.AbilityPower, 1);
        Assert.True(state.Stats.AttackRange >= 4f);
    }

    [Fact]
    public void Status_StunBlocksActAndSlowAffectsMove()
    {
        var state = new HeroCombatState();
        state.ApplyHero(HeroId.PanWei);
        state.Status.ApplyStun(1f);
        Assert.False(state.CanAct);
        Assert.False(state.CanMove);

        state.Status.Tick(1f);
        Assert.True(state.CanAct);
        state.Status.ApplySlow(2f, 0.5f);
        Assert.Equal(state.Stats.MoveSpeed * 0.5f, state.EffectiveMoveSpeed, 2);
    }

    [Fact]
    public void Shield_AbsorbsDamage()
    {
        var status = new StatusEffectState();
        status.AddShield(50f);
        var leftover = status.AbsorbDamage(30f);
        Assert.Equal(0f, leftover);
        Assert.Equal(20f, status.ShieldRemaining);
        leftover = status.AbsorbDamage(40f);
        Assert.Equal(20f, leftover);
        Assert.Equal(0f, status.ShieldRemaining);
    }

    [Fact]
    public void PurchaseItem_SpendsGoldAndBuffsAttack()
    {
        var state = new HeroCombatState();
        state.ApplyHero(HeroId.ChuanYun);
        var wallet = new PlayerWallet(500);
        var before = state.Stats.Attack;
        Assert.True(state.TryPurchase(ItemId.IronBlade, wallet, out _));
        Assert.Equal(500 - 300, wallet.Gold);
        Assert.Equal(before + 25f, state.Stats.Attack, 1);
    }

    [Fact]
    public void Purchase_FailsWhenBrokeOrFull()
    {
        var state = new HeroCombatState();
        state.ApplyHero(HeroId.PanWei);
        var wallet = new PlayerWallet(100);
        Assert.False(state.TryPurchase(ItemId.IronBlade, wallet, out var err));
        Assert.Equal("not enough gold", err);

        wallet = new PlayerWallet(5000);
        for (var i = 0; i < EquipmentInventory.SlotCount; i++)
        {
            Assert.True(state.TryPurchase(ItemId.SwiftBoots, wallet, out _));
        }

        Assert.False(state.TryPurchase(ItemId.SwiftBoots, wallet, out err));
        Assert.Equal("inventory full", err);
    }

    [Fact]
    public void SkillCooldown_AppliesReduction()
    {
        var state = new HeroCombatState();
        state.ApplyHero(HeroId.JinFa);
        var wallet = new PlayerWallet(1000);
        Assert.True(state.TryPurchase(ItemId.ChronoCharm, wallet, out _));
        Assert.True(state.TryStartSkill(SkillSlot.One));
        var expected = HeroCatalog.Get(HeroId.JinFa).Skill1.Cooldown * (1f - 0.15f);
        Assert.InRange(state.Skill1CooldownRemaining, expected - 0.05f, expected + 0.05f);
    }

    [Fact]
    public void ConeTargeting_Works()
    {
        Assert.True(TargetingRules.IsInCone(0, 0, 0, 1, 0, 3, 5f, 60f));
        Assert.False(TargetingRules.IsInCone(0, 0, 0, 1, 3, 0, 5f, 60f));
    }
}
