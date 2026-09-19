using UnityEngine;
using Moba.Combat;
using Moba.Core;

namespace Moba.Bootstrap
{
    /// <summary>Soft top-down follow of the local hero.</summary>
    public sealed class ArenaCameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 _offset = new(0f, 28f, -18f);
        [SerializeField] private float _smooth = 6f;

        private Transform _target;
        private Camera _cam;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (_cam == null)
            {
                _cam = Camera.main;
                if (_cam == null)
                {
                    return;
                }
            }

            if (_target == null)
            {
                TryBind();
                if (_target == null)
                {
                    return;
                }
            }

            var desired = _target.position + _offset;
            _cam.transform.position = Vector3.Lerp(
                _cam.transform.position, desired, 1f - Mathf.Exp(-_smooth * Time.deltaTime));
            _cam.transform.LookAt(_target.position + Vector3.up * 1.2f);
        }

        private void TryBind()
        {
            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (combat.IsOwner && combat.Kind == CombatTargetKind.Hero && !combat.IsBotUnit)
                {
                    _target = combat.transform;
                    return;
                }
            }
        }
    }
}
