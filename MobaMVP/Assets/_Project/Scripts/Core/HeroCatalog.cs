using System;
using System.Collections.Generic;

namespace Moba.Core
{
    public enum HeroId : byte
    {
        None = 0,
        PanWei = 1,   // 坦克
        YingXi = 2,   // 刺客
        JinFa = 3,    // 法师
        ChuanYun = 4  // 射手
    }

    public enum SkillSlot : byte
    {
        None = 0,
        One = 1,
        Two = 2,
        Three = 3
    }

    public enum SkillShape : byte
    {
        SelfCircle = 0,
        Cone = 1,
        Dash = 2,
        PointCircle = 3
    }

    public sealed class SkillDefinition
    {
        public string Id;
        public string DisplayName;
        public SkillShape Shape;
        public float Cooldown = 5f;
        public float Range = 4f;
        public float Radius = 3f;
        public float ConeAngleDegrees = 60f;
        public float DamageRatio = 1f;
        public DamageType DamageType = DamageType.Magical;
        public float StunDuration;
        public float SlowDuration;
        public float SlowMultiplier = 0.5f;
        public float ShieldAmount;
        public float DashDistance;
        public float MoveSpeedBuffDuration;
        public float MoveSpeedBuffMultiplier = 1.3f;
    }

    public sealed class HeroDefinition
    {
        public HeroId Id;
        public string DisplayName;
        public float MaxHealth = 600f;
        public float Attack = 55f;
        public float AbilityPower = 40f;
        public float Armor = 20f;
        public float MagicResist = 20f;
        public float MoveSpeed = 3.5f;
        public float AttackRange = 2.5f;
        public float AttackInterval = 0.75f;
        public SkillDefinition Skill1;
        public SkillDefinition Skill2;
        public SkillDefinition Skill3;

        public SkillDefinition GetSkill(SkillSlot slot)
        {
            return slot switch
            {
                SkillSlot.One => Skill1,
                SkillSlot.Two => Skill2,
                SkillSlot.Three => Skill3,
                _ => null
            };
        }
    }

    public static class HeroCatalog
    {
        private static readonly Dictionary<HeroId, HeroDefinition> Heroes = Build();

        public static IReadOnlyDictionary<HeroId, HeroDefinition> All => Heroes;

        public static HeroDefinition Get(HeroId id)
        {
            if (id == HeroId.None)
            {
                id = HeroId.PanWei;
            }

            return Heroes.TryGetValue(id, out var def) ? def : Heroes[HeroId.PanWei];
        }

        public static string[] DisplayNames()
        {
            return new[]
            {
                Heroes[HeroId.PanWei].DisplayName,
                Heroes[HeroId.YingXi].DisplayName,
                Heroes[HeroId.JinFa].DisplayName,
                Heroes[HeroId.ChuanYun].DisplayName
            };
        }

        public static HeroId FromDropdownIndex(int index)
        {
            return index switch
            {
                1 => HeroId.YingXi,
                2 => HeroId.JinFa,
                3 => HeroId.ChuanYun,
                _ => HeroId.PanWei
            };
        }

