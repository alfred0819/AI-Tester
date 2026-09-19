using UnityEngine;
using Moba.Combat;
using Moba.Core;

namespace Moba.UI
{
    /// <summary>Owner skill aim preview (circle / cone) while holding Q/R/E or 1/2/3.</summary>
    public sealed class SkillAimIndicator : MonoBehaviour
    {
        [SerializeField] private LineRenderer _circle;
        [SerializeField] private LineRenderer _cone;
        [SerializeField] private int _segments = 48;

        private HeroCombat _local;
        private SkillSlot _previewSlot = SkillSlot.None;

        private void Awake()
        {
            EnsureRenderers();
            SetVisible(false);
        }

        private void Update()
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
                SetVisible(false);
                return;
            }

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            _previewSlot = SkillSlot.None;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed || keyboard.digit1Key.isPressed || keyboard.kKey.isPressed)
                {
                    _previewSlot = SkillSlot.One;
                }
                else if (keyboard.rKey.isPressed || keyboard.digit2Key.isPressed)
                {
                    _previewSlot = SkillSlot.Two;
                }
                else if (keyboard.eKey.isPressed || keyboard.digit3Key.isPressed)
                {
                    _previewSlot = SkillSlot.Three;
                }
            }

            if (_previewSlot == SkillSlot.None)
            {
                SetVisible(false);
                return;
            }

            var skill = HeroCatalog.Get(_local.SelectedHero).GetSkill(_previewSlot);
            if (skill == null)
            {
                SetVisible(false);
                return;
            }

            var origin = _local.WorldPosition;
            origin.y = 0.08f;
            var aim = ReadAim(keyboard, _local.transform.forward);

            switch (skill.Shape)
            {
                case SkillShape.Cone:
                    DrawCone(origin, aim, skill.Range, skill.ConeAngleDegrees);
                    _circle.enabled = false;
                    _cone.enabled = true;
                    break;
                case SkillShape.PointCircle:
                {
                    var point = origin + aim * Mathf.Clamp(skill.Range * 0.7f, 2f, skill.Range);
                    DrawCircle(point, skill.Radius, new Color(0.3f, 0.9f, 1f, 0.85f));
                    _circle.enabled = true;
                    _cone.enabled = false;
                    break;
                }
                default:
                    DrawCircle(origin, Mathf.Max(skill.Radius, skill.DashDistance > 0f ? 1.2f : skill.Radius),
                        new Color(0.4f, 1f, 0.6f, 0.85f));
                    _circle.enabled = true;
                    _cone.enabled = false;
                    break;
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

        private static Vector3 ReadAim(UnityEngine.InputSystem.Keyboard keyboard, Vector3 fallback)
        {
            if (keyboard == null)
            {
                return fallback;
            }

            var move = Vector3.zero;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.z -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.z += 1f;
            return move.sqrMagnitude > 0.0001f ? move.normalized : fallback;
        }

        private void EnsureRenderers()
        {
            if (_circle == null)
            {
                var go = new GameObject("AimCircle");
                go.transform.SetParent(transform, false);
                _circle = go.AddComponent<LineRenderer>();
                ConfigureLine(_circle, new Color(0.4f, 1f, 0.6f, 0.85f));
            }

            if (_cone == null)
            {
                var go = new GameObject("AimCone");
                go.transform.SetParent(transform, false);
                _cone = go.AddComponent<LineRenderer>();
                ConfigureLine(_cone, new Color(1f, 0.85f, 0.3f, 0.9f));
            }
        }

        private static void ConfigureLine(LineRenderer lr, Color color)
        {
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.widthMultiplier = 0.12f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            lr.startColor = color;
            lr.endColor = color;
            lr.positionCount = 0;
        }

        private void DrawCircle(Vector3 center, float radius, Color color)
        {
            _circle.startColor = color;
            _circle.endColor = color;
            _circle.loop = true;
            _circle.positionCount = _segments + 1;
            for (var i = 0; i <= _segments; i++)
            {
                var t = i / (float)_segments * Mathf.PI * 2f;
                _circle.SetPosition(i, center + new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius));
            }
        }

        private void DrawCone(Vector3 origin, Vector3 aim, float range, float angleDeg)
        {
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = Vector3.forward;
            }

            aim.Normalize();
            var half = angleDeg * 0.5f * Mathf.Deg2Rad;
            var left = Quaternion.Euler(0f, -angleDeg * 0.5f, 0f) * aim;
            var right = Quaternion.Euler(0f, angleDeg * 0.5f, 0f) * aim;
            _cone.loop = false;
            _cone.positionCount = 3;
            _cone.SetPosition(0, origin + left * range);
            _cone.SetPosition(1, origin);
            _cone.SetPosition(2, origin + right * range);
        }

        private void SetVisible(bool visible)
        {
            if (_circle != null)
            {
                _circle.enabled = visible;
            }

            if (_cone != null)
            {
                _cone.enabled = visible;
            }
        }
    }
}
