using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Moba.Core;
using Moba.Bootstrap;
using Moba.Combat;

namespace Moba.Network
{
    /// <summary>Networked greybox avatar: host-authoritative move (server-tick integrated), team color.</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerAvatar : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 3.5f;
        [SerializeField] private Renderer _bodyRenderer;

        private readonly NetworkVariable<TeamId> _team = new(
            TeamId.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private HeroCombat _combat;
        private Vector3 _serverMoveInput;

        public TeamId Team => _team.Value;

        public override void OnNetworkSpawn()
        {
            _combat = GetComponent<HeroCombat>();
            if (IsServer)
            {
                AssignTeamOnServer();
            }

            ApplyTeamColor(_team.Value);
            _team.OnValueChanged += OnTeamChanged;

            if (IsOwner)
            {
                name = $"PlayerAvatar_Owner_{OwnerClientId}";
            }
        }

        public override void OnNetworkDespawn()
        {
            _team.OnValueChanged -= OnTeamChanged;
        }

        private void Update()
        {
            if (IsOwner)
            {
                if (_combat != null && !_combat.IsAlive)
                {
                    _serverMoveInput = Vector3.zero;
                }
                else
                {
                    SubmitMoveServerRpc(ReadMoveInput());
                }
            }

            if (!IsServer)
            {
                return;
            }

            ApplyServerMove(Time.deltaTime);
        }

        private static Vector3 ReadMoveInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector3.zero;
            }

            var x = 0f;
            var z = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) z -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) z += 1f;

            var v = new Vector3(x, 0f, z);
            if (v.sqrMagnitude > 1f)
            {
                v.Normalize();
            }

            return v;
        }

        [ServerRpc]
        private void SubmitMoveServerRpc(Vector3 direction)
        {
            if (!IsSpawned)
            {
                return;
            }

            var cmd = new PlayerCommand
            {
                MoveX = direction.x,
                MoveY = direction.z
            }.ClampedMove();
            _serverMoveInput = new Vector3(cmd.MoveX, 0f, cmd.MoveY);
        }

        private void ApplyServerMove(float deltaTime)
        {
            if (_combat != null && !_combat.CanMoveOnServer)
            {
                return;
            }

            if (!HeroCombat.IsArenaSceneActive)
            {
                return;
            }

            if (_serverMoveInput.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _combat?.NotifyMovedServer();

            var speed = _combat != null ? _combat.EffectiveMoveSpeed : _moveSpeed;
            if (speed <= 0.01f)
            {
                return;
            }

            transform.position += _serverMoveInput * speed * deltaTime;
            transform.rotation = Quaternion.LookRotation(_serverMoveInput, Vector3.up);
        }

        private void AssignTeamOnServer()
        {
            var mode = GameSession.Instance != null
                ? GameSession.Instance.Mode
                : GameModeKind.OneVsOne;

            var slot = 0;
            foreach (var id in NetworkManager.ConnectedClientsIds)
            {
                if (id == OwnerClientId)
                {
                    break;
                }

                slot++;
            }

            _team.Value = TeamRules.AssignTeam(slot, mode);
            _combat?.SetFountainSlot(BotBrain.SpawnIndexInTeam(slot, mode));
            _combat?.TryRegisterHumanWithMatch();
        }

        private void OnTeamChanged(TeamId previous, TeamId next)
        {
            ApplyTeamColor(next);
        }

        private void ApplyTeamColor(TeamId team)
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (_bodyRenderer == null)
            {
                return;
            }

            var color = team switch
            {
                TeamId.Blue => new Color(0.2f, 0.45f, 1f),
                TeamId.Red => new Color(1f, 0.3f, 0.25f),
                _ => Color.gray
            };

            _bodyRenderer.material.color = color;
        }
    }
}
