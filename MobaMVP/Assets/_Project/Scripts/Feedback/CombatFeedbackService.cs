using UnityEngine;
using UnityEngine.UI;
using Moba.Core;

namespace Moba.Feedback
{
    /// <summary>Client hit feedback: damage floaters + brief body flash.</summary>
    public sealed class CombatFeedbackService : MonoBehaviour
    {
        public static CombatFeedbackService Instance { get; private set; }

        [SerializeField] private Canvas _worldCanvas;
        [SerializeField] private float _floaterLifetime = 0.85f;

        private void Awake()
        {
            Instance = this;
            if (_worldCanvas == null)
            {
                var go = new GameObject("FeedbackCanvas", typeof(Canvas));
                go.transform.SetParent(transform, false);
                _worldCanvas = go.GetComponent<Canvas>();
                _worldCanvas.renderMode = RenderMode.WorldSpace;
                _worldCanvas.sortingOrder = 50;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void OnHit(ulong targetNetId, float damage, bool fromSkill)
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null)
            {
                return;
            }

            if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var netObj) || netObj == null)
            {
                return;
            }

            SpawnFloater(netObj.transform.position + Vector3.up * 2f, damage, fromSkill);
            FlashRenderer(netObj.gameObject);
            MobaAudio.Instance?.PlayHit(fromSkill);
        }

        public void OnSkillCast(byte slot)
        {
            MobaAudio.Instance?.PlaySkill(slot);
        }

        private void SpawnFloater(Vector3 worldPos, float damage, bool fromSkill)
        {
            var go = new GameObject("DmgFloater", typeof(RectTransform), typeof(Text), typeof(DamageFloater));
            go.transform.SetParent(_worldCanvas.transform, false);
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one * 0.02f;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 42;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = fromSkill ? new Color(0.4f, 0.85f, 1f) : new Color(1f, 0.85f, 0.3f);
            text.text = Mathf.RoundToInt(damage).ToString();

            var floater = go.GetComponent<DamageFloater>();
            floater.Duration = _floaterLifetime;
        }

        private static void FlashRenderer(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r == null)
                {
                    continue;
                }

                var flash = r.gameObject.GetComponent<HitFlash>();
                if (flash == null)
                {
                    flash = r.gameObject.AddComponent<HitFlash>();
                }

                flash.Play(r);
            }
        }
    }

    public sealed class DamageFloater : MonoBehaviour
    {
        public float Duration = 0.85f;
        private float _t;
        private Vector3 _start;

        private void Start()
        {
            _start = transform.position;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            var u = _t / Duration;
            transform.position = _start + Vector3.up * (u * 1.4f);
            if (Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
            }

            var text = GetComponent<Text>();
            if (text != null)
            {
                var c = text.color;
                c.a = 1f - u;
                text.color = c;
            }

            if (_t >= Duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public sealed class HitFlash : MonoBehaviour
    {
        private Renderer _renderer;
        private Color _original;
        private float _left;

        public void Play(Renderer renderer)
        {
            _renderer = renderer;
            if (_renderer != null && _renderer.material != null)
            {
                _original = _renderer.material.color;
                _renderer.material.color = Color.white;
            }

            _left = 0.12f;
            enabled = true;
        }

        private void Update()
        {
            _left -= Time.deltaTime;
            if (_left > 0f || _renderer == null)
            {
                return;
            }

            _renderer.material.color = _original;
            enabled = false;
        }
    }
}
