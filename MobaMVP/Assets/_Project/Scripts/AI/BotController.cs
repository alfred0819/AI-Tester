using Unity.Netcode;
using UnityEngine;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Server-only AI for fill bots: lane, fight, retreat, cast skills.</summary>
    [RequireComponent(typeof(HeroCombat))]
    public sealed class BotController : NetworkBehaviour
    {
        [SerializeField] private float _thinkInterval = 0.2f;
        [SerializeField] private int _spawnSlotInTeam;

        private HeroCombat _combat;
        private float _thinkTimer;

        public void SetSpawnSlot(int slotInTeam) => _spawnSlotInTeam = slotInTeam;

        public override void OnNetworkSpawn()
        {
            _combat = GetComponent<HeroCombat>();
        }

        private void Update()
        {
            if (!IsServer || _combat == null || !_combat.IsAlive)
            {
                return;
            }

            if (MatchResultController.Instance != null && MatchResultController.Instance.MatchEnded)
            {
                return;
            }

            _thinkTimer -= Time.deltaTime;
            if (_thinkTimer > 0f)
            {
                return;
            }

            _thinkTimer = _thinkInterval;
            ThinkAndAct();
        }

        private void ThinkAndAct()
        {
            var hasEnemy = CombatTargeting.TryFindNearestEnemy(
                _combat, BotBrain.DefaultAggroRange, out var enemy, includeBuildings: true);

            var inAttack = false;
            if (hasEnemy)
            {
                var d = CombatTargeting.HorizontalDistance(_combat.WorldPosition, enemy.WorldPosition);
                inAttack = TargetingRules.IsInRange(d, _combat.AttackRange);
            }

            var team = _combat.Team;
            var fountainZ = team == TeamId.Blue ? -40f : 40f;
            var laneZ = team == TeamId.Blue ? 40f : -40f;

            var perception = new BotPerception
            {
                SelfX = _combat.WorldPosition.x,
                SelfZ = _combat.WorldPosition.z,
                HealthRatio = _combat.MaxHealth > 0f ? _combat.Health / _combat.MaxHealth : 0f,
                CanAct = _combat.IsAlive && !_combat.IsStunned,
                Skill1Ready = _combat.Skill1Cd <= 0.05f,
                Skill2Ready = _combat.Skill2Cd <= 0.05f,
                Skill3Ready = _combat.Skill3Cd <= 0.05f,
                HasEnemyInAttackRange = inAttack,
                HasEnemyInAggroRange = hasEnemy,
                EnemyX = hasEnemy ? enemy.WorldPosition.x : 0f,
                EnemyZ = hasEnemy ? enemy.WorldPosition.z : 0f,
                FountainX = 0f,
                FountainZ = fountainZ,
                LaneTargetX = 0f,
                LaneTargetZ = laneZ,
                AttackRange = _combat.AttackRange,
                AggroRange = BotBrain.DefaultAggroRange
            };

            var action = BotBrain.Decide(perception, out var mx, out var mz);
            var aim = hasEnemy
                ? (enemy.WorldPosition - _combat.WorldPosition)
                : new Vector3(mx, 0f, mz);
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = transform.forward;
            }

            switch (action)
            {
                case BotActionKind.Retreat:
                case BotActionKind.Move:
                    _combat.ServerMove(new Vector3(mx, 0f, mz), _thinkInterval);
                    break;
                case BotActionKind.Attack:
                    _combat.ServerTryBasicAttack();
                    break;
                case BotActionKind.CastSkill1:
                    _combat.ServerTrySkill(SkillSlot.One, aim);
                    break;
                case BotActionKind.CastSkill2:
                    _combat.ServerTrySkill(SkillSlot.Two, aim);
                    break;
                case BotActionKind.CastSkill3:
                    _combat.ServerTrySkill(SkillSlot.Three, aim);
                    break;
            }
        }

        public override void OnNetworkDespawn()
        {
            // no-op
        }
    }
}