        private static Dictionary<HeroId, HeroDefinition> Build()
        {
            return new Dictionary<HeroId, HeroDefinition>
            {
                [HeroId.PanWei] = new HeroDefinition
                {
                    Id = HeroId.PanWei,
                    DisplayName = "磐卫",
                    MaxHealth = 780f,
                    Attack = 50f,
                    AbilityPower = 20f,
                    Armor = 35f,
                    MagicResist = 30f,
                    MoveSpeed = 3.3f,
                    AttackRange = 2.2f,
                    Skill1 = new SkillDefinition
                    {
                        Id = "panwei_slam",
                        DisplayName = "盾击",
                        Shape = SkillShape.SelfCircle,
                        Cooldown = 6f,
                        Radius = 3.2f,
                        DamageRatio = 1.2f,
                        DamageType = DamageType.Physical,
                        StunDuration = 0.8f
                    },
                    Skill2 = new SkillDefinition
                    {
                        Id = "panwei_guard",
                        DisplayName = "铁壁",
                        Shape = SkillShape.SelfCircle,
                        Cooldown = 10f,
                        Radius = 0.1f,
                        ShieldAmount = 180f
                    },
                    Skill3 = new SkillDefinition
                    {
                        Id = "panwei_charge",
                        DisplayName = "冲阵",
                        Shape = SkillShape.Dash,
                        Cooldown = 12f,
                        DashDistance = 5f,
                        Range = 5f,
                        Radius = 2.5f,
                        DamageRatio = 1.5f,
                        DamageType = DamageType.Physical,
                        StunDuration = 1.0f
                    }
                },
                [HeroId.YingXi] = new HeroDefinition
                {
                    Id = HeroId.YingXi,
                    DisplayName = "影袭",
                    MaxHealth = 560f,
                    Attack = 62f,
                    AbilityPower = 15f,
                    Armor = 18f,
                    MoveSpeed = 3.7f,
                    AttackRange = 2.0f,
                    AttackInterval = 0.65f,
                    Skill1 = new SkillDefinition
                    {
                        Id = "yingxi_dash",
                        DisplayName = "影步",
                        Shape = SkillShape.Dash,
                        Cooldown = 7f,
                        DashDistance = 4.5f,
                        Range = 4.5f,
                        Radius = 2f,
                        DamageRatio = 1.1f,
                        DamageType = DamageType.Physical
                    },
                    Skill2 = new SkillDefinition
                    {
                        Id = "yingxi_mark",
                        DisplayName = "裂痕",
                        Shape = SkillShape.Cone,
                        Cooldown = 8f,
                        Range = 3.5f,
                        Radius = 3.5f,
                        ConeAngleDegrees = 70f,
                        DamageRatio = 1.6f,
                        DamageType = DamageType.Physical,
                        SlowDuration = 1.5f,
                        SlowMultiplier = 0.55f
                    },
                    Skill3 = new SkillDefinition
                    {
                        Id = "yingxi_execute",
                        DisplayName = "绝杀",
                        Shape = SkillShape.SelfCircle,
                        Cooldown = 14f,
                        Radius = 2.8f,
                        DamageRatio = 2.2f,
                        DamageType = DamageType.Physical
                    }
                },
                [HeroId.JinFa] = new HeroDefinition
                {
                    Id = HeroId.JinFa,
                    DisplayName = "烬法",
                    MaxHealth = 520f,
                    Attack = 40f,
                    AbilityPower = 70f,
                    Armor = 15f,
                    MagicResist = 25f,
                    MoveSpeed = 3.4f,
                    AttackRange = 4.5f,
                    Skill1 = new SkillDefinition
                    {
                        Id = "jinfa_bolt",
                        DisplayName = "炎弹",
                        Shape = SkillShape.Cone,
                        Cooldown = 5f,
                        Range = 6f,
                        Radius = 6f,
                        ConeAngleDegrees = 35f,
                        DamageRatio = 1.4f,
                        DamageType = DamageType.Magical
                    },
                    Skill2 = new SkillDefinition
                    {
                        Id = "jinfa_zone",
                        DisplayName = "霜域",
                        Shape = SkillShape.PointCircle,
                        Cooldown = 9f,
                        Range = 5f,
                        Radius = 3f,
                        DamageRatio = 0.9f,
                        DamageType = DamageType.Magical,
                        SlowDuration = 2f,
                        SlowMultiplier = 0.45f
                    },
                    Skill3 = new SkillDefinition
                    {
                        Id = "jinfa_storm",
                        DisplayName = "烬狱",
                        Shape = SkillShape.SelfCircle,
                        Cooldown = 16f,
                        Radius = 4f,
                        DamageRatio = 2.0f,
                        DamageType = DamageType.Magical
                    }
                },
                [HeroId.ChuanYun] = new HeroDefinition
                {
                    Id = HeroId.ChuanYun,
                    DisplayName = "穿云",
                    MaxHealth = 540f,
                    Attack = 68f,
                    AbilityPower = 10f,
                    Armor = 16f,
                    MoveSpeed = 3.5f,
                    AttackRange = 5.5f,
                    AttackInterval = 0.7f,
                    Skill1 = new SkillDefinition
                    {
                        Id = "chuanyun_empower",
                        DisplayName = "穿甲矢",
                        Shape = SkillShape.Cone,
                        Cooldown = 6f,
                        Range = 7f,
                        Radius = 7f,
                        ConeAngleDegrees = 25f,
                        DamageRatio = 1.5f,
                        DamageType = DamageType.Physical
                    },
                    Skill2 = new SkillDefinition
                    {
                        Id = "chuanyun_haste",
                        DisplayName = "疾风",
                        Shape = SkillShape.SelfCircle,
                        Cooldown = 10f,
                        Radius = 0.1f,
                        MoveSpeedBuffDuration = 3f,
                        MoveSpeedBuffMultiplier = 1.45f
                    },
                    Skill3 = new SkillDefinition
                    {
                        Id = "chuanyun_rain",
                        DisplayName = "万箭",
                        Shape = SkillShape.PointCircle,
                        Cooldown = 15f,
                        Range = 8f,
                        Radius = 3.5f,
                        DamageRatio = 1.8f,
                        DamageType = DamageType.Physical,
                        SlowDuration = 1.2f,
                        SlowMultiplier = 0.6f
                    }
                }
            };
        }
    }
}
