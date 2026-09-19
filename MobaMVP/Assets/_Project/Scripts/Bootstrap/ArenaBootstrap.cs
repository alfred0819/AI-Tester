using UnityEngine;
using Moba.Core;

namespace Moba.Bootstrap
{
    /// <summary>Arena entry: initial framing + soft local-player camera follow.</summary>
    public sealed class ArenaBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Vector3 _cameraOffset = new(0f, 28f, -18f);
        [SerializeField] private Vector3 _lookAt = new(0f, 0f, 0f);

        private void Start()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var pivot = _cameraPivot != null ? _cameraPivot.position : _lookAt;
                cam.transform.position = pivot + _cameraOffset;
                cam.transform.LookAt(pivot);
                cam.orthographic = false;
            }

            if (GetComponent<ArenaCameraFollow>() == null)
            {
                gameObject.AddComponent<ArenaCameraFollow>();
            }

            Debug.Log($"[Arena] Ready mode={GameSession.Instance?.Mode} scene={SceneNames.Arena1Lane}");
            Moba.Combat.MatchResultController.Instance?.RescanHumanRoster();
        }
    }
}
