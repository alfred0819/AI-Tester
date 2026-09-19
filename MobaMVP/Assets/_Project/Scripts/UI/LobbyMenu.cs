using UnityEngine;
using UnityEngine.UI;
using Moba.Bootstrap;
using Moba.Core;
using Moba.Network;

namespace Moba.UI
{
    /// <summary>LAN lobby: pick mode, host/join, start arena (host).</summary>
    public sealed class LobbyMenu : MonoBehaviour
    {
        [SerializeField] private InputField _addressField;
        [SerializeField] private InputField _portField;
        [SerializeField] private Dropdown _modeDropdown;
        [SerializeField] private Dropdown _heroDropdown;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _startMatchButton;
        [SerializeField] private Button _disconnectButton;

        private void Awake()
        {
            EnsureSession();
        }

        private void OnEnable()
        {
            if (_hostButton != null) _hostButton.onClick.AddListener(OnHost);
            if (_joinButton != null) _joinButton.onClick.AddListener(OnJoin);
            if (_startMatchButton != null) _startMatchButton.onClick.AddListener(OnStartMatch);
            if (_disconnectButton != null) _disconnectButton.onClick.AddListener(OnDisconnect);
            if (_modeDropdown != null) _modeDropdown.onValueChanged.AddListener(OnModeChanged);
            if (_heroDropdown != null) _heroDropdown.onValueChanged.AddListener(OnHeroChanged);

            var net = MobaNetworkController.Instance;
            if (net != null)
            {
                net.StatusChanged += SetStatus;
            }

            RefreshFieldsFromSession();
            SetStatus("Ready — Host or Join on LAN");
        }

        private void OnDisable()
        {
            if (_hostButton != null) _hostButton.onClick.RemoveListener(OnHost);
            if (_joinButton != null) _joinButton.onClick.RemoveListener(OnJoin);
            if (_startMatchButton != null) _startMatchButton.onClick.RemoveListener(OnStartMatch);
            if (_disconnectButton != null) _disconnectButton.onClick.RemoveListener(OnDisconnect);
            if (_modeDropdown != null) _modeDropdown.onValueChanged.RemoveListener(OnModeChanged);
            if (_heroDropdown != null) _heroDropdown.onValueChanged.RemoveListener(OnHeroChanged);

            var net = MobaNetworkController.Instance;
            if (net != null)
            {
                net.StatusChanged -= SetStatus;
            }
        }

        private static void EnsureSession()
        {
            if (GameSession.Instance != null)
            {
                return;
            }

            var go = new GameObject("GameSession");
            go.AddComponent<GameSession>();
        }

        private void RefreshFieldsFromSession()
        {
            var session = GameSession.Instance;
            if (session == null)
            {
                return;
            }

            if (_addressField != null)
            {
                _addressField.text = session.JoinAddress;
            }

            if (_portField != null)
            {
                _portField.text = session.Port.ToString();
            }

            if (_modeDropdown != null)
            {
                _modeDropdown.value = session.Mode == GameModeKind.ThreeVsThree ? 1 : 0;
            }

            if (_heroDropdown != null)
            {
                EnsureHeroOptions();
                _heroDropdown.value = session.SelectedHero switch
                {
                    HeroId.YingXi => 1,
                    HeroId.JinFa => 2,
                    HeroId.ChuanYun => 3,
                    _ => 0
                };
            }
        }

        private void EnsureHeroOptions()
        {
            if (_heroDropdown == null || _heroDropdown.options.Count >= 4)
            {
                return;
            }

            _heroDropdown.ClearOptions();
            _heroDropdown.AddOptions(new System.Collections.Generic.List<string>(HeroCatalog.DisplayNames()));
        }

        private void ApplyUiToSession()
        {
            var session = GameSession.Instance;
            if (session == null)
            {
                return;
            }

            if (_addressField != null)
            {
                session.JoinAddress = _addressField.text;
            }

            if (_portField != null && ushort.TryParse(_portField.text, out var port))
            {
                session.Port = port;
            }

            if (_modeDropdown != null)
            {
                session.Mode = _modeDropdown.value == 1
                    ? GameModeKind.ThreeVsThree
                    : GameModeKind.OneVsOne;
            }

            if (_heroDropdown != null)
            {
                session.SelectedHero = HeroCatalog.FromDropdownIndex(_heroDropdown.value);
            }
        }

        private void OnModeChanged(int _)
        {
            ApplyUiToSession();
            SetStatus($"Mode: {GameSession.Instance.Mode} (max {GameSession.Instance.MaxPlayers})");
        }

        private void OnHeroChanged(int _)
        {
            ApplyUiToSession();
            SetStatus($"英雄: {HeroCatalog.Get(GameSession.Instance.SelectedHero).DisplayName}");
        }

        private void OnHost()
        {
            ApplyUiToSession();
            var net = MobaNetworkController.Instance;
            if (net == null)
            {
                SetStatus("Missing MobaNetworkController (run Phase0 Setup)");
                return;
            }

            net.StartHost();
        }

        private void OnJoin()
        {
            ApplyUiToSession();
            var net = MobaNetworkController.Instance;
            if (net == null)
            {
                SetStatus("Missing MobaNetworkController (run Phase0 Setup)");
                return;
            }

            net.StartClient();
        }

        private void OnStartMatch()
        {
            var net = MobaNetworkController.Instance;
            if (net == null)
            {
                SetStatus("Missing MobaNetworkController");
                return;
            }

            net.LoadArenaAsHost();
        }

        private void OnDisconnect()
        {
            MobaNetworkController.Instance?.Shutdown();
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }

            Debug.Log($"[Lobby] {message}");
        }
    }
}
