namespace Moba.Core
{
    /// <summary>Phase 5 balance targets: roughly 8–15 minutes to threaten high-ground tower.</summary>
    public static class BalanceRules
    {
        public const float TargetPushMinutesMin = 8f;
        public const float TargetPushMinutesMax = 15f;

        /// <summary>Rough seconds for 3 melee minions + 1 hero to burn a tower if uncontested.</summary>
        public static float EstimateTowerBurnSeconds(
            float towerHp,
            float towerArmor,
            float heroDps,
            float minionDps,
            int minionCount)
        {
            var totalDps = heroDps + minionDps * minionCount;
            if (totalDps <= 0.01f)
            {
                return float.MaxValue;
            }

            // Approximate armor mitigation at towerArmor.
            var mitigated = totalDps * (1f - towerArmor / (towerArmor + 100f));
            if (mitigated <= 0.01f)
            {
                return float.MaxValue;
            }

            return towerHp / mitigated;
        }

        public static bool IsBurnWithinPushWindow(float burnSeconds)
        {
            // Continuous uncontested DPS burn should be quick, but not instant;
            // real matches stretch toward 8–15 min via wave cadence and fights.
            return burnSeconds >= 15f && burnSeconds <= TargetPushMinutesMax * 60f;
        }
    }
}
