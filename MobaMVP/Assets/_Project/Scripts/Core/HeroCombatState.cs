namespace Moba.Core
{
    /// <summary>Legacy Phase1 constants kept for tests / fallback VFX sizing.</summary>
    public static class Phase1HeroKit
    {
        public const float BasicAttackRatio = 1f;
        public const float SkillDamageRatio = 1.8f;
        public const float SkillRange = 4f;
        public const float SkillRadius = 3f;
        public const float SkillCooldown = 5f;
        public const float BaseRespawnSeconds = 5f;
        public const byte SkillSlotShockwave = 1;
    }

    public sealed class HeroCombatState
    {
        public CombatStats Stats { get; } = new CombatStats();
        public StatusEffectState Status { get; } = new StatusEffectState();
        public EquipmentInventory Equipment { get; } = new EquipmentInventory();
        public HeroId HeroId { get; private set; } = HeroId.PanWei;

        public float AttackCooldownRemaining;
        public float Skill1CooldownRemaining;
        public float Skill2CooldownRemaining;
        public float Skill3CooldownRemaining;
        public float RespawnRemaining;
        public float CooldownReduction;
        public RecallChannelState Recall { get; } = new();

        public bool IsDead => !Stats.IsAlive;
        public bool IsRecalling => Recall.IsChanneling;
        public bool CanMove => Stats.IsAlive && !Status.IsStunned;
        public bool CanAct => Stats.IsAlive && !Status.IsStunned && !IsRecalling;

        public float EffectiveMoveSpeed => Stats.MoveSpeed * Status.MoveSpeedMultiplier;

        /// <summary>Skill1 CD mirror for HUD (Phase1 compat).</summary>
        public float SkillCooldownRemaining
        {
            get => Skill1CooldownRemaining;
            set => Skill1CooldownRemaining = value;
        }

        public void ApplyHero(HeroId heroId)
        {
            HeroId = heroId == HeroId.None ? HeroId.PanWei : heroId;
            var def = HeroCatalog.Get(HeroId);
            Stats.MaxHealth = def.MaxHealth;
            Stats.Health = def.MaxHealth;
            Stats.Attack = def.Attack;
            Stats.AbilityPower = def.AbilityPower;
            Stats.Armor = def.Armor;
            Stats.MagicResist = def.MagicResist;
            Stats.MoveSpeed = def.MoveSpeed;
            Stats.AttackRange = def.AttackRange;
            Stats.AttackInterval = def.AttackInterval;
            RecomputeFromEquipment();
            Stats.FullHeal();
        }

        public void RecomputeFromEquipment()
        {
            var def = HeroCatalog.Get(HeroId);
            var hpRatio = Stats.MaxHealth > 0f ? Stats.Health / Stats.MaxHealth : 1f;
            Stats.MaxHealth = def.MaxHealth;
            Stats.Attack = def.Attack;
            Stats.AbilityPower = def.AbilityPower;
            Stats.Armor = def.Armor;
            Stats.MagicResist = def.MagicResist;
            Stats.MoveSpeed = def.MoveSpeed;
            Stats.AttackRange = def.AttackRange;
            Stats.AttackInterval = def.AttackInterval;
            Equipment.ApplyBonuses(Stats, out CooldownReduction);
            Stats.Health = System.MathF.Min(Stats.MaxHealth, Stats.MaxHealth * hpRatio);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            AttackCooldownRemaining = System.MathF.Max(0f, AttackCooldownRemaining - deltaTime);
            Skill1CooldownRemaining = System.MathF.Max(0f, Skill1CooldownRemaining - deltaTime);
            Skill2CooldownRemaining = System.MathF.Max(0f, Skill2CooldownRemaining - deltaTime);
            Skill3CooldownRemaining = System.MathF.Max(0f, Skill3CooldownRemaining - deltaTime);
            Status.Tick(deltaTime);

            if (IsDead)
            {
                Recall.Cancel();
                if (RespawnRemaining > 0f)
                {
                    RespawnRemaining = System.MathF.Max(0f, RespawnRemaining - deltaTime);
                }
            }
        }

        public bool TryStartRecall()
        {
            if (!Stats.IsAlive || Status.IsStunned || IsRecalling)
            {
                return false;
            }

            return Recall.TryStart(RecallRules.ChannelSeconds);
        }

        public void CancelRecall() => Recall.Cancel();

        /// <summary>Returns true when recall channel finishes.</summary>
        public bool TickRecall(float deltaTime) => Recall.Tick(deltaTime);

        public bool TryStartBasicAttack()
        {
            if (!CanAct || AttackCooldownRemaining > 0f)
            {
                return false;
            }

            AttackCooldownRemaining = Stats.AttackInterval;
            return true;
        }

        public float ResolveBasicAttack(CombatStats target)
        {
            return target.ApplyPhysicalDamage(Stats.Attack, Phase1HeroKit.BasicAttackRatio);
        }

        public bool TryStartSkill(SkillSlot slot)
        {
            if (!CanAct)
            {
                return false;
            }

            var def = HeroCatalog.Get(HeroId).GetSkill(slot);
            if (def == null)
            {
                return false;
            }

            ref var cd = ref SkillCdRef(slot);
            if (cd > 0f)
            {
                return false;
            }

            var reduced = def.Cooldown * (1f - CooldownReduction);
            cd = System.MathF.Max(0.5f, reduced);
            return true;
        }

        /// <summary>Phase1 compat: start skill slot one.</summary>
        public bool TryStartSkill()
        {
            return TryStartSkill(SkillSlot.One);
        }

        public float ResolveSkillHit(CombatStats target)
        {
            var power = Stats.AbilityPower > 0f ? Stats.AbilityPower : Stats.Attack;
            return target.ApplyMagicalDamage(power, Phase1HeroKit.SkillDamageRatio);
        }

        public float GetSkillPower(SkillDefinition skill)
        {
            if (skill == null)
            {
                return 0f;
            }

            return skill.DamageType == DamageType.Physical ? Stats.Attack : Stats.AbilityPower;
        }

        public void MarkDead()
        {
            Stats.Health = 0f;
            RespawnRemaining = Phase1HeroKit.BaseRespawnSeconds;
            AttackCooldownRemaining = 0f;
            Status.StunRemaining = 0f;
            Status.ShieldRemaining = 0f;
            Recall.Cancel();
        }

        public bool ReadyToRespawn => IsDead && RespawnRemaining <= 0f;

        public void Respawn()
        {
            RecomputeFromEquipment();
            Stats.FullHeal();
            RespawnRemaining = 0f;
            AttackCooldownRemaining = 0f;
            Skill1CooldownRemaining = 0f;
            Skill2CooldownRemaining = 0f;
            Skill3CooldownRemaining = 0f;
            Status.StunRemaining = 0f;
            Status.SlowRemaining = 0f;
            Status.ShieldRemaining = 0f;
            Status.HasteRemaining = 0f;
            Recall.Cancel();
        }

        public bool TryPurchase(ItemId itemId, PlayerWallet wallet, out string error)
        {
            error = null;
            var def = ItemCatalog.Get(itemId);
            if (def == null)
            {
                error = "unknown item";
                return false;
            }

            if (!wallet.TrySpend(def.Cost))
            {
                error = "not enough gold";
                return false;
            }

            if (!Equipment.TryAdd(itemId, out _))
            {
                wallet.Add(def.Cost);
                error = "inventory full";
                return false;
            }

            RecomputeFromEquipment();
            return true;
        }

        private ref float SkillCdRef(SkillSlot slot)
        {
            switch (slot)
            {
                case SkillSlot.Two:
                    return ref Skill2CooldownRemaining;
                case SkillSlot.Three:
                    return ref Skill3CooldownRemaining;
                default:
                    return ref Skill1CooldownRemaining;
            }
        }
    }

    public static class TargetingRules
    {
        public static bool IsInRange(float distance, float range)
        {
            return distance <= range + 0.05f;
        }

        public static bool TryPickClosest(
            float selfX,
            float selfZ,
            float range,
            float[] enemyXs,
            float[] enemyZs,
            bool[] enemyAlive,
            out int index,
            out float distance)
        {
            index = -1;
            distance = float.MaxValue;
            if (enemyXs == null || enemyZs == null || enemyAlive == null)
            {
                return false;
            }

            var count = System.Math.Min(enemyXs.Length, System.Math.Min(enemyZs.Length, enemyAlive.Length));
            for (var i = 0; i < count; i++)
            {
                if (!enemyAlive[i])
                {
                    continue;
                }

                var dx = enemyXs[i] - selfX;
                var dz = enemyZs[i] - selfZ;
                var d = System.MathF.Sqrt(dx * dx + dz * dz);
                if (d < distance && IsInRange(d, range))
                {
                    distance = d;
                    index = i;
                }
            }

            return index >= 0;
        }

        public static bool IsInCone(
            float selfX,
            float selfZ,
            float dirX,
            float dirZ,
            float targetX,
            float targetZ,
            float range,
            float angleDegrees)
        {
            var dx = targetX - selfX;
            var dz = targetZ - selfZ;
            var dist = System.MathF.Sqrt(dx * dx + dz * dz);
            if (dist > range + 0.05f || dist < 0.001f)
            {
                return false;
            }

            var dirLen = System.MathF.Sqrt(dirX * dirX + dirZ * dirZ);
            if (dirLen < 0.001f)
            {
                return false;
            }

            dirX /= dirLen;
            dirZ /= dirLen;
            dx /= dist;
            dz /= dist;
            var dot = dx * dirX + dz * dirZ;
            var cos = System.MathF.Cos(angleDegrees * 0.5f * (System.MathF.PI / 180f));
            return dot >= cos;
        }
    }
}
