using Unity.Netcode;
using UnityEngine;
using Moba.Core;
using Moba.Network;

namespace Moba.Combat
{
    /// <summary>Host-authoritative HP, basic attack, shockwave skill, death/respawn.</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class HeroCombat : NetworkBehaviour
    {
        [SerializeField] private float _attackRangeOverride = -1f;
        [SerializeField] private Transform _vfxPoint;
        [SerializeField] private bool _useFixedTeam;
        [SerializeField] private TeamId _fixedTeam = TeamId.Red;

        private readonly HeroCombatState _state = new();
        private readonly NetworkVariable<float> _health = new(
            600f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _maxHealth = new(
            600f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _alive = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _respawnLeft = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _skillCdLeft = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private PlayerAvatar _avatar;

        public float Health => _health.Value;
        public float MaxHealth => _maxHealth.Value;
        public bool IsAlive => _alive.Value;
        public float RespawnRemaining => _respawnLeft.Value;
        public float SkillCooldownRemaining => _skillCdLeft.Value;
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
        }

        public bool CanMoveOnServer => IsServer && _state.CanMove;

        public void RequestBasicAttack()
        {
            if (!IsOwner)
            {
                return;
            }

            BasicAttackServerRpc();
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

            if (!TryFindNearestEnemy(AttackRange, out var target))
            {
                // Whiff still consumes cooldown so spam is limited.
                return;
            }

            var dmg = _state.ResolveBasicAttack(target._state.Stats);
            target.AfterDamaged();
            PlayHitClientRpc(target.NetworkObjectId, dmg, false);
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

            var origin = transform.position;
            foreach (var other in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (other == null || other == this || !other.IsSpawned)
                {
                    continue;
                }

                if (!TeamRules.AreEnemies(Team, other.Team))
                {
                    continue;
                }

                if (!other._state.Stats.IsAlive)
                {
                    continue;
                }

                var delta = other.transform.position - origin;
                delta.y = 0f;
                if (delta.magnitude > Phase1HeroKit.SkillRadius)
                {
                    continue;
                }

                var dmg = _state.ResolveSkillHit(other._state.Stats);
                other.AfterDamaged();
                PlayHitClientRpc(other.NetworkObjectId, dmg, true);
            }
        }

        private void AfterDamaged()
        {
            PushState();
            if (_state.IsDead)
            {
                _state.MarkDead();
                PushState();
                DiedClientRpc();
            }
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
        }

        private bool TryFindNearestEnemy(float range, out HeroCombat target)
        {
            target = null;
            var best = float.MaxValue;
            var self = transform.position;

            foreach (var other in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (other == null || other == this || !other.IsSpawned)
                {
                    continue;
                }

                if (!TeamRules.AreEnemies(Team, other.Team))
                {
                    continue;
                }

                if (!other._alive.Value)
                {
                    continue;
                }

                var d = Vector3.Distance(
                    new Vector3(self.x, 0f, self.z),
                    new Vector3(other.transform.position.x, 0f, other.transform.position.z));
                if (d < best && TargetingRules.IsInRange(d, range))
                {
                    best = d;
                    target = other;
                }
            }

            return target != null;
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
            Debug.Log($"[Combat] shockwave aim={aimDirection}");
        }

        [ClientRpc]
        private void DiedClientRpc()
        {
            Debug.Log($"[Combat] {name} died — respawn in {Phase1HeroKit.BaseRespawnSeconds}s");
            SetVisualDead(true);
        }

        [ClientRpc]
        private void RespawnedClientRpc()
        {
            Debug.Log($"[Combat] {name} respawned");
            SetVisualDead(false);
        }

        private void SetVisualDead(bool dead)
        {
            transform.localScale = dead ? new Vector3(1f, 0.35f, 1f) : Vector3.one;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, Phase1HeroKit.SkillRadius);
        }
    }
}
