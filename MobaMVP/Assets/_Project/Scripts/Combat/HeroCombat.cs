using Unity.Netcode;
using UnityEngine;
using Moba.Core;
using Moba.Network;
using Moba.Bootstrap;

namespace Moba.Combat
{
    /// <summary>Host-authoritative hero: kit, skills, gold, items, status, death/respawn.</summary>
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
        private readonly NetworkVariable<float> _skill1Cd = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _skill2Cd = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _skill3Cd = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _gold = new(
            EconomyRules.StartingGold, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<byte> _heroId = new(
            (byte)HeroId.PanWei, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _stunLeft = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item0 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item1 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item2 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item3 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item4 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item5 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerAvatar _avatar;
        private float _passiveGoldAcc;

        public float Health => _health.Value;
        public float MaxHealth => _maxHealth.Value;
        public bool IsAlive => _alive.Value;
        public bool IsAttackable => IsAlive;
        public float RespawnRemaining => _respawnLeft.Value;
        public float SkillCooldownRemaining => _skill1Cd.Value;
        public float Skill1Cd => _skill1Cd.Value;
        public float Skill2Cd => _skill2Cd.Value;
        public float Skill3Cd => _skill3Cd.Value;
        public int Gold => _gold.Value;
        public HeroId SelectedHero => (HeroId)_heroId.Value;
        public bool IsStunned => _stunLeft.Value > 0f;
        public CombatTargetKind Kind =>
            _useFixedTeam ? CombatTargetKind.Dummy : CombatTargetKind.Hero;
        public Vector3 WorldPosition => transform.position;
        public float AttackRange =>
            _attackRangeOverride > 0f ? _attackRangeOverride : _state.Stats.AttackRange;
        public float EffectiveMoveSpeed => _state.EffectiveMoveSpeed;

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
            if (IsServer && Kind == CombatTargetKind.Dummy)
            {
                _state.ApplyHero(HeroId.PanWei);
                _heroId.Value = (byte)HeroId.PanWei;
                PushState();
            }

            if (IsOwner && Kind == CombatTargetKind.Hero)
            {
                var hero = GameSession.Instance != null
                    ? GameSession.Instance.SelectedHero
                    : HeroId.PanWei;
                SelectHeroServerRpc((byte)hero);
            }
        }

        [ServerRpc]
        private void SelectHeroServerRpc(byte heroByte)
        {
            var hero = (HeroId)heroByte;
            if (hero == HeroId.None)
            {
                hero = HeroId.PanWei;
            }

            _state.ApplyHero(hero);
            _heroId.Value = (byte)_state.HeroId;
            PushState();
            Debug.Log($"[Hero] client {OwnerClientId} selected {_state.HeroId}");
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            _state.Tick(Time.deltaTime);
            SyncCds();
            _stunLeft.Value = _state.Status.StunRemaining;
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

        public float GetSkillPower(SkillDefinition skill) => _state.GetSkillPower(skill);

        public void AddGoldServer(int amount)
        {
            if (!IsServer || amount <= 0)
            {
                return;
            }

            _wallet.Add(amount);
            _gold.Value = _wallet.Gold;
        }

        public void ApplySelfSupport(SkillDefinition skill)
        {
            if (!IsServer || skill == null)
            {
                return;
            }

            if (skill.ShieldAmount > 0f)
            {
                _state.Status.AddShield(skill.ShieldAmount);
            }

            if (skill.MoveSpeedBuffDuration > 0f)
            {
                _state.Status.ApplyHaste(skill.MoveSpeedBuffDuration, skill.MoveSpeedBuffMultiplier);
            }
        }

        public void ApplyCrowdControl(float stun, float slowDuration, float slowMul)
        {
            if (!IsServer)
            {
                return;
            }

            if (stun > 0f)
            {
                _state.Status.ApplyStun(stun);
            }

            if (slowDuration > 0f)
            {
                _state.Status.ApplySlow(slowDuration, slowMul);
            }

            _stunLeft.Value = _state.Status.StunRemaining;
        }

        public float ReceiveDamage(float power, float ratio, DamageType type, ICombatTarget attacker)
        {
            if (!IsServer || !_state.Stats.IsAlive)
            {
                return 0f;
            }

            var raw = DamageFormula.Compute(type, power, ratio, _state.Stats.Armor, _state.Stats.MagicResist);
            var afterShield = _state.Status.AbsorbDamage(raw);
            _state.Stats.Health = Mathf.Max(0f, _state.Stats.Health - afterShield);
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

            return afterShield;
        }

        public void RequestBasicAttack()
        {
            if (IsOwner)
            {
                BasicAttackServerRpc();
            }
        }

        public void RequestSkill(SkillSlot slot, Vector3 aimWorldDirection)
        {
            if (!IsOwner)
            {
                return;
            }

            if (aimWorldDirection.sqrMagnitude < 0.0001f)
            {
                aimWorldDirection = transform.forward;
            }

            SkillServerRpc((byte)slot, aimWorldDirection.normalized);
        }

        /// <summary>Phase1 compat.</summary>
        public void RequestSkill(Vector3 aimWorldDirection)
        {
            RequestSkill(SkillSlot.One, aimWorldDirection);
        }

        public void RequestBuyItem(ItemId itemId)
        {
            if (IsOwner)
            {
                BuyItemServerRpc((byte)itemId);
            }
        }

        public ItemId GetItemSlot(int index)
        {
            return index switch
            {
                0 => (ItemId)_item0.Value,
                1 => (ItemId)_item1.Value,
                2 => (ItemId)_item2.Value,
                3 => (ItemId)_item3.Value,
                4 => (ItemId)_item4.Value,
                5 => (ItemId)_item5.Value,
                _ => ItemId.None
            };
        }

        [ServerRpc]
        private void BuyItemServerRpc(byte itemByte)
        {
            if (Kind != CombatTargetKind.Hero)
            {
                return;
            }

            if (!_state.TryPurchase((ItemId)itemByte, _wallet, out var error))
            {
                Debug.Log($"[Shop] buy failed: {error}");
                return;
            }

            PushItems();
            PushState();
            Debug.Log($"[Shop] bought {(ItemId)itemByte} gold={_wallet.Gold}");
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
        private void SkillServerRpc(byte slotByte, Vector3 aimDirection)
        {
            var slot = (SkillSlot)slotByte;
            if (!_state.TryStartSkill(slot))
            {
                return;
            }

            if (MatchResultController.Instance != null && MatchResultController.Instance.MatchEnded)
            {
                return;
            }

            SyncCds();
            var skill = HeroCatalog.Get(_state.HeroId).GetSkill(slot);
            PlaySkillFxClientRpc(slotByte, aimDirection, skill != null ? skill.Radius : 3f);
            SkillResolver.Execute(this, skill, aimDirection);
            PushState();
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

        private void SyncCds()
        {
            _skill1Cd.Value = _state.Skill1CooldownRemaining;
            _skill2Cd.Value = _state.Skill2CooldownRemaining;
            _skill3Cd.Value = _state.Skill3CooldownRemaining;
        }

        private void PushItems()
        {
            _item0.Value = (int)_state.Equipment.GetSlot(0);
            _item1.Value = (int)_state.Equipment.GetSlot(1);
            _item2.Value = (int)_state.Equipment.GetSlot(2);
            _item3.Value = (int)_state.Equipment.GetSlot(3);
            _item4.Value = (int)_state.Equipment.GetSlot(4);
            _item5.Value = (int)_state.Equipment.GetSlot(5);
        }

        private void PushState()
        {
            _health.Value = _state.Stats.Health;
            _maxHealth.Value = _state.Stats.MaxHealth;
            _alive.Value = _state.Stats.IsAlive;
            _respawnLeft.Value = _state.RespawnRemaining;
            SyncCds();
            _gold.Value = _wallet.Gold;
            _stunLeft.Value = _state.Status.StunRemaining;
            _heroId.Value = (byte)_state.HeroId;
            PushItems();
        }

        [ClientRpc]
        private void PlayHitClientRpc(ulong targetId, float damage, bool fromSkill)
        {
            Debug.Log($"[Combat] hit target={targetId} dmg={damage:F0} skill={fromSkill}");
        }

        [ClientRpc]
        private void PlaySkillFxClientRpc(byte slot, Vector3 aimDirection, float radius)
        {
            var center = _vfxPoint != null ? _vfxPoint.position : transform.position;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"SkillFx_{slot}";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = center + aimDirection.normalized * 0.5f;
            go.transform.localScale = Vector3.one * Mathf.Max(1f, radius * 2f);
            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = slot switch
                {
                    2 => new Color(0.4f, 1f, 0.5f, 0.35f),
                    3 => new Color(1f, 0.5f, 0.2f, 0.35f),
                    _ => new Color(0.4f, 0.8f, 1f, 0.35f)
                };
            }

            Object.Destroy(go, 0.35f);
        }

        [ClientRpc]
        private void DiedClientRpc() => SetVisualDead(true);

        [ClientRpc]
        private void RespawnedClientRpc() => SetVisualDead(false);

        private void SetVisualDead(bool dead)
        {
            transform.localScale = dead ? new Vector3(1f, 0.35f, 1f) : Vector3.one;
        }
    }
}
