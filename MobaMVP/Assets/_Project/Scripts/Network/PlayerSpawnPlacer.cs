using Unity.Netcode;
using UnityEngine;
using Moba.Core;
using Moba.Bootstrap;

namespace Moba.Network
{
    /// <summary>Moves newly spawned players to team spawn markers in the arena (server only).</summary>
    public sealed class PlayerSpawnPlacer : MonoBehaviour
    {
        private void OnEnable()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                return;
            }

            nm.OnClientConnectedCallback += OnClientConnected;
            if (nm.IsServer)
            {
                foreach (var clientId in nm.ConnectedClientsIds)
                {
                    TryPlace(nm, clientId);
                }
            }
        }

        private void OnDisable()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsServer)
            {
                // Player object may spawn one frame later.
                StartCoroutine(PlaceNextFrame(nm, clientId));
            }
        }

        private System.Collections.IEnumerator PlaceNextFrame(NetworkManager nm, ulong clientId)
        {
            yield return null;
            TryPlace(nm, clientId);
        }

        private static void TryPlace(NetworkManager nm, ulong clientId)
        {
            if (!nm.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
            {
                return;
            }

            var mode = GameSession.Instance != null
                ? GameSession.Instance.Mode
                : GameModeKind.OneVsOne;

            var slot = 0;
            foreach (var id in nm.ConnectedClientsIds)
            {
                if (id == clientId)
                {
                    break;
                }

                slot++;
            }

            var team = TeamRules.AssignTeam(slot, mode);
            var teamIndex = team == TeamId.Blue ? 0 : 1;
            var marker = GameObject.Find($"SpawnPoint_Team{teamIndex}_0");
            if (marker == null)
            {
                return;
            }

            client.PlayerObject.transform.position = marker.transform.position;
            client.PlayerObject.transform.rotation = Quaternion.LookRotation(
                team == TeamId.Blue ? Vector3.forward : Vector3.back);
        }
    }
}
