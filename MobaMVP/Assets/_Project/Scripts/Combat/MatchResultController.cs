using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Ends the match; shows KDA/gold/duration scoreboard; tracks human roster for disconnect.</summary>
    public sealed class MatchResultController : NetworkBehaviour
    {
        public static MatchResultController Instance { get; private set; }

        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _backButton;

        private readonly NetworkVariable<TeamId> _winner = new(
            TeamId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _duration = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<byte> _endReason = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly Dictionary<ulong, TeamId> _humanTeams = new();
        private bool _ended;
        private float _elapsed;

        public bool MatchEnded => _ended || _winner.Value != TeamId.None;
        public float MatchDuration => _duration.Value > 0f ? _duration.Value : _elapsed;

        private void Awake()
        {
            Instance = this;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public override void OnNetworkSpawn()
        {
            _winner.OnValueChanged += OnWinnerChanged;
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(OnBackClicked);
            }

            if (_winner.Value != TeamId.None)
            {
                ShowResult(_winner.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _winner.OnValueChanged -= OnWinnerChanged;
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackClicked);
            }
        }

        private void Update()
        {
            if (!IsServer || _ended)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            _duration.Value = _elapsed;
        }

        public void RegisterHumanClient(ulong clientId, TeamId team)
        {
            if (!IsServer || team == TeamId.None)
            {
                return;
            }

            _humanTeams[clientId] = team;
        }

        public bool TryGetHumanTeam(ulong clientId, out TeamId team)
        {
            return _humanTeams.TryGetValue(clientId, out team);
        }

        public void EndMatchServer(TeamId winner, TeamId destroyedCrystalTeam)
        {
            EndMatchServer(winner, endReason: 0);
            Debug.Log($"[Match] Winner={winner} (crystal {destroyedCrystalTeam} destroyed)");
        }

        public void EndMatchServer(TeamId winner, byte endReason)
        {
            if (!IsServer || _ended)
            {
                return;
            }

            _ended = true;
            _duration.Value = _elapsed;
            _endReason.Value = endReason;
            _winner.Value = winner;
        }

        private void OnWinnerChanged(TeamId previous, TeamId next)
        {
            if (next != TeamId.None)
            {
                ShowResult(next);
            }
        }

        private void ShowResult(TeamId winner)
        {
            _ended = true;
            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            if (_resultText != null)
            {
                _resultText.text = BuildScoreboardText(winner);
            }

            Time.timeScale = 1f;
        }

        private string BuildScoreboardText(TeamId winner)
        {
            var label = winner == TeamId.Blue
                ? "蓝方胜利！"
                : winner == TeamId.Red
                    ? "红方胜利！"
                    : "对局结束";
            if (_endReason.Value == 1)
            {
                label += "（对手断线）";
            }
            else if (winner != TeamId.None)
            {
                label += " 水晶已被摧毁";
            }

            var entries = new List<ScoreboardEntry>();
            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (combat.Kind != CombatTargetKind.Hero)
                {
                    continue;
                }

                entries.Add(combat.ToScoreboardEntry());
            }

            entries.Sort((a, b) =>
            {
                var ta = a.Team == TeamId.Blue ? 0 : 1;
                var tb = b.Team == TeamId.Blue ? 0 : 1;
                if (ta != tb)
                {
                    return ta.CompareTo(tb);
                }

                return b.Kills.CompareTo(a.Kills);
            });

            return MatchScoreboard.FormatResult(label, MatchDuration, entries);
        }

        private void OnBackClicked()
        {
            Time.timeScale = 1f;
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening)
            {
                nm.Shutdown();
            }

            SceneManager.LoadScene(SceneNames.Lobby);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            Time.timeScale = 1f;
        }
    }
}
