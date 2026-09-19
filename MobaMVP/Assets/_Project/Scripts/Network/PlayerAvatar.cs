using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Moba.Core;
using Moba.Bootstrap;
using Moba.Combat;

namespace Moba.Network
{
    /// <summary>Networked greybox avatar: host-authoritative move, team color.</summary>
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
            if (!IsOwner)
            {
                return;
            }

            if (_combat != null && !_combat.IsAlive)
            {
                return;
            }

            var move = ReadMoveInput();
            if (move.sqrMagnitude > 0.0001f)
            {
                SubmitMoveServerRpc(move);
            }
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

            if (_combat != null && !_combat.CanMoveOnServer)
            {
                return;
            }

            var cmd = new PlayerCommand
            {
                MoveX = direction.x,
                MoveY = direction.z
            }.ClampedMove();

            var speed = _moveSpeed;
            transform.position += new Vector3(cmd.MoveX, 0f, cmd.MoveY) * speed * Time.deltaTime;

            if (cmd.MoveX * cmd.MoveX + cmd.MoveY * cmd.MoveY > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(cmd.MoveX, 0f, cmd.MoveY), Vector3.up);
            }
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
