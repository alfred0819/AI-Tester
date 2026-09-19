using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Moba.Combat;

namespace Moba.Network
{
    /// <summary>Owner combat input → HeroCombat requests.</summary>
    public sealed class PlayerCombatInput : NetworkBehaviour
    {
        [SerializeField] private HeroCombat _combat;

        public override void OnNetworkSpawn()
        {
            if (_combat == null)
            {
                _combat = GetComponent<HeroCombat>();
            }
        }

        private void Update()
        {
            if (!IsOwner || _combat == null || !_combat.IsAlive)
            {
                return;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            var attack =
                (keyboard != null && keyboard.jKey.wasPressedThisFrame) ||
                (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (attack)
            {
                _combat.RequestBasicAttack();
            }

            var skill =
                (keyboard != null && (keyboard.kKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame));
            if (skill)
            {
                var aim = transform.forward;
                if (keyboard != null)
                {
                    var move = Vector3.zero;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.z -= 1f;
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.z += 1f;
                    if (move.sqrMagnitude > 0.0001f)
                    {
                        aim = move.normalized;
                    }
                }

                _combat.RequestSkill(aim);
            }
        }
    }
}
