using System;
using System.Collections.Generic;
using System.Text;

namespace Moba.Core
{
    /// <summary>Recall channel: hold at fountain teleport; cancel on move/damage/CC.</summary>
    public static class RecallRules
    {
        public const float ChannelSeconds = 4.5f;
        public const float AssistWindowSeconds = 10f;
        public const int GoldPerAssist = 50;
    }

    public sealed class RecallChannelState
    {
        public float Remaining { get; private set; }
        public bool IsChanneling => Remaining > 0f;

        public bool TryStart(float duration = RecallRules.ChannelSeconds)
        {
            if (IsChanneling || duration <= 0f)
            {
                return false;
            }

            Remaining = duration;
            return true;
        }

        public void Cancel()
        {
            Remaining = 0f;
        }

        /// <summary>Returns true when the channel completes this tick.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsChanneling || deltaTime <= 0f)
            {
                return false;
            }

            Remaining = MathF.Max(0f, Remaining - deltaTime);
            return Remaining <= 0f;
        }
    }

    public sealed class PlayerCombatRecord
    {
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Assists { get; private set; }

        public void RegisterKill() => Kills++;
        public void RegisterDeath() => Deaths++;
        public void RegisterAssist() => Assists++;

        public string FormatKda() => $"{Kills}/{Deaths}/{Assists}";
    }

    /// <summary>Tracks recent damagers for assist credit (pure logic; keys are opaque ids).</summary>
    public sealed class AssistTracker
    {
        private readonly Dictionary<ulong, float> _lastHitAt = new();

        public void RecordHit(ulong attackerId, float timeSeconds)
        {
            if (attackerId == 0)
            {
                return;
            }

            _lastHitAt[attackerId] = timeSeconds;
        }

        public void Clear() => _lastHitAt.Clear();

        public List<ulong> CollectAssists(ulong killerId, float nowSeconds, float window = RecallRules.AssistWindowSeconds)
        {
            var result = new List<ulong>();
            foreach (var pair in _lastHitAt)
            {
                if (pair.Key == killerId)
                {
                    continue;
                }

                var age = nowSeconds - pair.Value;
                if (age >= 0f && age <= window)
                {
                    result.Add(pair.Key);
                }
            }

            return result;
        }
    }

    public enum DisconnectResolution : byte
    {
        None = 0,
        EndMatchRemainingWins = 1,
        SpawnReplacementBot = 2
    }

    public static class DisconnectPolicy
    {
        public static DisconnectResolution Resolve(
            GameModeKind mode,
            bool matchInProgress,
            bool disconnectedIsHost)
        {
            if (!matchInProgress || disconnectedIsHost)
            {
                return DisconnectResolution.None;
            }

            return mode == GameModeKind.OneVsOne
                ? DisconnectResolution.EndMatchRemainingWins
                : DisconnectResolution.SpawnReplacementBot;
        }

        public static TeamId WinnerWhenOpponentLeft(TeamId remainingHumanTeam)
        {
            return remainingHumanTeam == TeamId.None ? TeamId.None : remainingHumanTeam;
        }
    }

    public readonly struct ScoreboardEntry
    {
        public readonly string Name;
        public readonly TeamId Team;
        public readonly int Kills;
        public readonly int Deaths;
        public readonly int Assists;
        public readonly int Gold;
        public readonly bool IsBot;

        public ScoreboardEntry(
            string name,
            TeamId team,
            int kills,
            int deaths,
            int assists,
            int gold,
            bool isBot)
        {
            Name = name ?? "?";
            Team = team;
            Kills = kills;
            Deaths = deaths;
            Assists = assists;
            Gold = gold;
            IsBot = isBot;
        }

        public string FormatLine()
        {
            var tag = IsBot ? "[Bot]" : "";
            var side = Team == TeamId.Blue ? "蓝" : Team == TeamId.Red ? "红" : "-";
            return $"{side} {Name}{tag}  {Kills}/{Deaths}/{Assists}  金{Gold}";
        }
    }

    public static class MatchScoreboard
    {
        public static string FormatDuration(float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }

            var total = (int)seconds;
            var m = total / 60;
            var s = total % 60;
            return $"{m:00}:{s:00}";
        }

        public static string FormatResult(string winnerLabel, float durationSeconds, IReadOnlyList<ScoreboardEntry> entries)
        {
            var sb = new StringBuilder();
            sb.Append(winnerLabel ?? "对局结束");
            sb.Append("\n时长 ").Append(FormatDuration(durationSeconds));
            if (entries == null || entries.Count == 0)
            {
                return sb.ToString();
            }

            sb.Append("\n—— 战绩 ——");
            for (var i = 0; i < entries.Count; i++)
            {
                sb.Append('\n').Append(entries[i].FormatLine());
            }

            return sb.ToString();
        }
    }
}
