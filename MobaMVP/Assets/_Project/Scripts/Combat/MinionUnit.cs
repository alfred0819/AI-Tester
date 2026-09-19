using Unity.Netcode;
using UnityEngine;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Lane minion: walk waypoints, aggro/attack, bounty on last hit.</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class MinionUnit : NetworkBehaviour, ICombatTarget
    {
        [SerializeField] private TeamId _team = TeamId.Blue;
        [SerializeField] private Renderer _bodyRenderer;

        private readonly CombatStats _stats = new()
        {
            MaxHealth = LaneWaveRules.MinionMaxHealth,
            Health = LaneWaveRules.MinionMaxHealth,
            Attack = LaneWaveRules.MinionAttack,
            Armor = LaneWaveRules.MinionArmor,
            MoveSpeed = LaneWaveRules.MinionMoveSpeed,
            AttackRange = LaneWaveRules.MinionAttackRange,
            AttackInterval = LaneWaveRules.MinionAttackInterval
        };

        private readonly NetworkVariable<float> _health = new(
            LaneWaveRules.MinionMaxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _alive = new(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<TeamId> _netTeam = new(
            TeamId.Blue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Vector3[] _waypoints;
        private int _waypointIndex;
        private float _attackCd;
        private ICombatTarget _focus;

        public TeamId Team => IsSpawned ? _netTeam.Value : _team;
        public bool IsAlive => _alive.Value;
        public bool IsAttackable => IsAlive;
        public CombatTargetKind Kind => CombatTargetKind.Minion;
        public Vector3 WorldPosition => transform.position;
        public float Health => _health.Value;
        public float MaxHealth => _stats.MaxHealth;

        public void Configure(TeamId team, Vector3[] waypoints)
        {
            _team = team;
            _waypoints = waypoints;
            _waypointIndex = 0;
        }

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
            _netTeam.OnValueChanged += (_, next) => ApplyColor(next);
        }

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

            if (_focus != null && (!_focus.IsAlive || !_focus.IsAttackable))
            {
                _focus = null;
            }

            if (_focus == null)
            {
                CombatTargeting.TryFindNearestEnemy(
                    this, LaneWaveRules.MinionAggroRange, out _focus, includeBuildings: true);
            }

            if (_focus != null)
            {
                var dist = CombatTargeting.HorizontalDistance(WorldPosition, _focus.WorldPosition);
                if (dist <= _stats.AttackRange)
                {
                    TryAttack(_focus);
                    return;
                }

                MoveToward(_focus.WorldPosition);
                return;
            }

            if (_waypoints != null && _waypointIndex < _waypoints.Length)
            {
                var target = _waypoints[_waypointIndex];
                if (CombatTargeting.HorizontalDistance(WorldPosition, target) < 0.6f)
                {
                    _waypointIndex++;
                }
                else
                {
                    MoveToward(target);
                }
            }
        }

        private void TryAttack(ICombatTarget target)
        {
            if (_attackCd > 0f)
            {
                return;
            }

            _attackCd = _stats.AttackInterval;
            target.ReceiveDamage(_stats.Attack, 1f, DamageType.Physical, this);
            Face(target.WorldPosition);
        }

        private void MoveToward(Vector3 world)
        {
            var pos = transform.position;
            var next = Vector3.MoveTowards(
                pos,
                new Vector3(world.x, pos.y, world.z),
                _stats.MoveSpeed * Time.deltaTime);
            transform.position = next;
            Face(world);
        }

        private void Face(Vector3 world)
        {
            var dir = world - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
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
                    hero.AddGoldServer(EconomyRules.BountyFor(CombatTargetKind.Minion));
                }

                DespawnSoon();
            }

            return dmg;
        }

        private void DespawnSoon()
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
        }

        private void ApplyColor(TeamId team)
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (_bodyRenderer == null)
            {
                return;
            }

            _bodyRenderer.material.color = team == TeamId.Blue
                ? new Color(0.35f, 0.55f, 1f)
                : new Color(1f, 0.4f, 0.35f);
        }
    }
}
