namespace Moba.Core
{
    /// <summary>Match size for lobby / room approval.</summary>
    public enum GameModeKind
    {
        OneVsOne = 0,
        ThreeVsThree = 1
    }

    public static class GameModeRules
    {
        public static int MaxPlayers(GameModeKind mode)
        {
            return mode == GameModeKind.ThreeVsThree ? 6 : 2;
        }

        public static int PlayersPerTeam(GameModeKind mode)
        {
            return mode == GameModeKind.ThreeVsThree ? 3 : 1;
        }
    }
}
