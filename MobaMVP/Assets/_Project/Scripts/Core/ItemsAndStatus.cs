using System;

namespace Moba.Core
{
    public enum ItemId : byte
    {
        None = 0,
        IronBlade = 1,
        ArcaneRod = 2,
        SwiftBoots = 3,
        ChainVest = 4,
        VitalPendant = 5,
        ChronoCharm = 6
    }

    public sealed class ItemDefinition
    {
        public ItemId Id;
        public string DisplayName;
        public int Cost;
        public float BonusAttack;
        public float BonusAbilityPower;
        public float BonusArmor;
        public float BonusMaxHealth;
        public float BonusMoveSpeed;
        public float CooldownReduction; // 0.15 = 15%
    }

    public static class ItemCatalog
    {
        public static readonly ItemDefinition[] All =
        {
            new ItemDefinition
            {
                Id = ItemId.IronBlade, DisplayName = "精铁刃", Cost = 300,
                BonusAttack = 25f
            },
            new ItemDefinition
            {
                Id = ItemId.ArcaneRod, DisplayName = "奥术杖", Cost = 320,
                BonusAbilityPower = 40f
            },
            new ItemDefinition
            {
                Id = ItemId.SwiftBoots, DisplayName = "疾行靴", Cost = 250,
                BonusMoveSpeed = 0.55f
            },
            new ItemDefinition
            {
                Id = ItemId.ChainVest, DisplayName = "锁子甲", Cost = 300,
                BonusArmor = 30f
            },
            new ItemDefinition
            {
                Id = ItemId.VitalPendant, DisplayName = "活力坠", Cost = 350,
                BonusMaxHealth = 220f
            },
            new ItemDefinition
            {
                Id = ItemId.ChronoCharm, DisplayName = "时砂符", Cost = 400,
                CooldownReduction = 0.15f, BonusAbilityPower = 15f
            }
        };

        public static ItemDefinition Get(ItemId id)
        {
            foreach (var item in All)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            return null;
        }
    }

    public sealed class EquipmentInventory
    {
        public const int SlotCount = 6;
        private readonly ItemId[] _slots = new ItemId[SlotCount];

        public ItemId GetSlot(int index)
        {
            if (index < 0 || index >= SlotCount)
            {
                return ItemId.None;
            }

            return _slots[index];
        }

        public bool TryAdd(ItemId id, out int slotIndex)
        {
            slotIndex = -1;
            if (id == ItemId.None || ItemCatalog.Get(id) == null)
            {
                return false;
            }

            for (var i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == ItemId.None)
                {
                    _slots[i] = id;
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

        public void Clear()
        {
            for (var i = 0; i < SlotCount; i++)
            {
                _slots[i] = ItemId.None;
            }
        }

        public void ApplyBonuses(CombatStats stats, out float cooldownReduction)
        {
            cooldownReduction = 0f;
            for (var i = 0; i < SlotCount; i++)
            {
                var def = ItemCatalog.Get(_slots[i]);
                if (def == null)
                {
                    continue;
                }

                stats.Attack += def.BonusAttack;
                stats.AbilityPower += def.BonusAbilityPower;
                stats.Armor += def.BonusArmor;
                stats.MoveSpeed += def.BonusMoveSpeed;
                if (def.BonusMaxHealth > 0f)
                {
                    var ratio = stats.MaxHealth > 0f ? stats.Health / stats.MaxHealth : 1f;
                    stats.MaxHealth += def.BonusMaxHealth;
                    stats.Health = MathF.Min(stats.MaxHealth, stats.MaxHealth * ratio);
                }

                cooldownReduction += def.CooldownReduction;
            }

            if (cooldownReduction > 0.4f)
            {
                cooldownReduction = 0.4f;
            }
        }
    }

    /// <summary>Runtime stun / slow / shield / haste on a combatant.</summary>
    public sealed class StatusEffectState
    {
        public float StunRemaining;
        public float SlowRemaining;
        public float SlowMultiplier = 1f;
        public float ShieldRemaining;
        public float HasteRemaining;
        public float HasteMultiplier = 1f;

        public bool IsStunned => StunRemaining > 0f;

        public void Tick(float dt)
        {
            if (dt <= 0f)
            {
                return;
            }

            StunRemaining = MathF.Max(0f, StunRemaining - dt);
            SlowRemaining = MathF.Max(0f, SlowRemaining - dt);
            HasteRemaining = MathF.Max(0f, HasteRemaining - dt);
            if (SlowRemaining <= 0f)
            {
                SlowMultiplier = 1f;
            }

            if (HasteRemaining <= 0f)
            {
                HasteMultiplier = 1f;
            }
        }

        public void ApplyStun(float duration)
        {
            if (duration > StunRemaining)
            {
                StunRemaining = duration;
            }
        }

        public void ApplySlow(float duration, float multiplier)
        {
            SlowRemaining = MathF.Max(SlowRemaining, duration);
            SlowMultiplier = MathF.Min(SlowMultiplier, MathF.Max(0.2f, multiplier));
        }

        public void AddShield(float amount)
        {
            if (amount > 0f)
            {
                ShieldRemaining += amount;
            }
        }

        public void ApplyHaste(float duration, float multiplier)
        {
            HasteRemaining = MathF.Max(HasteRemaining, duration);
            HasteMultiplier = MathF.Max(HasteMultiplier, multiplier);
        }

        public float AbsorbDamage(float damage)
        {
            if (damage <= 0f || ShieldRemaining <= 0f)
            {
                return damage;
            }

            var absorbed = MathF.Min(ShieldRemaining, damage);
            ShieldRemaining -= absorbed;
            return damage - absorbed;
        }

        public float MoveSpeedMultiplier
        {
            get
            {
                if (IsStunned)
                {
                    return 0f;
                }

                var mul = 1f;
                if (SlowRemaining > 0f)
                {
                    mul *= SlowMultiplier;
                }

                if (HasteRemaining > 0f)
                {
                    mul *= HasteMultiplier;
                }

                return mul;
            }
        }
    }
}
