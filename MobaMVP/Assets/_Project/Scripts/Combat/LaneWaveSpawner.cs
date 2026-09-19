using Unity.Netcode;
using UnityEngine;
using Moba.Core;

namespace Moba.Combat
{
    /// <summary>Spawns mid-lane waves for both teams on a timer (server only).</summary>
    public sealed class LaneWaveSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject _minionPrefab;
        [SerializeField] private float _firstDelay = LaneWaveRules.FirstWaveDelaySeconds;
        [SerializeField] private float _interval = LaneWaveRules.WaveIntervalSeconds;
        [SerializeField] private int _perWave = LaneWaveRules.MeleePerWave;

        private float _timer;
        private bool _started;

        private void Start()
        {
            _timer = _firstDelay;
        }

        private void Update()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || _minionPrefab == null)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _timer = _interval;
            SpawnWave(TeamId.Blue);
            SpawnWave(TeamId.Red);
            _started = true;
            Debug.Log("[LaneWave] Wave spawned");
        }

        private void SpawnWave(TeamId team)
        {
            var waypoints = BuildWaypoints(team);
            var spawn = waypoints[0];
            for (var i = 0; i < _perWave; i++)
            {
                var offset = new Vector3((i - 1) * 1.2f, 0f, team == TeamId.Blue ? -1.2f * i : 1.2f * i);
                var instance = Instantiate(_minionPrefab, spawn + offset, Quaternion.identity);
                var minion = instance.GetComponent<MinionUnit>();
                minion.Configure(team, waypoints);
                instance.Spawn(true);
            }
        }

        private static Vector3[] BuildWaypoints(TeamId team)
        {
            // Mid lane corridor: fountain → tower → crystal approach.
            if (team == TeamId.Blue)
            {
                return new[]
                {
                    new Vector3(0f, 1f, -38f),
                    new Vector3(0f, 1f, -20f),
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0f, 1f, 20f),
                    new Vector3(0f, 1f, 40f)
                };
            }

            return new[]
            {
                new Vector3(0f, 1f, 38f),
                new Vector3(0f, 1f, 20f),
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 1f, -20f),
                new Vector3(0f, 1f, -40f)
            };
        }
    }
}
