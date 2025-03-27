using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System.Linq;


namespace NGA {

// Represents a group of Sosigs that can be spawned based on different configurations.
public class FGTargetPosse : MonoBehaviour
{
    public FGContract contract { get; private set; } // Active contract
    public List<PosseConfig> TargetConfigs = new List<PosseConfig>(); // All configurations

    public PosseConfig selectedPosseConfig { get; private set; } // The currently active config

    public List<FGTrackedSosig> trackedTargets { get; private set; } = new List<FGTrackedSosig>();
    public List<FGTrackedSosig> trackedGuards { get; private set; } = new List<FGTrackedSosig>();
    public List<FGTrackedSosig> trackedExtras { get; private set; } = new List<FGTrackedSosig>();

    // A configuration of Sosigs, separated into roles.
    [System.Serializable]
    public class PosseConfig
    {
        public List<FGSosigMandate> Targets = new List<FGSosigMandate>();
        public List<FGSosigMandate> Guards = new List<FGSosigMandate>();
        public List<FGSosigMandate> Extras = new List<FGSosigMandate>();
    }

    public FGMapFactionAssigner factionAssigner { get; private set; }

    public FGTargetPosse()
    {
        factionAssigner = new FGMapFactionAssigner();
    }


    // Assigns a new contract, clearing any existing Sosigs.
    public void SetContract(FGContract newContract)
    {
        DestroyAll(); // Remove existing Sosigs before setting a new contract
        contract = newContract;
    }

    // Clears all spawned Sosigs and resets tracking.
    public void DestroyAll()
    {
        foreach (var sosig in trackedTargets) Destroy(sosig.SosigInstance.gameObject);
        foreach (var sosig in trackedGuards) Destroy(sosig.SosigInstance.gameObject);
        foreach (var sosig in trackedExtras) Destroy(sosig.SosigInstance.gameObject);

        trackedTargets.Clear();
        trackedGuards.Clear();
        trackedExtras.Clear();

        selectedPosseConfig = null;
    }

    // Selects and spawns a new posse configuration.
    public void SelectAndSpawnPosseConfig()
    {
        if (TargetConfigs.Count == 0) return;

        selectedPosseConfig = TargetConfigs[Random.Range(0, TargetConfigs.Count)]; // Pick a random posse
        SpawnSelectedConfig();
    }

    public void AddPosseConfig(PosseConfig config) {
        TargetConfigs.Add(config);
    }

    // Finds a Sosig in the tracked lists.
    public FGTrackedSosig FindSosig(Sosig sosig)
    {
        foreach (var tracked in trackedTargets)
            if (tracked.SosigInstance == sosig) return tracked;

        foreach (var tracked in trackedGuards)
            if (tracked.SosigInstance == sosig) return tracked;

        foreach (var tracked in trackedExtras)
            if (tracked.SosigInstance == sosig) return tracked;

        return null;
    }

    // Spawns Sosigs based on the selected configuration.
    private void SpawnSelectedConfig()
    {
        if (selectedPosseConfig == null || contract == null)  {
            Debug.LogError("No selected posse config or contract.");
            return;
        }
        trackedTargets.Clear();
        trackedGuards.Clear();
        trackedExtras.Clear();
        
        SpawnEntities(contract._TargetIDs, 
            /*isTarget*/true, /*isGuard*/false, /*isExtra*/false, 
            selectedPosseConfig.Targets, trackedTargets);
        SpawnEntities(contract._GuardIDs, 
            /*isTarget*/false, /*isGuard*/true, /*isExtra*/false, 
            selectedPosseConfig.Guards, trackedGuards);
        SpawnEntities(contract._ExtrasIDs, 
            /*isTarget*/false, /*isGuard*/false, /*isExtra*/true, 
            selectedPosseConfig.Extras, trackedExtras);
    }

