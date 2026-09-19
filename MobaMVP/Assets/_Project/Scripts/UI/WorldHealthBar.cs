using UnityEngine;
using UnityEngine.UI;
using Moba.Combat;

namespace Moba.UI
{
    /// <summary>World-space HP bar following a hero.</summary>
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private HeroCombat _combat;
        [SerializeField] private Image _fill;
        [SerializeField] private Vector3 _offset = new(0f, 2.2f, 0f);
        [SerializeField] private Canvas _canvas;

        private Camera _cam;

        private void Awake()
        {
            if (_combat == null)
            {
                _combat = GetComponentInParent<HeroCombat>();
            }

            _cam = Camera.main;
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
            }
        }

        private void LateUpdate()
        {
            if (_combat == null)
            {
                return;
            }

            transform.position = _combat.transform.position + _offset;
            if (_cam == null)
            {
                _cam = Camera.main;
            }

            if (_cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position);
            }

            if (_fill != null && _combat.MaxHealth > 0f)
            {
                _fill.fillAmount = Mathf.Clamp01(_combat.Health / _combat.MaxHealth);
                _fill.color = _combat.IsAlive
                    ? new Color(0.2f, 0.85f, 0.35f)
                    : new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }
}
