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
    // Remove SerializableKeyValuePair and related lists
    // [Serializable]
    // public class SerializableKeyValuePair { ... }

    // Replace Faction_Target, Faction_Guards, Faction_Extras
    [SerializeField] private List<string> Faction_Target_Keys = new List<string>();
    [SerializeField] private List<string> Faction_Target_Values = new List<string>();
    [SerializeField] private List<string> Faction_Guards_Keys = new List<string>();
    [SerializeField] private List<string> Faction_Guards_Values = new List<string>();
    [SerializeField] private List<string> Faction_Extras_Keys = new List<string>();
    [SerializeField] private List<string> Faction_Extras_Values = new List<string>();

    // Replace TargetIDs, GuardIDs, ExtrasIDs
    [SerializeField] private List<string> TargetIDs_Keys = new List<string>();
    [SerializeField] private List<FGContract.IntListWrapper> TargetIDs_Values = new List<FGContract.IntListWrapper>();
    [SerializeField] private List<string> GuardIDs_Keys = new List<string>();
    [SerializeField] private List<FGContract.IntListWrapper> GuardIDs_Values = new List<FGContract.IntListWrapper>();
    [SerializeField] private List<string> ExtrasIDs_Keys = new List<string>();
    [SerializeField] private List<FGContract.IntListWrapper> ExtrasIDs_Values = new List<FGContract.IntListWrapper>();

    [NonSerialized] public Dictionary<string, List<SosigEnemyID>> _TargetIDs = new Dictionary<string, List<SosigEnemyID>>();
    [NonSerialized] public Dictionary<string, List<SosigEnemyID>> _GuardIDs = new Dictionary<string, List<SosigEnemyID>>();
    [NonSerialized] public Dictionary<string, List<SosigEnemyID>> _ExtrasIDs = new Dictionary<string, List<SosigEnemyID>>();

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

    [Serializable]
    public class IntListWrapper
    {
        public List<int> InnerList = new List<int>();
    }

    // -- UI Printout Helpers --- \\
    public string PrintContract()
    {
        StringBuilder sb = new StringBuilder();
        string GetStatusPrintFromBool(bool success, bool violated)
        {
            if (success) return "✔ Completed";
            if (violated) return "✖ Failed";
            return "Pending";
        }

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
                    string status = GetStatusPrintFromBool(constraint.constraintSuccess, 
                                        constraint.constraintViolated);

                    sb.AppendLine($"  - {constraint.ConstraintID} ({status})");
                }
            }

            if (optionalConstraints.Count > 0)
            {
                sb.AppendLine("Optional Bonuses:");
                foreach (var constraint in optionalConstraints)
                {
                    string status = GetStatusPrintFromBool(constraint.constraintSuccess, 
                                        constraint.constraintViolated);
                    string bonus = constraint.rewardAddedIfSucceed > 0 ? $" +${constraint.rewardAddedIfSucceed}" : "";
                    string penalty = constraint.rewardSubtractedIfFail > 0 ? $" -${constraint.rewardSubtractedIfFail}" : "";
                    string outcome = $"{bonus}|{penalty}";
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
        // Convert _TargetIDs to TargetIDs_Keys and TargetIDs_Values
        void SerializeSosigEnemyLists(Dictionary<string, List<SosigEnemyID>> source, List<string> keys, List<IntListWrapper> values)
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in source)
            {
                keys.Add(kvp.Key);
                var wrapper = new IntListWrapper { InnerList = kvp.Value.Select(id => (int)id).ToList() };
                values.Add(wrapper);
            }
        }
        SerializeSosigEnemyLists(_TargetIDs, TargetIDs_Keys, TargetIDs_Values);
        SerializeSosigEnemyLists(_GuardIDs, GuardIDs_Keys, GuardIDs_Values);
        SerializeSosigEnemyLists(_ExtrasIDs, ExtrasIDs_Keys, ExtrasIDs_Values);

        void SerializeSimpleStringDict(Dictionary<string, string> source, List<string> keys, List<string> values)
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in source)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        SerializeSimpleStringDict(_Faction_Target, Faction_Target_Keys, Faction_Target_Values);
        SerializeSimpleStringDict(_Faction_Guards, Faction_Guards_Keys, Faction_Guards_Values);
        SerializeSimpleStringDict(_Faction_Extras, Faction_Extras_Keys, Faction_Extras_Values);
    }

    // Method to convert lists back to dictionaries after deserialization
    public void ConvertToDictionary()
    {
        // Exit early if any of the new fields are missing
        if (TargetIDs_Keys == null || TargetIDs_Values == null ||
            GuardIDs_Keys == null || GuardIDs_Values == null ||
            ExtrasIDs_Keys == null || ExtrasIDs_Values == null ||
            Faction_Target_Keys == null || Faction_Target_Values == null ||
            Faction_Guards_Keys == null || Faction_Guards_Values == null ||
            Faction_Extras_Keys == null || Faction_Extras_Values == null)
        {
            Debug.LogWarning("One or more required fields are missing. Skipping ConvertToDictionary.");
            return;
        }

        void DeserializeSosigEnemyLists(List<string> keys, List<IntListWrapper> values, Dictionary<string, List<SosigEnemyID>> targetDict)
        {
            targetDict.Clear();
            for (int i = 0; i < keys.Count; i++)
            {
                if (values[i] != null && values[i].InnerList != null)
                {
                    targetDict[keys[i]] = values[i].InnerList.Select(id => (SosigEnemyID)id).ToList();
                } else
                {
                    targetDict[keys[i]] = new List<SosigEnemyID>();
                    Debug.LogError($"Null or empty InnerList for key: {keys[i]}");
                }
            }
        }
        DeserializeSosigEnemyLists(TargetIDs_Keys, TargetIDs_Values, _TargetIDs);
        DeserializeSosigEnemyLists(GuardIDs_Keys, GuardIDs_Values, _GuardIDs);
        DeserializeSosigEnemyLists(ExtrasIDs_Keys, ExtrasIDs_Values, _ExtrasIDs);

        void DeserializeSimpleStringDict(List<string> keys, List<string> values, Dictionary<string, string> targetDict)
        {
            targetDict.Clear();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i < values.Count) // Check if the value exists
                {
                    targetDict[keys[i]] = values[i];
                }
                else
                {
                    Debug.LogError($"Missing value for key: {keys[i]}");
                }
            }
        }
        DeserializeSimpleStringDict(Faction_Target_Keys, Faction_Target_Values, _Faction_Target);
        DeserializeSimpleStringDict(Faction_Guards_Keys, Faction_Guards_Values, _Faction_Guards);
        DeserializeSimpleStringDict(Faction_Extras_Keys, Faction_Extras_Values, _Faction_Extras);
    }
}
} // namespace NGA