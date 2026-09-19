using System;

namespace Moba.Core
{
    public enum DamageType : byte
    {
        Physical = 0,
        Magical = 1
    }

    public static class DamageFormula
    {
        public static float Mitigate(float rawDamage, float resistance)
        {
            if (rawDamage <= 0f)
            {
                return 0f;
            }

            if (resistance <= 0f)
            {
                return rawDamage;
            }

            return rawDamage * (1f - resistance / (resistance + 100f));
        }

        public static float Physical(float attack, float skillRatio, float armor)
        {
            return Mitigate(attack * skillRatio, armor);
        }

        public static float Magical(float abilityPower, float skillRatio, float magicResist)
        {
            return Mitigate(abilityPower * skillRatio, magicResist);
        }

        public static float Compute(DamageType type, float power, float ratio, float armor, float magicResist)
        {
            return type == DamageType.Magical
                ? Magical(power, ratio, magicResist)
                : Physical(power, ratio, armor);
        }
    }

    public sealed class CombatStats
    {
        public float MaxHealth = 600f;
        public float Health = 600f;
        public float Attack = 55f;
        public float AbilityPower = 40f;
        public float Armor = 20f;
        public float MagicResist = 20f;
        public float MoveSpeed = 3.5f;
        public float AttackRange = 2.5f;
        public float AttackInterval = 0.75f;

        public bool IsAlive => Health > 0f;

        public float ApplyDamage(DamageType type, float power, float ratio)
        {
            var dmg = DamageFormula.Compute(type, power, ratio, Armor, MagicResist);
            Health = MathF.Max(0f, Health - dmg);
            return dmg;
        }

        public float ApplyPhysicalDamage(float attack, float ratio)
        {
            return ApplyDamage(DamageType.Physical, attack, ratio);
        }

        public float ApplyMagicalDamage(float abilityPower, float ratio)
        {
            return ApplyDamage(DamageType.Magical, abilityPower, ratio);
        }

        public void FullHeal()
        {
            Health = MaxHealth;
        }
    }
}
