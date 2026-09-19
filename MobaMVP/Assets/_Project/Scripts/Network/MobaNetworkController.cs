using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Moba.Bootstrap;
using Moba.Core;

namespace Moba.Network
{
    /// <summary>Thin wrapper around NGO NetworkManager for LAN host/client.</summary>
    public sealed class MobaNetworkController : MonoBehaviour
    {
        public static MobaNetworkController Instance { get; private set; }

        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private string _arenaSceneName = SceneNames.Arena1Lane;

        public NetworkManager NetworkManager => _networkManager;
        public bool IsConnected => _networkManager != null && _networkManager.IsListening;
        public bool IsHost => _networkManager != null && _networkManager.IsHost;

        public event Action Connected;
        public event Action Disconnected;
        public event Action<string> StatusChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_networkManager == null)
            {
                _networkManager = GetComponent<NetworkManager>();
            }
        }

        private void OnEnable()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            _networkManager.ConnectionApprovalCallback = ApprovalCheck;
        }

        private void OnDisable()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.ConnectionApprovalCallback = null;
        }

        public bool StartHost()
        {
            ApplyTransport(listenServer: true);
            ConfigureSpawn();
            var ok = _networkManager.StartHost();
            StatusChanged?.Invoke(ok ? "Host started" : "Host failed");
            if (ok)
            {
                Connected?.Invoke();
            }

            return ok;
        }

        public bool StartClient()
        {
            ApplyTransport(listenServer: false);
            ConfigureSpawn();
            var ok = _networkManager.StartClient();
            StatusChanged?.Invoke(ok ? "Connecting..." : "Client start failed");
            if (ok)
            {
                Connected?.Invoke();
            }

            return ok;
        }

        public void Shutdown()
        {
            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }

            StatusChanged?.Invoke("Disconnected");
            Disconnected?.Invoke();
        }

        public void LoadArenaAsHost()
        {
            if (_networkManager == null || !_networkManager.IsServer)
            {
                StatusChanged?.Invoke("Only host can start match");
                return;
            }

            StatusChanged?.Invoke($"Loading {_arenaSceneName}");
            _networkManager.SceneManager.LoadScene(_arenaSceneName, LoadSceneMode.Single);
        }

        private void ApplyTransport(bool listenServer)
        {
            var session = GameSession.Instance;
            var address = session != null ? session.JoinAddress : NetworkDefaults.DefaultAddress;
            var port = session != null ? session.Port : NetworkDefaults.DefaultPort;

            var transport = _networkManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = _networkManager.gameObject.AddComponent<UnityTransport>();
            }

            if (listenServer)
            {
                transport.SetConnectionData("0.0.0.0", port);
            }
            else
            {
                transport.SetConnectionData(address, port);
            }

            _networkManager.NetworkConfig.NetworkTransport = transport;
            _networkManager.NetworkConfig.ConnectionApproval = true;
            _networkManager.NetworkConfig.TickRate = (uint)NetworkDefaults.SimulationTickRate;
        }

        private void ConfigureSpawn()
        {
            // Player prefab is assigned on NetworkManager in the Phase0 setup menu.
            _networkManager.NetworkConfig.PlayerPrefab = _networkManager.NetworkConfig.PlayerPrefab;
        }

        private void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var max = GameSession.Instance != null
                ? GameSession.Instance.MaxPlayers
                : GameModeRules.MaxPlayers(GameModeKind.OneVsOne);

            var connected = _networkManager.ConnectedClientsIds.Count;
            // Host counts as already connected when approving additional clients.
            var allowed = connected < max;
            response.Approved = allowed;
            response.CreatePlayerObject = allowed;
            response.Pending = false;
            response.Reason = allowed ? string.Empty : "Room full";
        }

        private void OnClientConnected(ulong clientId)
        {
            StatusChanged?.Invoke($"Client connected: {clientId} (players={_networkManager.ConnectedClientsIds.Count})");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            StatusChanged?.Invoke($"Client disconnected: {clientId}");
            if (clientId == _networkManager.LocalClientId)
            {
                Disconnected?.Invoke();
            }
        }
    }
}
