namespace Moba.Core
{
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Lobby = "Lobby";
        public const string Arena1Lane = "Arena_1Lane";
    }

    public static class NetworkDefaults
    {
        public const ushort DefaultPort = 7777;
        public const string DefaultAddress = "127.0.0.1";
        public const float SimulationTickRate = 20f;
        public const float SimulationDelta = 1f / SimulationTickRate;
    }
}
