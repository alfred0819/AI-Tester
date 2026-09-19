namespace Moba.Core
{
    public enum TeamId : byte
    {
        None = 0,
        Blue = 1,
        Red = 2
    }

    public static class TeamRules
    {
        public static TeamId AssignTeam(int slotIndex, GameModeKind mode)
        {
            var perTeam = GameModeRules.PlayersPerTeam(mode);
            return slotIndex < perTeam ? TeamId.Blue : TeamId.Red;
        }

        public static bool AreEnemies(TeamId a, TeamId b)
        {
            return a != TeamId.None && b != TeamId.None && a != b;
        }
    }
}
