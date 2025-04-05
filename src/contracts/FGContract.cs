using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text;
using System.Linq;

namespace NGA {
[Serializable]
public class FGContract
{
    public int uniqueID;
    // --- For-display Metadata --- \\
    public string DisplayName;
    public string HiringFactionID; // case-sensitive
    public string TargetFirstName;
    public string TargetLastName;
    public string Infraction;


    // --- Target Info --- \\
    [Serializable]
    public class SerializableKeyValuePair
    {
        public string Key;
        public string Value;

        public SerializableKeyValuePair() { }

        public SerializableKeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    // Mapping from gameplanner keys to SosigEnemyIDs.
    [SerializeField] private List<SerializableKeyValuePair> TargetIDs = new List<SerializableKeyValuePair>(); // default key "targets"
    [SerializeField] private List<SerializableKeyValuePair> GuardIDs = new List<SerializableKeyValuePair>(); // default key "guards"
    [SerializeField] private List<SerializableKeyValuePair> ExtrasIDs = new List<SerializableKeyValuePair>(); // default key "extras"
    [NonSerialized] public Dictionary<string, List<SosigEnemyID>> _TargetIDs = new Dictionary<string, List<SosigEnemyID>>();
    [NonSerialized] public Dictionary<string, List<SosigEnemyID>> _GuardIDs = new Dictionary<string, List<SosigEnemyID>>();
    [NonSerialized] public Dictionary<string, List<SosigEnemyID>> _ExtrasIDs = new Dictionary<string, List<SosigEnemyID>>();

    // Mapping from gameplanner keys to factions.
    [SerializeField] private List<SerializableKeyValuePair> Faction_Target = new List<SerializableKeyValuePair>();  // Add list of factions for Target
    [SerializeField] private List<SerializableKeyValuePair> Faction_Guards = new List<SerializableKeyValuePair>();   // Add list of factions for Guards
    [SerializeField] private List<SerializableKeyValuePair> Faction_Extras = new List<SerializableKeyValuePair>();   // Add list of factions for Extras
    [NonSerialized] public Dictionary<string, string> _Faction_Target = new Dictionary<string, string>();
    [NonSerialized] public Dictionary<string, string> _Faction_Guards = new Dictionary<string, string>();
    [NonSerialized] public Dictionary<string, string> _Faction_Extras = new Dictionary<string, string>();


    // --- Scene Loading --- \\
    public string SceneName; // In-game name of scene to load.
    // Default is "default_civ". Can be empty to load nothing. Otherwise it's the X of the X.json
    // file that should be loaded (Game-planner Lite).
    public string SceneCivConfigName; 
    // Default is "default_enemy". Can be empty to let mapper load stuff. Otherwise it's the X of the X.json
    // file that should be loaded (Game-planner Lite)
    public string SceneEnemyConfigName;


    // --- Restrictions --- \\
    public List<ReputationRequirement> ReputationRequirements;
    public List<ConstraintAndReward> ConstraintsAndRewards = new List<ConstraintAndReward>();
    public string expirationTime;
    public DateTime ExpirationDateTime
    {
        get
        {
            return DateTime.Parse(expirationTime); // Use .ToString("o") to store as ISO 8601 string.
        }
    }


    // --- Rewards --- \\
    public int Compensation;
    public List<ReputationReward> ReputationRewards;


    // --- Quest state --- \\
    public bool hasEnded; // Regardless of success/fail.
    public bool isAccepted;
    public bool hasSucceeded, hasFailed;

    [Serializable]
    public struct ReputationReward {
        public string FactionID; // case-sensitive

        public float Rep; // positive or negative
    }

    [Serializable]
    public struct ReputationRequirement
    {
        public string FactionID; // case-sensitive

        public float MinimumRep;

        public float MaximumRep;
    }

    [Serializable]
    public struct ConstraintAndReward {
        public string ConstraintID; // Each corresponds to an IContractConstraint implementation.
        public bool optional;
        public bool constraintSuccess;
        public bool constraintViolated;
        public int rewardSubtractedIfFail; // Positive number.
        public int rewardAddedIfSucceed; // Can be zero.
    }

    // -- UI Printout Helpers --- \\
    public string PrintContract()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Contract: {DisplayName}");
        sb.AppendLine($"Hiring Faction: {HiringFactionID}");
        sb.AppendLine($"Target: {TargetFirstName} {TargetLastName}");
        sb.AppendLine($"Infraction: {Infraction}");
        sb.AppendLine($"Scene: {SceneName}");

        DateTime inGameTime = FGTimeSystem.Instance.CurrentTime;
        if (ExpirationDateTime > inGameTime)
        {
            var realTimeLeft = FGTimeSystem.Instance.CalculateRealTimeUntil(inGameTime, ExpirationDateTime);
            sb.AppendLine($"Time until expiration: {realTimeLeft.Hours}h {realTimeLeft.Minutes}m {realTimeLeft.Seconds}s");
        }

        sb.AppendLine($"Compensation: ${Compensation:N0}");

        if (ReputationRequirements != null && ReputationRequirements.Count > 0)
        {
            sb.AppendLine("Reputation Requirements:");
            foreach (var req in ReputationRequirements)
            {
                sb.AppendLine($"  - {req.FactionID}: {req.MinimumRep} to {req.MaximumRep}");
            }
        }