    // For every entity specified in the contract it tries to spawn a sosig 
    // if there are available spots provided by mapper in mandate list. Adds
    // tracked sosigs to corresponding list.
    private void SpawnEntities(
        Dictionary<string, List<SosigEnemyID>> entityList, 
        bool isTarget, bool isGuard, bool isExtra, 
        List<FGSosigMandate> mandateList,
        List<FGTrackedSosig> trackedList)
    {
        HashSet<int> pickedMandateIndexes = new HashSet<int>();
        foreach (var id_to_listSosigEnemyIds in entityList)
        {
            string specialIdentifier = id_to_listSosigEnemyIds.Key;
            List<SosigEnemyID> possibleTypes = id_to_listSosigEnemyIds.Value;
            if (possibleTypes == null || possibleTypes.Count == 0) {
                continue;
            }

            SosigEnemyID chosenEnemyType = (possibleTypes != null && possibleTypes.Count > 0) 
                ? possibleTypes[UnityEngine.Random.Range(0, possibleTypes.Count)] 
                : SosigEnemyID.Misc_Dummy;

            FGSosigMandate chosenMandate = PickUnpickedMandate(specialIdentifier, 
                                                                pickedMandateIndexes,
                                                                mandateList);
            if (chosenMandate == null) {
                return; // No more spots to place targets.
            }

            chosenMandate.Manifest.EnemyId = (int)chosenEnemyType;

            if (isTarget)
            {
                chosenMandate.Manifest.Faction = contract._Faction_Target
                    .TryGetValue(specialIdentifier, out var faction) ? faction : "";
                chosenMandate.Manifest.FirstName = contract.TargetFirstName;
                chosenMandate.Manifest.LastName = contract.TargetLastName;
                chosenMandate.Manifest.IsTarget = true;
            } else if (isGuard) {
                chosenMandate.Manifest.Faction = contract._Faction_Guards
                    .TryGetValue(specialIdentifier, out var guardFaction) ? guardFaction : "";
                chosenMandate.Manifest.IsGuard = true;
            } else {
                chosenMandate.Manifest.Faction = contract._Faction_Extras
                    .TryGetValue(specialIdentifier, out var extraFaction) ? extraFaction : "";
                chosenMandate.Manifest.IsExtra = true;
            }

            if (string.IsNullOrEmpty(chosenMandate.Manifest.UniqueId))
            {
                chosenMandate.Manifest.UniqueId = UnityEngine.Random.Range(0, 99999).ToString();
            }

            FGTrackedSosig spawnedSosig = SpawnNpcFromMandate(chosenMandate);
            if (spawnedSosig != null) {
                trackedList.Add(spawnedSosig);
                ConfigureSosigFaction(spawnedSosig, chosenMandate.Manifest.Faction);
            }
        }
    }
    
    // Grabs mandate with Unique id if exists otherwise grabs next mandate not yet picked.
    private FGSosigMandate PickUnpickedMandate(string uniqueId, HashSet<int> pickedIndexes,
        List<FGSosigMandate> mandates)
    {
        for (int i = 0; i < mandates.Count; i++)
        {
            var mandate = mandates[i];
            if (!pickedIndexes.Contains(i)
                && mandate.Manifest.UniqueId == uniqueId)
            {
                pickedIndexes.Add(i);
                return mandate;
            }
        }
        // Pick from remaining mandates, excluding those that have been picked already
        for (int i = 0; i < mandates.Count; i++)
        {
            if (!pickedIndexes.Contains(i))
            {
                pickedIndexes.Add(i);
                return mandates[i];
            }
        }

        // If all mandates have been picked, return null (or handle accordingly)
        return null;
    }

    // Spawns an NPC based on a given mandate.
    private FGTrackedSosig SpawnNpcFromMandate(FGSosigMandate mandate)
    {
        if (mandate == null || mandate.SpawnPoint == null || mandate.Manifest == null) {
            Debug.LogError("Either spawn point or manifest null");
            return null;
        }
        Sosig sosig = FGSosigSpawner.SpawnMySosig(mandate);
        if (sosig == null) {
            return null;
        }

        // TODO: Assign relevant Sosig properties from mandate.Manifest
        // Example: sosig.SetFaction(mandate.Manifest.Faction);
        
        return new FGTrackedSosig { SosigInstance = sosig, Path = mandate.Path, Manifest = mandate.Manifest };
    }

    private void ConfigureSosigFaction(FGTrackedSosig trackedSosig, string faction)
    {
        Sosig sosig = trackedSosig.SosigInstance;
        FGFactionStance stance = FG_GM.Instance.factionStance;
        stance.GetFactionEnemies(faction, out var enemies);
        sosig.SetIFF(factionAssigner.AssignIFF(faction));
        // Make Sosig friendly to all factions by default, updated afterwards.
        sosig.Priority.SetAllFriendly();
        FGMapFactionAssigner.MakeSosigThreatenableToAll(sosig);
        
        // Make Sosig hostile to all enemies of the faction.
        foreach (var enemy in enemies)
        {
            factionAssigner.SetSosigFriendlyToFaction(sosig, enemy, false);
        }

        // Make Sosig friendly to all target and guard factions if it is a target 
        // or guard.
        if (trackedSosig.Manifest.IsTarget || trackedSosig.Manifest.IsGuard)
        {
            var friendlyFactions = contract._Faction_Target.Values
                .Concat(contract._Faction_Guards.Values)
                .Distinct();

            foreach (var friendlyFaction in friendlyFactions)
            {
                factionAssigner.SetSosigFriendlyToFaction(sosig, friendlyFaction, true);
                factionAssigner.SetSosigThreatableToFaction(sosig, friendlyFaction, false);
            }
        }
    }

}

} // namespace NGA