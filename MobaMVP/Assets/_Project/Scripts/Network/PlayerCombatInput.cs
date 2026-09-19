using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Moba.Combat;
using Moba.Core;

namespace Moba.Network
{
    /// <summary>Owner combat input: attack, skills (Q/R/E or 1/2/3), B shop.</summary>
    public sealed class PlayerCombatInput : NetworkBehaviour
    {
        [SerializeField] private HeroCombat _combat;
        [SerializeField] private Moba.UI.ShopPanel _shopPanel;

        public override void OnNetworkSpawn()
        {
            if (_combat == null)
            {
                _combat = GetComponent<HeroCombat>();
            }
        }

        private void Update()
        {
            if (!IsOwner || _combat == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            {
                if (_shopPanel == null)
                {
                    _shopPanel = FindFirstObjectByType<Moba.UI.ShopPanel>();
                }

                _shopPanel?.Toggle();
            }

            if (!_combat.IsAlive || _combat.IsStunned)
            {
                return;
            }

            var attack =
                (keyboard != null && keyboard.jKey.wasPressedThisFrame) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (attack)
            {
                _combat.RequestBasicAttack();
            }

            if (keyboard == null)
            {
                return;
            }

            var aim = ReadAim(keyboard);
            if (keyboard.qKey.wasPressedThisFrame || keyboard.kKey.wasPressedThisFrame ||
                keyboard.digit1Key.wasPressedThisFrame)
            {
                _combat.RequestSkill(SkillSlot.One, aim);
            }

            if (keyboard.rKey.wasPressedThisFrame || keyboard.digit2Key.wasPressedThisFrame)
            {
                _combat.RequestSkill(SkillSlot.Two, aim);
            }

            if (keyboard.eKey.wasPressedThisFrame || keyboard.digit3Key.wasPressedThisFrame)
            {
                _combat.RequestSkill(SkillSlot.Three, aim);
            }
        }

        private Vector3 ReadAim(Keyboard keyboard)
        {
            var aim = transform.forward;
            var move = Vector3.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.z -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.z += 1f;
            if (move.sqrMagnitude > 0.0001f)
            {
                aim = move.normalized;
            }

            return aim;
        }
    }
}
