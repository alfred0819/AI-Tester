using UnityEngine;
using UnityEngine.UI;
using Moba.Combat;
using Moba.Core;

namespace Moba.UI
{
    /// <summary>Owner combat HUD with match clock, KDA, and recall status.</summary>
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private Text _hintText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _timerText;

        private HeroCombat _localCombat;
        private float _matchElapsed;

        private void Update()
        {
            if (_localCombat == null)
            {
                TryBindLocal();
            }

            if (MatchResultController.Instance != null && MatchResultController.Instance.MatchEnded)
            {
                // freeze local clock display at end
            }
            else
            {
                _matchElapsed += Time.deltaTime;
            }

            if (_timerText != null)
            {
                var source = MatchResultController.Instance != null
                    ? MatchResultController.Instance.MatchDuration
                    : _matchElapsed;
                var m = Mathf.FloorToInt(source / 60f);
                var s = Mathf.FloorToInt(source % 60f);
                _timerText.text = $"{m:00}:{s:00}";
            }

            if (_localCombat == null)
            {
                return;
            }

            if (_hintText != null)
            {
                _hintText.text = "按住 Q/R/E 看范围 | J普攻 | B商店 | T回城 | WASD";
            }

            if (_statusText != null)
            {
                var hero = HeroCatalog.Get(_localCombat.SelectedHero).DisplayName;
                var hp = $"{_localCombat.Health:0}/{_localCombat.MaxHealth:0}";
                var cds =
                    $"Q{_localCombat.Skill1Cd:0.0} R{_localCombat.Skill2Cd:0.0} E{_localCombat.Skill3Cd:0.0}";
                var kda = $"{_localCombat.Kills}/{_localCombat.Deaths}/{_localCombat.Assists}";
                var life = !_localCombat.IsAlive
                    ? $"复活 {_localCombat.RespawnRemaining:0.0}s"
                    : _localCombat.IsRecalling
                        ? $"回城 {_localCombat.RecallRemaining:0.0}s"
                        : _localCombat.IsStunned
                            ? "眩晕"
                            : "存活";
                _statusText.text =
                    $"{hero}  HP {hp}  金{_localCombat.Gold}  KDA {kda}  {cds}  {life}";
            }
        }

        private void TryBindLocal()
        {
            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (combat.IsOwner && combat.Kind == CombatTargetKind.Hero && !combat.IsBotUnit)
                {
                    _localCombat = combat;
                    return;
                }
            }
        }
    }
}
