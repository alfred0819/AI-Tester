using UnityEngine;
using Unity.Netcode;
using Moba.Core;

namespace Moba.Combat
{
    public interface ICombatTarget
    {
        TeamId Team { get; }
        bool IsAlive { get; }
        bool IsAttackable { get; }
        CombatTargetKind Kind { get; }
        Vector3 WorldPosition { get; }
        float Health { get; }
        float MaxHealth { get; }
        NetworkObject NetworkObject { get; }

        /// <summary>Server-only. Returns damage dealt. Awards bounty to killer on fatal blow.</summary>
        float ReceiveDamage(float power, float ratio, DamageType type, ICombatTarget attacker);
    }

    public static class CombatTargeting
    {
        public static bool TryFindNearestEnemy(
            ICombatTarget self,
            float range,
            out ICombatTarget target,
            bool includeBuildings = true)
        {
            target = null;
            if (self == null || !self.IsAlive)
            {
                return false;
            }

            var best = float.MaxValue;
            var selfPos = self.WorldPosition;

            foreach (var candidate in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is not ICombatTarget other || ReferenceEquals(other, self))
                {
                    continue;
                }

                if (!other.IsAlive || !other.IsAttackable)
                {
                    continue;
                }

                if (!TeamRules.AreEnemies(self.Team, other.Team))
                {
                    continue;
                }

                if (!includeBuildings &&
                    (other.Kind == CombatTargetKind.Tower || other.Kind == CombatTargetKind.Crystal))
                {
                    continue;
                }

                var d = HorizontalDistance(selfPos, other.WorldPosition);
                if (d < best && TargetingRules.IsInRange(d, range))
                {
                    best = d;
                    target = other;
                }
            }

            return target != null;
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static void ForEachEnemyInRadius(
            ICombatTarget self,
            float radius,
            System.Action<ICombatTarget> action,
            bool includeBuildings = true)
        {
            if (self == null || action == null)
            {
                return;
            }

            var origin = self.WorldPosition;
            foreach (var candidate in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (candidate is not ICombatTarget other || ReferenceEquals(other, self))
                {
                    continue;
                }

                if (!other.IsAlive || !other.IsAttackable)
                {
                    continue;
                }

                if (!TeamRules.AreEnemies(self.Team, other.Team))
                {
                    continue;
                }

                if (!includeBuildings &&
                    (other.Kind == CombatTargetKind.Tower || other.Kind == CombatTargetKind.Crystal))
                {
                    continue;
                }

                if (HorizontalDistance(origin, other.WorldPosition) <= radius)
                {
                    action(other);
                }
            }
        }
    }
}
