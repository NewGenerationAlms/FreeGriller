using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

namespace NGA {
public class FGContractTemplateFactory
{
    private static List<FGContractTemplate> contractTemplates = new List<FGContractTemplate>();

    public static void RegisterTemplate(FGContractTemplate template)
    {
        var existingTemplate = contractTemplates.FirstOrDefault(t => t.TemplateID == template.TemplateID);
        if (existingTemplate != null)
        {
            contractTemplates.Remove(existingTemplate);
        }
        contractTemplates.Add(template);
    }

    public static FGContractTemplate GetTemplateForFactionAndReputation(string factionID, float reputation)
    {
        List<FGContractTemplate> possibleTemplates = new List<FGContractTemplate>();

        for (int i = 0; i < contractTemplates.Count; i++)
        {
            FGContractTemplate template = contractTemplates[i];

            if (template.HiringFactionID != factionID)
                continue;

            // Check if reputation matches any requirement in the list
            bool isValid = template.ReputationRequirements.Count == 0; // No requirements means valid.
            for (int j = 0; j < template.ReputationRequirements.Count; j++)
            {
                FGContract.ReputationRequirement req = template.ReputationRequirements[j];
                if (req.FactionID == factionID && reputation >= req.MinimumRep && reputation <= req.MaximumRep)
                {
                    isValid = true;
                    break;
                }
            }

            if (isValid)
                possibleTemplates.Add(template);
        }

        return possibleTemplates.Count > 0 
            ? possibleTemplates[UnityEngine.Random.Range(0, possibleTemplates.Count)] 
            : null;
    }
}

[Serializable]
public class FGContractTemplate
{
    public string TemplateID; // Unique identifier for the template
    public string HiringFactionID;
    public string Infraction;
    
    public int MinGuards, MaxGuards;
    public List<SosigEnemyID> PossibleGuardTypes;
    public List<string> PossibleGuardFactionIDs;

    public int MinTargets = 1, MaxTargets;
    public List<SosigEnemyID> PossibleTargetTypes;
    public List<string> PossibleTargetFactionIDs;

    public int MinExtras, MaxExtras;
    public List<SosigEnemyID> PossibleExtraTypes;
    public List<string> PossibleExtrasFactionIDs;

    public List<string> PossibleScenes;
    public List<string> PossibleSceneCivConfigs;
    public List<string> PossibleSceneEnemConfigs;
    public int MaxConstraints = 3;
    public List<FGContract.ConstraintAndReward> PossibleConstraints = new List<FGContract.ConstraintAndReward>();

    public int MinCompensation, MaxCompensation;

    public int MinHoursLimit = 3;
    public int MaxHoursLimit = 8;

    public List<FGContract.ReputationReward> RepRewards = new List<FGContract.ReputationReward>();
    public List<FGContract.ReputationRequirement> ReputationRequirements = new List<FGContract.ReputationRequirement>();

    public FGContract GenerateContract()
    {
        if (!ValidateContract()) {
            Debug.LogError("You made a mistake Bucko! Contract invalid.");
            return null;
        }
        int compensation = UnityEngine.Random.Range(MinCompensation, MaxCompensation);
        FGContract contract = new FGContract
        {
            uniqueID = UnityEngine.Random.Range(0, 10000000),
            DisplayName = $"{HiringFactionID}: ${compensation}",
            TargetFirstName = "First", // TODO: Add way to add this from pools.
            TargetLastName = "Last",
            Infraction = Infraction,
            HiringFactionID = HiringFactionID,
            Compensation = compensation,
            SceneName = PossibleScenes[UnityEngine.Random.Range(0, PossibleScenes.Count)],
            SceneCivConfigName = GetRandomFromListOrEmpty(PossibleSceneCivConfigs),
            SceneEnemyConfigName = GetRandomFromListOrEmpty(PossibleSceneEnemConfigs),
            ConstraintsAndRewards = GenerateConstraints(MaxConstraints),
            expirationTime = FGTimeSystem.Instance.GetInGameTimeAfterRealDuration(
                                                TimeSpan.FromHours(
                                                    UnityEngine.Random.Range(MinHoursLimit, MaxHoursLimit)))
                                                    .ToString("o"),
            ReputationRequirements = ReputationRequirements,
            hasEnded = false,
            isAccepted = false,
            hasSucceeded = false,
            hasFailed = false,
            ReputationRewards = RepRewards,  
        };

        AssignTargetsGuardsExtras(contract);
        AssignFactionsTargetsGuardsExtras(contract);
        contract.ConvertToSerializable(); // Because we set dicts.
        return contract;
    }

