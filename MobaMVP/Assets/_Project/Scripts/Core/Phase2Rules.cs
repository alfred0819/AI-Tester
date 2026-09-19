namespace Moba.Core
{
    public enum CombatTargetKind : byte
    {
        Hero = 0,
        Minion = 1,
        Tower = 2,
        Crystal = 3,
        Dummy = 4
    }

    public static class LaneWaveRules
    {
        public const float WaveIntervalSeconds = 25f;
        public const float FirstWaveDelaySeconds = 5f;
        public const int MeleePerWave = 3;
        public const float MinionMaxHealth = 280f;
        public const float MinionAttack = 18f;
        public const float MinionArmor = 5f;
        public const float MinionMoveSpeed = 2.8f;
        public const float MinionAttackRange = 1.6f;
        public const float MinionAttackInterval = 1.0f;
        public const float MinionAggroRange = 4.5f;
    }

    public static class BuildingRules
    {
        public const float TowerMaxHealth = 2500f;
        public const float TowerAttack = 90f;
        public const float TowerArmor = 40f;
        public const float TowerAttackRange = 8f;
        public const float TowerAttackInterval = 1.1f;

        public const float CrystalMaxHealth = 3500f;
        public const float CrystalArmor = 50f;
    }

    public static class EconomyRules
    {
        public const int StartingGold = 0;
        public const int GoldPerMinion = 18;
        public const int GoldPerTower = 150;
        public const int GoldPerHeroKill = 200;
        public const float PassiveGoldPerSecond = 1.2f;

        public static int BountyFor(CombatTargetKind kind)
        {
            return kind switch
            {
                CombatTargetKind.Minion => GoldPerMinion,
                CombatTargetKind.Tower => GoldPerTower,
                CombatTargetKind.Hero => GoldPerHeroKill,
                CombatTargetKind.Dummy => GoldPerMinion,
                _ => 0
            };
        }
    }

    public sealed class PlayerWallet
    {
        public int Gold { get; private set; }

        public PlayerWallet(int starting = 0)
        {
            Gold = starting;
        }

        public void Add(int amount)
        {
            if (amount > 0)
            {
                Gold += amount;
            }
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Gold < amount)
            {
                return false;
            }

            Gold -= amount;
            return true;
        }
    }

    public static class MatchWinRules
    {
        public static TeamId WinnerWhenCrystalDestroyed(TeamId destroyedCrystalTeam)
        {
            if (destroyedCrystalTeam == TeamId.Blue)
            {
                return TeamId.Red;
            }

            if (destroyedCrystalTeam == TeamId.Red)
            {
                return TeamId.Blue;
            }

            return TeamId.None;
        }
    }
}
