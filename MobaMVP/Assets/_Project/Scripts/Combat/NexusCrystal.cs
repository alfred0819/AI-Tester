using Unity.Netcode;
using UnityEngine;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Nexus crystal: attackable after allied tower falls; destruction ends the match.</summary>
    public sealed class NexusCrystal : NetworkBehaviour, ICombatTarget
    {
        [SerializeField] private TeamId _team = TeamId.Blue;
        [SerializeField] private Renderer _bodyRenderer;

        private readonly CombatStats _stats = new()
        {
            MaxHealth = BuildingRules.CrystalMaxHealth,
            Health = BuildingRules.CrystalMaxHealth,
            Armor = BuildingRules.CrystalArmor
        };

        private readonly NetworkVariable<float> _health = new(
            BuildingRules.CrystalMaxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _alive = new(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _unlocked = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<TeamId> _netTeam = new(
            TeamId.Blue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public TeamId Team => IsSpawned ? _netTeam.Value : _team;
        public bool IsAlive => _alive.Value;
        public bool IsAttackable => IsAlive && _unlocked.Value;
        public bool IsUnlocked => _unlocked.Value;
        public CombatTargetKind Kind => CombatTargetKind.Crystal;
        public Vector3 WorldPosition => transform.position;
        public float Health => _health.Value;
        public float MaxHealth => _stats.MaxHealth;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _netTeam.Value = _team;
                _stats.FullHeal();
                _health.Value = _stats.Health;
                _alive.Value = true;
                RefreshUnlockFromTowers();
            }

            ApplyColor(Team);
            _netTeam.OnValueChanged += OnTeamChanged;
        }

        public override void OnNetworkDespawn()
        {
            _netTeam.OnValueChanged -= OnTeamChanged;
        }

        private void OnTeamChanged(TeamId previous, TeamId next) => ApplyColor(next);

        public void OnAlliedTowerDestroyed()
        {
            if (IsServer)
            {
                _unlocked.Value = true;
            }
        }

        private void RefreshUnlockFromTowers()
        {
            var anyAliveTower = false;
            foreach (var tower in FindObjectsByType<DefenseTower>(FindObjectsSortMode.None))
            {
                if (tower.Team == _team && tower.IsAlive)
                {
                    anyAliveTower = true;
                    break;
                }
            }

            _unlocked.Value = !anyAliveTower;
        }

        public float ReceiveDamage(float power, float ratio, DamageType type, ICombatTarget attacker)
        {
            if (!IsServer || !IsAttackable)
            {
                return 0f;
            }

            var dmg = _stats.ApplyDamage(type, power, ratio);
            _health.Value = _stats.Health;
            if (!_stats.IsAlive)
            {
                _alive.Value = false;
                var winner = MatchWinRules.WinnerWhenCrystalDestroyed(_team);
                MatchResultController.Instance?.EndMatchServer(winner, _team);
                OnDestroyedClientRpc();
            }

            return dmg;
        }

        [ClientRpc]
        private void OnDestroyedClientRpc()
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.material.color = Color.black;
            }

            Debug.Log($"[Crystal] {_team} crystal destroyed");
        }

        private void ApplyColor()
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.material.color = _team == TeamId.Blue
                    ? new Color(0.4f, 0.7f, 1f)
                    : new Color(1f, 0.5f, 0.4f);
            }
        }

        public void SetTeam(TeamId team)
        {
            _team = team;
        }
    }
}
