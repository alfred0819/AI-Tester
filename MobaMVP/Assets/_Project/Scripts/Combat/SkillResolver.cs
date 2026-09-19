using UnityEngine;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Resolves data-driven skills against ICombatTarget set.</summary>
    public static class SkillResolver
    {
        public static void Execute(
            HeroCombat caster,
            SkillDefinition skill,
            Vector3 aimDirection,
            Vector3? pointOverride = null)
        {
            if (caster == null || skill == null)
            {
                return;
            }

            aimDirection.y = 0f;
            if (aimDirection.sqrMagnitude < 0.0001f)
            {
                aimDirection = caster.transform.forward;
            }

            aimDirection.Normalize();

            switch (skill.Shape)
            {
                case SkillShape.Dash:
                    ApplyDash(caster, skill, aimDirection);
                    HitAround(caster, skill, caster.WorldPosition, Mathf.Max(skill.Radius, 2f));
                    break;
                case SkillShape.SelfCircle:
                    caster.ApplySelfSupport(skill);
                    if (skill.DamageRatio > 0f || skill.StunDuration > 0f || skill.SlowDuration > 0f)
                    {
                        HitAround(caster, skill, caster.WorldPosition, Mathf.Max(skill.Radius, 0.5f));
                    }

                    break;
                case SkillShape.PointCircle:
                {
                    var point = pointOverride ??
                                caster.WorldPosition + aimDirection * Mathf.Clamp(skill.Range * 0.7f, 2f, skill.Range);
                    HitAround(caster, skill, point, skill.Radius);
                    break;
                }
                case SkillShape.Cone:
                    HitCone(caster, skill, aimDirection);
                    break;
            }
        }

        private static void ApplyDash(HeroCombat caster, SkillDefinition skill, Vector3 aim)
        {
            var dist = skill.DashDistance > 0f ? skill.DashDistance : skill.Range;
            caster.transform.position += aim * dist;
            caster.transform.rotation = Quaternion.LookRotation(aim, Vector3.up);
        }

        private static void HitAround(HeroCombat caster, SkillDefinition skill, Vector3 center, float radius)
        {
            var search = CombatTargeting.HorizontalDistance(caster.WorldPosition, center) + radius + 0.5f;
            CombatTargeting.ForEachEnemyInRadius(caster, Mathf.Max(search, radius), other =>
            {
                if (CombatTargeting.HorizontalDistance(center, other.WorldPosition) > radius + 0.05f)
                {
                    return;
                }

                ApplyHit(caster, skill, other);
            }, includeBuildings: true);
        }

        private static void HitCone(HeroCombat caster, SkillDefinition skill, Vector3 aim)
        {
            var origin = caster.WorldPosition;
            CombatTargeting.ForEachEnemyInRadius(caster, skill.Range, other =>
            {
                if (!TargetingRules.IsInCone(
                        origin.x, origin.z,
                        aim.x, aim.z,
                        other.WorldPosition.x, other.WorldPosition.z,
                        skill.Range,
                        skill.ConeAngleDegrees))
                {
                    return;
                }

                ApplyHit(caster, skill, other);
            }, includeBuildings: true);
        }

        private static void ApplyHit(HeroCombat caster, SkillDefinition skill, ICombatTarget other)
        {
            if (skill.DamageRatio > 0f)
            {
                var power = caster.GetSkillPower(skill);
                other.ReceiveDamage(power, skill.DamageRatio, skill.DamageType, caster);
            }

            if (other is HeroCombat hero)
            {
                hero.ApplyCrowdControl(skill.StunDuration, skill.SlowDuration, skill.SlowMultiplier);
            }
        }
    }
}
