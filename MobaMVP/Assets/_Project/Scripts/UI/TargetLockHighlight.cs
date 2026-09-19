using UnityEngine;
using Moba.Combat;
using Moba.Core;

namespace Moba.UI
{
    /// <summary>Highlights the owner's current nearest attack target with a ground ring.</summary>
    public sealed class TargetLockHighlight : MonoBehaviour
    {
        [SerializeField] private LineRenderer _ring;
        [SerializeField] private int _segments = 32;

        private HeroCombat _local;
        private Transform _follow;

        private void Awake()
        {
            if (_ring == null)
            {
                var go = new GameObject("LockRing");
                go.transform.SetParent(transform, false);
                _ring = go.AddComponent<LineRenderer>();
                _ring.useWorldSpace = true;
                _ring.loop = true;
                _ring.widthMultiplier = 0.14f;
                _ring.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
                _ring.startColor = new Color(1f, 0.35f, 0.2f, 0.95f);
                _ring.endColor = _ring.startColor;
            }

            _ring.enabled = false;
        }

        private void LateUpdate()
        {
            if (_local == null)
            {
                TryBind();
                if (_local == null)
                {
                    return;
                }
            }

            if (!_local.IsOwner || !_local.IsAlive)
            {
                _ring.enabled = false;
                return;
            }

            if (!CombatTargeting.TryFindNearestEnemy(_local, _local.AttackRange * 1.35f, out var target))
            {
                _ring.enabled = false;
                return;
            }

            var center = target.WorldPosition;
            center.y = 0.1f;
            var radius = target.Kind == CombatTargetKind.Tower || target.Kind == CombatTargetKind.Crystal
                ? 1.6f
                : 0.9f;
            _ring.enabled = true;
            _ring.positionCount = _segments + 1;
            for (var i = 0; i <= _segments; i++)
            {
                var t = i / (float)_segments * Mathf.PI * 2f;
                _ring.SetPosition(i, center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius));
            }
        }

        private void TryBind()
        {
            foreach (var combat in FindObjectsByType<HeroCombat>(FindObjectsSortMode.None))
            {
                if (combat.IsOwner && combat.Kind == CombatTargetKind.Hero && !combat.IsBotUnit)
                {
                    _local = combat;
                    return;
                }
            }
        }
    }
}
