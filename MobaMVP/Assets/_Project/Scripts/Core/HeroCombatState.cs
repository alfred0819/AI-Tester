namespace Moba.Core
{
    /// <summary>Phase 1 greybox hero: basic attack + one circle skill.</summary>
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
        public float AttackCooldownRemaining;
        public float SkillCooldownRemaining;
        public float RespawnRemaining;
        public bool IsDead => !Stats.IsAlive;

        public bool CanMove => Stats.IsAlive;
        public bool CanAct => Stats.IsAlive;

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (AttackCooldownRemaining > 0f)
            {
                AttackCooldownRemaining = System.MathF.Max(0f, AttackCooldownRemaining - deltaTime);
            }

            if (SkillCooldownRemaining > 0f)
            {
                SkillCooldownRemaining = System.MathF.Max(0f, SkillCooldownRemaining - deltaTime);
            }

            if (IsDead && RespawnRemaining > 0f)
            {
                RespawnRemaining = System.MathF.Max(0f, RespawnRemaining - deltaTime);
            }
        }

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

        public bool TryStartSkill()
        {
            if (!CanAct || SkillCooldownRemaining > 0f)
            {
                return false;
            }

            SkillCooldownRemaining = Phase1HeroKit.SkillCooldown;
            return true;
        }

        public float ResolveSkillHit(CombatStats target)
        {
            var power = Stats.AbilityPower > 0f ? Stats.AbilityPower : Stats.Attack;
            return target.ApplyMagicalDamage(power, Phase1HeroKit.SkillDamageRatio);
        }

        public void MarkDead()
        {
            Stats.Health = 0f;
            RespawnRemaining = Phase1HeroKit.BaseRespawnSeconds;
            AttackCooldownRemaining = 0f;
        }

        public bool ReadyToRespawn => IsDead && RespawnRemaining <= 0f;

        public void Respawn()
        {
            Stats.FullHeal();
            RespawnRemaining = 0f;
            AttackCooldownRemaining = 0f;
            SkillCooldownRemaining = 0f;
        }
    }

    public static class TargetingRules
    {
        public static bool IsInRange(float distance, float range)
        {
            return distance <= range + 0.05f;
        }

        /// <summary>Prefer closest living enemy within range; returns true if a candidate exists.</summary>
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
    }
}
