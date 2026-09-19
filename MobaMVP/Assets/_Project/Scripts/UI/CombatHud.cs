using UnityEngine;
using UnityEngine.UI;
using Moba.Combat;
using Moba.Core;

namespace Moba.UI
{
    /// <summary>Owner-only combat HUD: HP, gold, skill CDs, hero name.</summary>
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private Text _hintText;
        [SerializeField] private Text _statusText;

        private HeroCombat _localCombat;

        private void Update()
        {
            if (_localCombat == null)
            {
                TryBindLocal();
            }

            if (_localCombat == null)
            {
                return;
            }

            if (_hintText != null)
            {
                _hintText.text = "J普攻 | Q/1 技能1 | R/2 技能2 | E/3 技能3 | B商店 | WASD移动";
            }

            if (_statusText != null)
            {
                var hero = HeroCatalog.Get(_localCombat.SelectedHero).DisplayName;
                var hp = $"{_localCombat.Health:0}/{_localCombat.MaxHealth:0}";
                var cds =
                    $"Q{_localCombat.Skill1Cd:0.0} R{_localCombat.Skill2Cd:0.0} E{_localCombat.Skill3Cd:0.0}";
                var life = _localCombat.IsAlive
                    ? (_localCombat.IsStunned ? "眩晕" : "存活")
                    : $"复活 {_localCombat.RespawnRemaining:0.0}s";
                _statusText.text = $"{hero}  HP {hp}  金{_localCombat.Gold}  {cds}  {life}";
            }
        }

        private void TryBindLocal()
        {
            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (combat.IsOwner && combat.Kind == CombatTargetKind.Hero)
                {
                    _localCombat = combat;
                    return;
                }
            }
        }
    }
}
