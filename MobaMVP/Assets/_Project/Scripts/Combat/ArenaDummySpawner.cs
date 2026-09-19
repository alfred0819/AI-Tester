using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Moba.Combat
{
    /// <summary>Server spawns a TrainingDummy near the blue lane for solo combat checks.</summary>
    public sealed class ArenaDummySpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject _dummyPrefab;
        [SerializeField] private Vector3 _spawnPosition = new(0f, 1f, -30f);
        [SerializeField] private bool _spawnOnStart = true;

        private NetworkObject _spawned;

        private void Start()
        {
            if (_spawnOnStart)
            {
                StartCoroutine(SpawnWhenReady());
            }
        }

        private IEnumerator SpawnWhenReady()
        {
            for (var i = 0; i < 60; i++)
            {
                TrySpawn();
                if (_spawned != null)
                {
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning("[ArenaDummySpawner] Failed to spawn TrainingDummy (is host running?)");
        }

        public void TrySpawn()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || _dummyPrefab == null)
            {
                return;
            }

            if (_spawned != null)
            {
                return;
            }

            var instance = Instantiate(_dummyPrefab, _spawnPosition, Quaternion.identity);
            instance.Spawn(true);
            _spawned = instance;
            Debug.Log("[ArenaDummySpawner] TrainingDummy spawned");
        }
    }
}
