using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

namespace NGA {

    [Serializable]
    public class FGFaction {
        public string FactionId;
        public float currentReputation;
        public float startingReputation;
        public float maxPossibleReputation;
        public float minPossibleReputation;
        public List<string> AlwaysHostileTowardsFactionIds;
    }

    public class FGFactionStance {
        [Serializable]
        public struct Config {
            public List<FGFaction> Factions;
        }

        public List<FGFaction> factions = new List<FGFaction>();

        public FGFactionStance() {
            factions = new List<FGFaction>();
        }

        public void InitFromConfig(Config config) {
            factions = config.Factions;
        }

        public Config GetConfig() {
            return new Config { Factions = factions };
        }

        public void RegisterFaction(FGFaction faction) {
            var existingFaction = factions.FirstOrDefault(f => f.FactionId == faction.FactionId);
            if (existingFaction != null) {
                existingFaction.startingReputation = faction.startingReputation;
                existingFaction.maxPossibleReputation = faction.maxPossibleReputation;
                existingFaction.minPossibleReputation = faction.minPossibleReputation;
                foreach (var hostileId in faction.AlwaysHostileTowardsFactionIds) {
                    if (!existingFaction.AlwaysHostileTowardsFactionIds.Contains(hostileId)) {
                        existingFaction.AlwaysHostileTowardsFactionIds.Add(hostileId);
                    }
                }
            } else {
                factions.Add(faction);
            }
        }

        public bool TryAdjustReputation(string factionId, float adjustment) {
            var faction = factions.FirstOrDefault(f => f.FactionId == factionId);
            if (faction == null) return false;

            faction.currentReputation = Mathf.Clamp(faction.currentReputation + adjustment, faction.minPossibleReputation, faction.maxPossibleReputation);
            return true;
        }

        // Returns the current reputation of the player with the specified faction
        // or 0 if the player has no reputation with the faction.
        public float GetReputationWithFaction(string factionId) {
            var faction = factions.FirstOrDefault(f => f.FactionId == factionId);
            return faction?.currentReputation ?? 0f;
        }

        public bool IsFactionHostileTowards(string sourceFaction, string toFaction) {
            var faction = factions.FirstOrDefault(f => f.FactionId == sourceFaction);
            return faction?.AlwaysHostileTowardsFactionIds.Contains(toFaction) ?? false;
        }

        public bool IsFactionHostileTowardsPly(string factionId) {
            // TODO: Determine if a faction is hostile towards the player. Unused rn.
            return true;
        }

        public string PrintFactionStance() {
            var result = new System.Text.StringBuilder();
            foreach (var faction in factions) {
                result.AppendLine($"<b>Faction ID:</b> {faction.FactionId}");
                result.AppendLine($"<b>Current Reputation:</b> {faction.currentReputation}");
                result.AppendLine($"<b>Max Reputation:</b> {faction.maxPossibleReputation}");
                result.AppendLine($"<b>Min Reputation:</b> {faction.minPossibleReputation}");
                result.AppendLine($"<b>Always Hostile Towards:</b> {string.Join(", ", faction.AlwaysHostileTowardsFactionIds.ToArray())}");
                result.AppendLine("---------------------------");
            }
            return result.ToString();
        }
    }

} // namespace NGA