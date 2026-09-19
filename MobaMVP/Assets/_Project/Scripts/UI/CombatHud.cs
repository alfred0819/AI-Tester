using UnityEngine;
using UnityEngine.UI;
using Moba.Combat;

namespace Moba.UI
{
    /// <summary>Owner-only combat HUD: attack / skill prompts and CD.</summary>
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
                _hintText.text = "J / LMB 普攻   |   K / Q 冲击波技能   |   WASD 移动";
            }

            if (_statusText != null)
            {
                var hp = $"{_localCombat.Health:0}/{_localCombat.MaxHealth:0}";
                var cd = _localCombat.SkillCooldownRemaining > 0f
                    ? $"技能CD {_localCombat.SkillCooldownRemaining:0.0}s"
                    : "技能就绪";
                var life = _localCombat.IsAlive
                    ? "存活"
                    : $"复活 {_localCombat.RespawnRemaining:0.0}s";
                _statusText.text = $"HP {hp}   {cd}   {life}";
            }
        }

        private void TryBindLocal()
        {
            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (combat.IsOwner)
                {
                    _localCombat = combat;
                    return;
                }
            }
        }
    }
}
