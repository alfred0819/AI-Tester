using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using Moba.Core;
using Moba.Network;
using Moba.Bootstrap;

namespace Moba.Combat
{
    /// <summary>Host-authoritative hero: kit, skills, gold, items, status, death/respawn, KDA, recall.</summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class HeroCombat : NetworkBehaviour, ICombatTarget
    {
        [SerializeField] private float _attackRangeOverride = -1f;
        [SerializeField] private Transform _vfxPoint;
        [SerializeField] private bool _useFixedTeam;
        [SerializeField] private bool _isBot;
        [SerializeField] private TeamId _fixedTeam = TeamId.Red;
        [SerializeField] private int _fountainSlot;

        private readonly HeroCombatState _state = new();
        private readonly PlayerWallet _wallet = new(EconomyRules.StartingGold);
        private readonly PlayerCombatRecord _record = new();
        private readonly AssistTracker _assists = new();

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
        private readonly NetworkVariable<TeamId> _netTeam = new(
            TeamId.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> _netIsBot = new(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item0 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item1 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item2 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item3 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item4 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _item5 = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _kills = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _deaths = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _assistsNet = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _recallLeft = new(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerAvatar _avatar;
        private NetworkTransform _networkTransform;
        private float _passiveGoldAcc;
        private float _matchTime;

        public static bool IsArenaSceneActive =>
            SceneManager.GetActiveScene().name == SceneNames.Arena1Lane;

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
        public int Kills => _kills.Value;
        public int Deaths => _deaths.Value;
        public int Assists => _assistsNet.Value;
        public float RecallRemaining => _recallLeft.Value;
        public bool IsRecalling => _recallLeft.Value > 0f;
        public HeroId SelectedHero => (HeroId)_heroId.Value;
        public bool IsStunned => _stunLeft.Value > 0f;
        public int FountainSlot => _fountainSlot;
        public CombatTargetKind Kind
        {
            get
            {
                if (IsBotUnit)
                {
                    return CombatTargetKind.Hero;
                }

                return _useFixedTeam ? CombatTargetKind.Dummy : CombatTargetKind.Hero;
            }
        }

        public bool IsBotUnit => _isBot || _netIsBot.Value;
        public Vector3 WorldPosition => transform.position;
        public float AttackRange =>
            _attackRangeOverride > 0f ? _attackRangeOverride : _state.Stats.AttackRange;
        public float EffectiveMoveSpeed => _state.EffectiveMoveSpeed;

        public TeamId Team
        {
            get
            {
                if (_avatar != null)
                {
                    return _avatar.Team;
                }

                if (IsSpawned && _netTeam.Value != TeamId.None)
                {
                    return _netTeam.Value;
                }

                if (_useFixedTeam || _isBot)
                {
                    return _fixedTeam;
                }

                return TeamId.None;
            }
        }

        public ScoreboardEntry ToScoreboardEntry()
        {
            var name = HeroCatalog.Get(SelectedHero).DisplayName;
            return new ScoreboardEntry(name, Team, Kills, Deaths, Assists, Gold, IsBotUnit);
        }

        public void SetFountainSlot(int slotInTeam)
        {
            _fountainSlot = Mathf.Clamp(slotInTeam, 0, 2);
        }

        public void TryRegisterHumanWithMatch()
        {
            if (!IsServer || IsBotUnit || Kind != CombatTargetKind.Hero)
            {
                return;
            }

            if (Team == TeamId.None)
            {
                return;
            }

            MatchResultController.Instance?.RegisterHumanClient(OwnerClientId, Team);
        }

        public override void OnNetworkSpawn()
        {
            _avatar = GetComponent<PlayerAvatar>();
            _networkTransform = GetComponent<NetworkTransform>();
            if (IsServer && Kind == CombatTargetKind.Dummy)
            {
                _netTeam.Value = _fixedTeam;
                _state.ApplyHero(HeroId.PanWei);
                _heroId.Value = (byte)HeroId.PanWei;
                PushState();
            }

            if (IsServer && IsBotUnit)
            {
                _netIsBot.Value = true;
                _netTeam.Value = _fixedTeam;
                _heroId.Value = (byte)_state.HeroId;
                PushState();
            }

            if (IsServer)
            {
                TryRegisterHumanWithMatch();
            }

            if (IsOwner && Kind == CombatTargetKind.Hero && !IsBotUnit)
            {
                var hero = GameSession.Instance != null
                    ? GameSession.Instance.SelectedHero
                    : HeroId.PanWei;
                SelectHeroServerRpc((byte)hero);
            }
        }

        /// <summary>Call on server before Spawn for fill bots.</summary>
        public void ConfigureBot(TeamId team, HeroId hero)
        {
            _isBot = true;
            _useFixedTeam = true;
            _fixedTeam = team;
            _state.ApplyHero(hero == HeroId.None ? HeroId.PanWei : hero);
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
            TryRegisterHumanWithMatch();
            Debug.Log($"[Hero] client {OwnerClientId} selected {_state.HeroId}");
        }

        public void ServerTryBasicAttack()
        {
            if (!IsServer)
            {
                return;
            }

            ExecuteBasicAttack();
        }

        public void ServerTrySkill(SkillSlot slot, Vector3 aimDirection)
        {
            if (!IsServer)
            {
                return;
            }

            if (aimDirection.sqrMagnitude < 0.0001f)
            {
                aimDirection = transform.forward;
            }

            ExecuteSkill(slot, aimDirection.normalized);
        }

        public void ServerMove(Vector3 direction, float deltaTime)
        {
            if (!IsServer || !_state.CanMove || !IsArenaSceneActive)
            {
                return;
            }

            var cmd = new PlayerCommand { MoveX = direction.x, MoveY = direction.z }.ClampedMove();
            if (cmd.MoveX * cmd.MoveX + cmd.MoveY * cmd.MoveY > 0.0001f)
            {
                NotifyMovedServer();
            }

            var speed = _state.EffectiveMoveSpeed;
            if (speed <= 0.01f)
            {
                return;
            }

            transform.position += new Vector3(cmd.MoveX, 0f, cmd.MoveY) * speed * deltaTime;
            if (cmd.MoveX * cmd.MoveX + cmd.MoveY * cmd.MoveY > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(cmd.MoveX, 0f, cmd.MoveY), Vector3.up);
            }
        }

        public void NotifyMovedServer()
        {
            if (!IsServer)
            {
                return;
            }

            if (_state.IsRecalling)
            {
                _state.CancelRecall();
                _recallLeft.Value = 0f;
            }
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            if (!IsArenaSceneActive)
            {
                return;
            }

            TryRegisterHumanWithMatch();

            _matchTime += Time.deltaTime;
            _state.Tick(Time.deltaTime);
            SyncCds();
            _stunLeft.Value = _state.Status.StunRemaining;
            _respawnLeft.Value = _state.RespawnRemaining;

            if (_state.IsRecalling)
            {
                if (_state.TickRecall(Time.deltaTime))
                {
                    CompleteRecallServer();
                }
                else
                {
                    _recallLeft.Value = _state.Recall.Remaining;
                }
            }
            else
            {
                _recallLeft.Value = 0f;
            }

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
                _state.CancelRecall();
                _recallLeft.Value = 0f;
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
            // Any landed hit (including fully shielded) cancels recall.
            if (raw > 0f && _state.IsRecalling)
            {
                _state.CancelRecall();
                _recallLeft.Value = 0f;
            }

            var afterShield = _state.Status.AbsorbDamage(raw);
            _state.Stats.Health = Mathf.Max(0f, _state.Stats.Health - afterShield);

            if (attacker is HeroCombat heroAttacker &&
                heroAttacker.Kind == CombatTargetKind.Hero &&
                heroAttacker.NetworkObject != null)
            {
                _assists.RecordHit(heroAttacker.NetworkObject.NetworkObjectId, _matchTime);
            }

            PushState();
            if (_state.IsDead)
            {
                _state.MarkDead();
                _record.RegisterDeath();
                _deaths.Value = _record.Deaths;
                PushState();
                if (attacker is HeroCombat killer && killer.Kind == CombatTargetKind.Hero)
                {
                    killer.RegisterKillServer();
                    killer.AddGoldServer(EconomyRules.BountyFor(Kind));
                    CreditAssistsServer(killer);
                }

                _assists.Clear();
                DiedClientRpc();
            }

            return afterShield;
        }

        private void RegisterKillServer()
        {
            _record.RegisterKill();
            _kills.Value = _record.Kills;
        }

        private void RegisterAssistServer()
        {
            _record.RegisterAssist();
            _assistsNet.Value = _record.Assists;
            AddGoldServer(EconomyRules.GoldPerAssist);
        }

        private void CreditAssistsServer(HeroCombat killer)
        {
            var killerId = killer.NetworkObject != null ? killer.NetworkObject.NetworkObjectId : 0UL;
            var ids = _assists.CollectAssists(killerId, _matchTime);
            if (ids.Count == 0)
            {
                return;
            }

            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (!combat.IsSpawned || combat.NetworkObject == null)
                {
                    continue;
                }

                if (ids.Contains(combat.NetworkObject.NetworkObjectId))
                {
                    combat.RegisterAssistServer();
                }
            }
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

        public void RequestRecall()
        {
            if (IsOwner)
            {
                RecallServerRpc();
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
            if (Kind != CombatTargetKind.Hero || !IsArenaSceneActive)
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
        private void RecallServerRpc()
        {
            if (Kind != CombatTargetKind.Hero || !IsArenaSceneActive || !_state.TryStartRecall())
            {
                return;
            }

            _recallLeft.Value = _state.Recall.Remaining;
            Debug.Log($"[Recall] client {OwnerClientId} channeling");
        }

        [ServerRpc]
        private void BasicAttackServerRpc() => ExecuteBasicAttack();

        [ServerRpc]
        private void SkillServerRpc(byte slotByte, Vector3 aimDirection) =>
            ExecuteSkill((SkillSlot)slotByte, aimDirection);

        private void ExecuteBasicAttack()
        {
            if (!IsArenaSceneActive)
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

            if (!_state.TryStartBasicAttack())
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

        private void ExecuteSkill(SkillSlot slot, Vector3 aimDirection)
        {
            if (!IsArenaSceneActive)
            {
                return;
            }

            if (MatchResultController.Instance != null && MatchResultController.Instance.MatchEnded)
            {
                return;
            }

            if (!_state.TryStartSkill(slot))
            {
                return;
            }

            SyncCds();
            var skill = HeroCatalog.Get(_state.HeroId).GetSkill(slot);
            PlaySkillFxClientRpc((byte)slot, aimDirection, skill != null ? skill.Radius : 3f);
            SkillResolver.Execute(this, skill, aimDirection);
            PushState();
        }

        private void CompleteRecallServer()
        {
            _recallLeft.Value = 0f;
            TeleportToFountainServer();
            RecallDoneClientRpc();
        }

        private void TeleportToFountainServer()
        {
            var teamIndex = Team == TeamId.Blue ? 0 : 1;
            var slot = ResolveFountainSlot();
            var marker = FindSpawnMarker(teamIndex, slot);
            if (marker == null)
            {
                return;
            }

            ApplyTeleport(marker.transform.position);
        }

        private int ResolveFountainSlot()
        {
            if (IsBotUnit)
            {
                var bot = GetComponent<BotController>();
                if (bot != null)
                {
                    return bot.SpawnSlotInTeam;
                }
            }

            return _fountainSlot;
        }

        private void ApplyTeleport(Vector3 position)
        {
            if (_networkTransform == null)
            {
                _networkTransform = GetComponent<NetworkTransform>();
            }

            if (_networkTransform != null)
            {
                _networkTransform.Teleport(position, transform.rotation, transform.localScale);
            }
            else
            {
                transform.position = position;
            }
        }

        private void PerformRespawn()
        {
            _state.Respawn();
            PushState();
            TeleportToFountainServer();
            RespawnedClientRpc();
        }

        public static GameObject FindSpawnMarker(int teamIndex, int slotInTeam)
        {
            var exact = GameObject.Find($"SpawnPoint_Team{teamIndex}_{slotInTeam}");
            if (exact != null)
            {
                return exact;
            }

            return GameObject.Find($"SpawnPoint_Team{teamIndex}_0");
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
            _kills.Value = _record.Kills;
            _deaths.Value = _record.Deaths;
            _assistsNet.Value = _record.Assists;
            _recallLeft.Value = _state.Recall.Remaining;
            PushItems();
        }

        [ClientRpc]
        private void PlayHitClientRpc(ulong targetId, float damage, bool fromSkill)
        {
            Feedback.CombatFeedbackService.Instance?.OnHit(targetId, damage, fromSkill);
        }

        [ClientRpc]
        private void PlaySkillFxClientRpc(byte slot, Vector3 aimDirection, float radius)
        {
            Feedback.CombatFeedbackService.Instance?.OnSkillCast(slot);
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

        [ClientRpc]
        private void RecallDoneClientRpc()
        {
            // Soft feedback hook for local VFX if needed later.
        }

        private void SetVisualDead(bool dead)
        {
            transform.localScale = dead ? new Vector3(1f, 0.35f, 1f) : Vector3.one;
        }
    }
}
