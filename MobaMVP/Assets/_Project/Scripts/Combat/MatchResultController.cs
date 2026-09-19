using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Ends the match when a crystal falls; shows result overlay.</summary>
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

        private bool _ended;

        public bool MatchEnded => _ended || _winner.Value != TeamId.None;

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

        public void EndMatchServer(TeamId winner, TeamId destroyedCrystalTeam)
        {
            if (!IsServer || _ended)
            {
                return;
            }

            _ended = true;
            _winner.Value = winner;
            Debug.Log($"[Match] Winner={winner} (crystal {destroyedCrystalTeam} destroyed)");
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
                _resultText.text = winner == TeamId.Blue
                    ? "蓝方胜利！水晶已被摧毁"
                    : winner == TeamId.Red
                        ? "红方胜利！水晶已被摧毁"
                        : "对局结束";
            }

            Time.timeScale = 1f;
            // Freeze further combat by leaving MatchEnded true; units check optionally later.
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