    private bool ValidateContract() {
        bool scenesOk = PossibleScenes.Count > 0;
        if (MinTargets < 1) {
            Debug.LogError("I expected MinTargets must be at least 1.");
        }
        return scenesOk;
    }

    private void AssignTargetsGuardsExtras(FGContract contract)
    {
        int targetCount = UnityEngine.Random.Range(MinTargets, MaxTargets + 1);
        int guardCount = UnityEngine.Random.Range(MinGuards, MaxGuards + 1);
        int extraCount = UnityEngine.Random.Range(MinExtras, MaxExtras + 1);
        Debug.Log($"Assigning {targetCount} targets, {guardCount} guards, {extraCount} extras.");
        Debug.Log($"PossibleTargetTypes: {string.Join(", ", PossibleTargetTypes.Select(type => type.ToString()).ToArray())}");
        Debug.Log($"PossibleGuardTypes: {string.Join(", ", PossibleGuardTypes.Select(type => type.ToString()).ToArray())}");

        contract._TargetIDs["targets"] = SelectRandomSubset(PossibleTargetTypes, targetCount);
        contract._GuardIDs["guards"] = SelectRandomSubset(PossibleGuardTypes, guardCount);
        contract._ExtrasIDs["extras"] = SelectRandomSubset(PossibleExtraTypes, extraCount);
    }

    private void AssignFactionsTargetsGuardsExtras(FGContract contract)
    {
        Debug.Log("PossibleTargetFactionIDs: " + string.Join(", ", PossibleTargetFactionIDs.ToArray()));
        Debug.Log("PossibleGuardFactionIDs: " + string.Join(", ", PossibleGuardFactionIDs.ToArray()));
        contract._Faction_Target["targets"] = GetRandomFromListOrEmpty(PossibleTargetFactionIDs);
        contract._Faction_Guards["guards"] = GetRandomFromListOrEmpty(PossibleGuardFactionIDs);
        contract._Faction_Extras["extras"] = GetRandomFromListOrEmpty(PossibleExtrasFactionIDs);
    }

    private string GetRandomFromListOrEmpty(List<string> list) {
        return (list != null && list.Count > 0) 
            ? list[UnityEngine.Random.Range(0, list.Count)] 
            : "";
    }

    private List<SosigEnemyID> SelectRandomSubset(List<SosigEnemyID> source, int count)
    {
        List<SosigEnemyID> subset = new List<SosigEnemyID>();
        if (source.Count == 0)
            return subset;
        for (int i = 0; i < count; i++)
        {
            subset.Add(source[UnityEngine.Random.Range(0, source.Count)]);
        }
        return subset;
    }

    private List<FGContract.ConstraintAndReward> GenerateConstraints(int maxConstraints)
    {
        List<FGContract.ConstraintAndReward> constraints = new List<FGContract.ConstraintAndReward>();

        // Shuffle PossibleConstraints and take up to maxConstraints
        if (PossibleConstraints.Count > 0)
        {
            constraints.AddRange(PossibleConstraints.OrderBy(_ => UnityEngine.Random.value)
                                                    .Take(Mathf.Min(maxConstraints, PossibleConstraints.Count)));
        }
        // Template maker is in charge of making sure GrillAllTargets is included. This will help in future
        // if we want to add non-kill quests.

        return constraints;
    }
}

} // namespace NGA