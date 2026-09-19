using Unity.Netcode;
using UnityEngine;
using Moba.Core;
using Moba.Network;

namespace Moba.Combat
{
    /// <summary>Host-authoritative hero: HP, attack, skill, gold, death/respawn.</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class HeroCombat : NetworkBehaviour, ICombatTarget
    {
        [SerializeField] private float _attackRangeOverride = -1f;
        [SerializeField] private Transform _vfxPoint;
        [SerializeField] private bool _useFixedTeam;
        [SerializeField] private TeamId _fixedTeam = TeamId.Red;

        private readonly HeroCombatState _state = new();
        private readonly PlayerWallet _wallet = new(EconomyRules.StartingGold);

        private readonly NetworkVariable<float> _health = new(
            600f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _maxHealth = new(
            600f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _alive = new(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _respawnLeft = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _skillCdLeft = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _gold = new(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerAvatar _avatar;
        private float _passiveGoldAcc;

        public float Health => _health.Value;
        public float MaxHealth => _maxHealth.Value;
        public bool IsAlive => _alive.Value;
        public bool IsAttackable => IsAlive;
        public float RespawnRemaining => _respawnLeft.Value;
        public float SkillCooldownRemaining => _skillCdLeft.Value;
        public int Gold => _gold.Value;
        public CombatTargetKind Kind =>
            _useFixedTeam ? CombatTargetKind.Dummy : CombatTargetKind.Hero;
        public Vector3 WorldPosition => transform.position;
        public float AttackRange =>
            _attackRangeOverride > 0f ? _attackRangeOverride : _state.Stats.AttackRange;

        public TeamId Team
        {
            get
            {
                if (_useFixedTeam)
                {
                    return _fixedTeam;
                }

                return _avatar != null ? _avatar.Team : TeamId.None;
            }
        }

        public override void OnNetworkSpawn()
        {
            _avatar = GetComponent<PlayerAvatar>();
            if (IsServer)
            {
                _state.Stats.FullHeal();
                PushState();
            }
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            _state.Tick(Time.deltaTime);
            _skillCdLeft.Value = _state.SkillCooldownRemaining;
            _respawnLeft.Value = _state.RespawnRemaining;

            if (_state.ReadyToRespawn)
            {
                PerformRespawn();
            }

            if (_alive.Value && Kind == CombatTargetKind.Hero)
            {
                _passiveGoldAcc += Time.deltaTime * EconomyRules.PassiveGoldPerSecond;
                if (_passiveGoldAcc >= 1f)
                {
                    var add = Mathf.FloorToInt(_passiveGoldAcc);
                    _passiveGoldAcc -= add;
                    AddGoldServer(add);
                }
            }
        }

        public bool CanMoveOnServer => IsServer && _state.CanMove;

        public void AddGoldServer(int amount)
        {
            if (!IsServer || amount <= 0)
            {
                return;
            }

            _wallet.Add(amount);
            _gold.Value = _wallet.Gold;
        }

        public float ReceiveDamage(float power, float ratio, DamageType type, ICombatTarget attacker)
        {
            if (!IsServer || !_state.Stats.IsAlive)
            {
                return 0f;
            }

            var dmg = _state.Stats.ApplyDamage(type, power, ratio);
            PushState();
            if (_state.IsDead)
            {
                _state.MarkDead();
                PushState();
                if (attacker is HeroCombat killer && killer.Kind == CombatTargetKind.Hero)
                {
                    killer.AddGoldServer(EconomyRules.BountyFor(Kind));
                }

                DiedClientRpc();
            }

            return dmg;
        }

        public void RequestBasicAttack()
        {
            if (IsOwner)
            {
                BasicAttackServerRpc();
            }
        }

        public void RequestSkill(Vector3 aimWorldDirection)
        {
            if (!IsOwner)
            {
                return;
            }

            if (aimWorldDirection.sqrMagnitude < 0.0001f)
            {
                aimWorldDirection = transform.forward;
            }

            SkillServerRpc(aimWorldDirection.normalized);
        }

        [ServerRpc]
        private void BasicAttackServerRpc()
        {
            if (!_state.TryStartBasicAttack())
            {
                return;
            }

            if (MatchResultController.Instance != null && MatchResultController.Instance.MatchEnded)
            {
                return;
            }

            if (!CombatTargeting.TryFindNearestEnemy(this, AttackRange, out var target))
            {
                return;
            }

            var dmg = target.ReceiveDamage(
                _state.Stats.Attack,
                Phase1HeroKit.BasicAttackRatio,
                DamageType.Physical,
                this);
            PlayHitClientRpc(target.NetworkObject.NetworkObjectId, dmg, false);
        }

        [ServerRpc]
        private void SkillServerRpc(Vector3 aimDirection)
        {
            if (!_state.TryStartSkill())
            {
                return;
            }

            _skillCdLeft.Value = _state.SkillCooldownRemaining;
            PlaySkillFxClientRpc(aimDirection);

            var power = _state.Stats.AbilityPower > 0f ? _state.Stats.AbilityPower : _state.Stats.Attack;
            CombatTargeting.ForEachEnemyInRadius(this, Phase1HeroKit.SkillRadius, other =>
            {
                var dmg = other.ReceiveDamage(
                    power,
                    Phase1HeroKit.SkillDamageRatio,
                    DamageType.Magical,
                    this);
                PlayHitClientRpc(other.NetworkObject.NetworkObjectId, dmg, true);
            });
        }

        private void PerformRespawn()
        {
            _state.Respawn();
            PushState();

            var teamIndex = Team == TeamId.Blue ? 0 : 1;
            var marker = GameObject.Find($"SpawnPoint_Team{teamIndex}_0");
            if (marker != null)
            {
                transform.position = marker.transform.position;
            }

            RespawnedClientRpc();
        }

        private void PushState()
        {
            _health.Value = _state.Stats.Health;
            _maxHealth.Value = _state.Stats.MaxHealth;
            _alive.Value = _state.Stats.IsAlive;
            _respawnLeft.Value = _state.RespawnRemaining;
            _skillCdLeft.Value = _state.SkillCooldownRemaining;
            _gold.Value = _wallet.Gold;
        }

        [ClientRpc]
        private void PlayHitClientRpc(ulong targetId, float damage, bool fromSkill)
        {
            Debug.Log($"[Combat] hit target={targetId} dmg={damage:F0} skill={fromSkill}");
        }

        [ClientRpc]
        private void PlaySkillFxClientRpc(Vector3 aimDirection)
        {
            var center = _vfxPoint != null ? _vfxPoint.position : transform.position;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ShockwaveFx";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = center;
            go.transform.localScale = Vector3.one * (Phase1HeroKit.SkillRadius * 2f);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.4f, 0.8f, 1f, 0.35f);
            }

            Object.Destroy(go, 0.35f);
        }

        [ClientRpc]
        private void DiedClientRpc()
        {
            SetVisualDead(true);
        }

        [ClientRpc]
        private void RespawnedClientRpc()
        {
            SetVisualDead(false);
        }

        private void SetVisualDead(bool dead)
        {
            transform.localScale = dead ? new Vector3(1f, 0.35f, 1f) : Vector3.one;
        }
    }
}
