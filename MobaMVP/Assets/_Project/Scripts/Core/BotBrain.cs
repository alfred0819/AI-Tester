using System;

namespace Moba.Core
{
    public enum BotActionKind : byte
    {
        None = 0,
        Move = 1,
        Attack = 2,
        CastSkill1 = 3,
        CastSkill2 = 4,
        CastSkill3 = 5,
        Retreat = 6
    }

    public struct BotPerception
    {
        public float SelfX;
        public float SelfZ;
        public float HealthRatio;
        public bool CanAct;
        public bool Skill1Ready;
        public bool Skill2Ready;
        public bool Skill3Ready;
        public bool HasEnemyInAttackRange;
        public bool HasEnemyInAggroRange;
        public float EnemyX;
        public float EnemyZ;
        public float FountainX;
        public float FountainZ;
        public float LaneTargetX;
        public float LaneTargetZ;
        public float AttackRange;
        public float AggroRange;
    }

    public static class BotBrain
    {
        public const float RetreatHealthRatio = 0.28f;
        public const float DefaultAggroRange = 7f;

        public static BotActionKind Decide(in BotPerception p, out float moveX, out float moveZ)
        {
            moveX = 0f;
            moveZ = 0f;

            if (!p.CanAct)
            {
                return BotActionKind.None;
            }

            if (p.HealthRatio <= RetreatHealthRatio)
            {
                Aim(p.SelfX, p.SelfZ, p.FountainX, p.FountainZ, out moveX, out moveZ);
                return BotActionKind.Retreat;
            }

            if (p.HasEnemyInAttackRange)
            {
                if (p.Skill3Ready)
                {
                    return BotActionKind.CastSkill3;
                }

                if (p.Skill1Ready)
                {
                    return BotActionKind.CastSkill1;
                }

                if (p.Skill2Ready)
                {
                    return BotActionKind.CastSkill2;
                }

                return BotActionKind.Attack;
            }

            if (p.HasEnemyInAggroRange)
            {
                Aim(p.SelfX, p.SelfZ, p.EnemyX, p.EnemyZ, out moveX, out moveZ);
                if (p.Skill1Ready)
                {
                    return BotActionKind.CastSkill1;
                }

                return BotActionKind.Move;
            }

            Aim(p.SelfX, p.SelfZ, p.LaneTargetX, p.LaneTargetZ, out moveX, out moveZ);
            return BotActionKind.Move;
        }

        public static int SpawnIndexInTeam(int globalSlot, GameModeKind mode)
        {
            var perTeam = GameModeRules.PlayersPerTeam(mode);
            return globalSlot % perTeam;
        }

        public static int BotsNeeded(int humanCount, GameModeKind mode)
        {
            var max = GameModeRules.MaxPlayers(mode);
            var need = max - humanCount;
            return need < 0 ? 0 : need;
        }

        public static HeroId PickHeroForSlot(int slot)
        {
            return (slot % 4) switch
            {
                0 => HeroId.PanWei,
                1 => HeroId.YingXi,
                2 => HeroId.JinFa,
                _ => HeroId.ChuanYun
            };
        }

        private static void Aim(float selfX, float selfZ, float tx, float tz, out float mx, out float mz)
        {
            mx = tx - selfX;
            mz = tz - selfZ;
            var len = MathF.Sqrt(mx * mx + mz * mz);
            if (len < 0.001f)
            {
                mx = 0f;
                mz = 0f;
                return;
            }

            mx /= len;
            mz /= len;
        }
    }
}
