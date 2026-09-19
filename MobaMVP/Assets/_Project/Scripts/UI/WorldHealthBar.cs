using UnityEngine;
using UnityEngine.UI;

namespace Moba.UI
{
    /// <summary>World-space HP bar for any ICombatTarget.</summary>
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _targetBehaviour;
        [SerializeField] private Image _fill;
        [SerializeField] private Vector3 _offset = new(0f, 2.2f, 0f);
        [SerializeField] private Canvas _canvas;

        private Moba.Combat.ICombatTarget _target;
        private Camera _cam;

        private void Awake()
        {
            ResolveTarget();
            _cam = Camera.main;
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
            }
        }

        private void ResolveTarget()
        {
            if (_targetBehaviour is Moba.Combat.ICombatTarget t)
            {
                _target = t;
                return;
            }

            _target = GetComponentInParent<Moba.Combat.ICombatTarget>();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                ResolveTarget();
                if (_target == null)
                {
                    return;
                }
            }

            var pos = _target.WorldPosition;
            transform.position = pos + _offset;
            if (_cam == null)
            {
                _cam = Camera.main;
            }

            if (_cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
            }

            if (_fill != null && _target.MaxHealth > 0f)
            {
                _fill.fillAmount = Mathf.Clamp01(_target.Health / _target.MaxHealth);
                _fill.color = _target.IsAlive
                    ? (_target.IsAttackable
                        ? new Color(0.2f, 0.85f, 0.35f)
                        : new Color(0.85f, 0.75f, 0.2f))
                    : new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }
}
