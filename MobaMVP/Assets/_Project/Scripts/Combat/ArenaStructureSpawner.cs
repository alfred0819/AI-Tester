using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Server spawns towers and crystals for both teams.</summary>
    public sealed class ArenaStructureSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject _towerPrefab;
        [SerializeField] private NetworkObject _crystalPrefab;

        private void Start()
        {
            StartCoroutine(SpawnWhenReady());
        }

        private IEnumerator SpawnWhenReady()
        {
            for (var i = 0; i < 60; i++)
            {
                if (TrySpawn())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private bool TrySpawn()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || _towerPrefab == null || _crystalPrefab == null)
            {
                return false;
            }

            if (FindFirstObjectByType<DefenseTower>() != null)
            {
                return true;
            }

            SpawnTower(TeamId.Blue, new Vector3(0f, 1.5f, -25f));
            SpawnTower(TeamId.Red, new Vector3(0f, 1.5f, 25f));
            SpawnCrystal(TeamId.Blue, new Vector3(0f, 1f, -45f));
            SpawnCrystal(TeamId.Red, new Vector3(0f, 1f, 45f));
            Debug.Log("[Arena] Towers and crystals spawned");
            return true;
        }

        private void SpawnTower(TeamId team, Vector3 pos)
        {
            var instance = Instantiate(_towerPrefab, pos, Quaternion.identity);
            var tower = instance.GetComponent<DefenseTower>();
            tower.SetTeam(team);
            instance.Spawn(true);
        }

        private void SpawnCrystal(TeamId team, Vector3 pos)
        {
            var instance = Instantiate(_crystalPrefab, pos, Quaternion.identity);
            var crystal = instance.GetComponent<NexusCrystal>();
            crystal.SetTeam(team);
            instance.Spawn(true);
        }
    }
}
