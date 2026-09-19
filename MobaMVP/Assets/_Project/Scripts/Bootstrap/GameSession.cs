using UnityEngine;
using Moba.Core;

namespace Moba.Bootstrap
{
    /// <summary>Session choices that survive lobby → arena (host authoritative later).</summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private GameModeKind _mode = GameModeKind.OneVsOne;
        [SerializeField] private string _joinAddress = NetworkDefaults.DefaultAddress;
        [SerializeField] private ushort _port = NetworkDefaults.DefaultPort;
        [SerializeField] private HeroId _selectedHero = HeroId.PanWei;
        [SerializeField] private bool _fillBots = true;

        public GameModeKind Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public bool FillBots
        {
            get => _fillBots;
            set => _fillBots = value;
        }

        public HeroId SelectedHero
        {
            get => _selectedHero == HeroId.None ? HeroId.PanWei : _selectedHero;
            set => _selectedHero = value == HeroId.None ? HeroId.PanWei : value;
        }

        public string JoinAddress
        {
            get => _joinAddress;
            set => _joinAddress = string.IsNullOrWhiteSpace(value)
                ? NetworkDefaults.DefaultAddress
                : value.Trim();
        }

        public ushort Port
        {
            get => _port;
            set => _port = value == 0 ? NetworkDefaults.DefaultPort : value;
        }

        public int MaxPlayers => GameModeRules.MaxPlayers(_mode);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
