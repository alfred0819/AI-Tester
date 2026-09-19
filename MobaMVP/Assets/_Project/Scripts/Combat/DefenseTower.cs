using Unity.Netcode;
using UnityEngine;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Lane tower: attacks nearest enemy in range; gates crystal when alive.</summary>
    public sealed class DefenseTower : NetworkBehaviour, ICombatTarget
    {
        [SerializeField] private TeamId _team = TeamId.Blue;
        [SerializeField] private Renderer _bodyRenderer;

        private readonly CombatStats _stats = new()
        {
            MaxHealth = BuildingRules.TowerMaxHealth,
            Health = BuildingRules.TowerMaxHealth,
            Attack = BuildingRules.TowerAttack,
            Armor = BuildingRules.TowerArmor,
            AttackRange = BuildingRules.TowerAttackRange,
            AttackInterval = BuildingRules.TowerAttackInterval
        };

        private readonly NetworkVariable<float> _health = new(
            BuildingRules.TowerMaxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _alive = new(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<TeamId> _netTeam = new(
            TeamId.Blue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private float _attackCd;

        public TeamId Team => IsSpawned ? _netTeam.Value : _team;
        public bool IsAlive => _alive.Value;
        public bool IsAttackable => IsAlive;
        public CombatTargetKind Kind => CombatTargetKind.Tower;
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
            }

            ApplyColor(Team);
            _netTeam.OnValueChanged += OnTeamChanged;
        }

        public override void OnNetworkDespawn()
        {
            _netTeam.OnValueChanged -= OnTeamChanged;
        }

        private void OnTeamChanged(TeamId previous, TeamId next) => ApplyColor(next);

        private void Update()
        {
            if (!IsServer || !_alive.Value)
            {
                return;
            }

            if (MatchResultController.Instance != null && MatchResultController.Instance.MatchEnded)
            {
                return;
            }

            _attackCd = Mathf.Max(0f, _attackCd - Time.deltaTime);
            if (_attackCd > 0f)
            {
                return;
            }

            if (!CombatTargeting.TryFindNearestEnemy(
                    this, BuildingRules.TowerAttackRange, out var target, includeBuildings: false))
            {
                return;
            }

            _attackCd = BuildingRules.TowerAttackInterval;
            target.ReceiveDamage(_stats.Attack, 1f, DamageType.Physical, this);
        }

        public float ReceiveDamage(float power, float ratio, DamageType type, ICombatTarget attacker)
        {
            if (!IsServer || !_stats.IsAlive)
            {
                return 0f;
            }

            var dmg = _stats.ApplyDamage(type, power, ratio);
            _health.Value = _stats.Health;
            if (!_stats.IsAlive)
            {
                _alive.Value = false;
                if (attacker is HeroCombat hero && hero.Kind == CombatTargetKind.Hero)
                {
                    hero.AddGoldServer(EconomyRules.BountyFor(CombatTargetKind.Tower));
                }

                OnDestroyedClientRpc();
                NotifyCrystalUnlocked();
            }

            return dmg;
        }

        private void NotifyCrystalUnlocked()
        {
            foreach (var crystal in FindObjectsByType<NexusCrystal>(FindObjectsSortMode.None))
            {
                if (crystal.Team == Team)
                {
                    crystal.OnAlliedTowerDestroyed();
                }
            }
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
                _bodyRenderer.material.color = Color.gray;
            }

            transform.localScale = new Vector3(1f, 0.4f, 1f);
            Debug.Log($"[Tower] {_team} tower destroyed");
        }

        private void ApplyColor(TeamId team)
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.material.color = team == TeamId.Blue
                    ? new Color(0.2f, 0.45f, 1f)
                    : new Color(1f, 0.3f, 0.25f);
            }
        }

        public void SetTeam(TeamId team)
        {
            _team = team;
            if (IsServer && IsSpawned)
            {
                _netTeam.Value = team;
            }
        }
    }
}
