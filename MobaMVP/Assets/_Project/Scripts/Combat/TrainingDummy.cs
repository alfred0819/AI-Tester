using Unity.Netcode;
using UnityEngine;

namespace Moba.Combat
{
    /// <summary>Stationary red-team dummy for solo Phase 1 combat checks.</summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(HeroCombat))]
    public sealed class TrainingDummy : NetworkBehaviour
    {
        [SerializeField] private Renderer _bodyRenderer;

        public override void OnNetworkSpawn()
        {
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (_bodyRenderer != null)
            {
                _bodyRenderer.material.color = new Color(0.85f, 0.25f, 0.2f);
            }

            name = "TrainingDummy";
        }
    }
}
