using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Moba.Bootstrap;
using Moba.Core;
using Moba.Network;

namespace Moba.Combat
{
    /// <summary>Host fills empty roster slots with bots when arena starts.</summary>
    public sealed class BotFillSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkObject _botPrefab;
        [SerializeField] private float _delaySeconds = 1.25f;

        private bool _spawned;

        private void Start()
        {
            StartCoroutine(SpawnWhenReady());
        }

        private IEnumerator SpawnWhenReady()
        {
            yield return new WaitForSeconds(_delaySeconds);
            for (var i = 0; i < 45; i++)
            {
                if (TryFill())
                {
                    yield break;
                }

                yield return null;
            }
        }

        private bool TryFill()
        {
            var nm = NetworkManager.Singleton;
            var session = GameSession.Instance;
            if (nm == null || !nm.IsServer || _botPrefab == null || session == null)
            {
                return false;
            }

            if (!session.FillBots || _spawned)
            {
                return session.FillBots == false || _spawned;
            }

            var humans = nm.ConnectedClientsIds.Count;
            var botsNeeded = BotBrain.BotsNeeded(humans, session.Mode);
            if (botsNeeded <= 0)
            {
                _spawned = true;
                return true;
            }

            var nextSlot = humans;
            for (var i = 0; i < botsNeeded; i++)
            {
                var slot = nextSlot + i;
                var team = TeamRules.AssignTeam(slot, session.Mode);
                var slotInTeam = BotBrain.SpawnIndexInTeam(slot, session.Mode);
                var hero = BotBrain.PickHeroForSlot(slot);
                SpawnBot(team, hero, slotInTeam);
            }

            _spawned = true;
            Debug.Log($"[BotFill] spawned {botsNeeded} bots for mode {session.Mode} (humans={humans})");
            return true;
        }

        private void SpawnBot(TeamId team, HeroId hero, int slotInTeam)
        {
            var teamIndex = team == TeamId.Blue ? 0 : 1;
            var marker = HeroCombat.FindSpawnMarker(teamIndex, slotInTeam);
            var pos = marker != null
                ? marker.transform.position
                : new Vector3((slotInTeam - 1) * 2f, 1f, team == TeamId.Blue ? -40f : 40f);

            var instance = Instantiate(_botPrefab, pos, Quaternion.identity);
            var combat = instance.GetComponent<HeroCombat>();
            combat.ConfigureBot(team, hero);
            var botAi = instance.GetComponent<BotController>();
            botAi?.SetSpawnSlot(slotInTeam);
            instance.Spawn(true);

            // Color body
            var renderer = instance.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = team == TeamId.Blue
                    ? new Color(0.25f, 0.5f, 1f)
                    : new Color(1f, 0.35f, 0.3f);
            }
        }
    }
}