        if (ReputationRewards != null && ReputationRewards.Count > 0)
        {
            sb.AppendLine("Reputation Rewards:");
            foreach (var reward in ReputationRewards)
            {
                sb.AppendLine($"  - {reward.FactionID}: {reward.Rep:+0.0;-0.0}");
            }
        }

        if (ConstraintsAndRewards != null && ConstraintsAndRewards.Count > 0)
        {
            var requiredConstraints = ConstraintsAndRewards.Where(c => !c.optional).ToList();
            var optionalConstraints = ConstraintsAndRewards.Where(c => c.optional).ToList();

            if (requiredConstraints.Count > 0)
            {
                sb.AppendLine("Required Conditions:");
                foreach (var constraint in requiredConstraints)
                {
                    string status = constraint.constraintSuccess ? "✔ Completed" :
                                    constraint.constraintViolated ? "✖ Failed" :
                                    "Pending";

                    string penalty = constraint.rewardSubtractedIfFail > 0 ? $" -${constraint.rewardSubtractedIfFail}" : "";
                    sb.AppendLine($"  - {constraint.ConstraintID} ({status}){penalty}");
                }
            }

            if (optionalConstraints.Count > 0)
            {
                sb.AppendLine("Optional Bonuses:");
                foreach (var constraint in optionalConstraints)
                {
                    string status = constraint.constraintSuccess ? "✔ Completed" :
                                    constraint.constraintViolated ? "✖ Failed" :
                                    "Pending";

                    string bonus = constraint.rewardAddedIfSucceed > 0 ? $" +${constraint.rewardAddedIfSucceed}" : "";
                    sb.AppendLine($"  - {constraint.ConstraintID} ({status}){bonus}");
                }
            }
        }

        return sb.ToString();
    }



    // --- Save/Load Helper methods --- \\
    public List<IContractConstraint> GetConstraints()
    {
        List<IContractConstraint> constraints = new List<IContractConstraint>();

        foreach (var constraintData in ConstraintsAndRewards)
        {
            var constraint = ConstraintFactory.GetConstraint(constraintData.ConstraintID);
            if (constraint != null)
                constraints.Add(constraint);
        }

        return constraints;
    }
    public void PrepareForSave() {
        ConvertToSerializable();
    }
    public void PrepareFromLoad() {
        ConvertToDictionary();
    }
    public void ConvertToSerializable()
    {
        GuardIDs.Clear();
        foreach (var kvp in _GuardIDs)
        {
            GuardIDs.Add(new SerializableKeyValuePair(kvp.Key, string.Join(",", kvp.Value.Select(id => ((int)id).ToString()).ToArray())));
        }
        
        TargetIDs.Clear();
        foreach (var kvp in _TargetIDs)
        {
            TargetIDs.Add(new SerializableKeyValuePair(kvp.Key, string.Join(",", kvp.Value.Select(id => ((int)id).ToString()).ToArray())));
        }
        
        ExtrasIDs.Clear();
        foreach (var kvp in _ExtrasIDs)
        {
            ExtrasIDs.Add(new SerializableKeyValuePair(kvp.Key, string.Join(",", kvp.Value.Select(id => ((int)id).ToString()).ToArray())));
        }
        Faction_Target.Clear();
        foreach (var kvp in _Faction_Target)
        {
            Faction_Target.Add(new SerializableKeyValuePair(kvp.Key, kvp.Value));
        }

        Faction_Guards.Clear();
        foreach (var kvp in _Faction_Guards)
        {
            Faction_Guards.Add(new SerializableKeyValuePair(kvp.Key, kvp.Value));
        }

        Faction_Extras.Clear();
        foreach (var kvp in _Faction_Extras)
        {
            Faction_Extras.Add(new SerializableKeyValuePair(kvp.Key, kvp.Value));
        }
    }

    // Method to convert lists back to dictionaries after deserialization
    public void ConvertToDictionary()
    {
        _GuardIDs.Clear();
        foreach (var kvp in GuardIDs)
        {
            _GuardIDs.Add(kvp.Key, kvp.Value.Split(',').Select(id => (SosigEnemyID)int.Parse(id)).ToList());
        }
        
        _TargetIDs.Clear();
        foreach (var kvp in TargetIDs)
        {
            _TargetIDs.Add(kvp.Key, kvp.Value.Split(',').Select(id => (SosigEnemyID)int.Parse(id)).ToList());
        }
        
        _ExtrasIDs.Clear();
        foreach (var kvp in ExtrasIDs)
        {
            _ExtrasIDs.Add(kvp.Key, kvp.Value.Split(',').Select(id => (SosigEnemyID)int.Parse(id)).ToList());
        }
        _Faction_Target.Clear();
        foreach (var kvp in Faction_Target)
        {
            _Faction_Target.Add(kvp.Key, kvp.Value);
        }

        _Faction_Guards.Clear();
        foreach (var kvp in Faction_Guards)
        {
            _Faction_Guards.Add(kvp.Key, kvp.Value);
        }

        _Faction_Extras.Clear();
        foreach (var kvp in Faction_Extras)
        {
            _Faction_Extras.Add(kvp.Key, kvp.Value);
        }
    }
}
} // namespace NGA