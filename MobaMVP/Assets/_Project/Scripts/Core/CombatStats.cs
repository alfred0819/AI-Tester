using System;

namespace Moba.Core
{
    public static class DamageFormula
    {
        /// <summary>Simple diminishing armor curve used by the MVP.</summary>
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
    }

    public sealed class CombatStats
    {
        public float MaxHealth = 600f;
        public float Health = 600f;
        public float Attack = 50f;
        public float AbilityPower;
        public float Armor = 20f;
        public float MagicResist = 20f;
        public float MoveSpeed = 3.5f;
        public float AttackRange = 2f;
        public float AttackInterval = 0.8f;

        public bool IsAlive => Health > 0f;

        public float ApplyPhysicalDamage(float attack, float ratio)
        {
            var dmg = DamageFormula.Physical(attack, ratio, Armor);
            Health = MathF.Max(0f, Health - dmg);
            return dmg;
        }
    }
}
