using System.Collections.Generic;
using UnityEngine;
using FistVR;
using Sodalite.Api;

namespace NGA {

// Represents configuration data for Sosigs.
[System.Serializable]
public class FGSosigManifest
{
    // Public fields (Hidden in Unity Editor, but accessible in code)
    [HideInInspector] public string Faction;
    [HideInInspector] public string FirstName;
    [HideInInspector] public string LastName;

    // Public fields (Visible & Editable in Unity Editor)
    public string UniqueId; // Can be use to match to FGContract TargetIDs key in the key-value pair.
    public int IFF = -1;
    public int SosigOrder; // Should correspond to Sosig.SosigOrder enum
    public int EnemyId;    // Should correspond to SosigEnemyID enum. Will be overriden if provided in contract.
    public bool IsTarget;
    public bool IsGuard;
    public bool IsExtra;
}

// Represents a movement path for Sosigs.
[System.Serializable]
public class FGPaths
{
    public List<Transform> Path;
}

// Represents a Sosig mandate, combining a spawn point, path, and manifest.
[System.Serializable]
public class FGSosigMandate
{
    public Transform SpawnPoint; // Where the Sosig spawns
    public FGPaths Path;         // Optional movement path
    public FGSosigManifest Manifest; // Defines Sosig properties
}

// Tracks an active Sosig instance along with its assigned path and manifest.
public class FGTrackedSosig
{
    public Sosig SosigInstance; // Reference to the spawned Sosig
    public FGPaths Path;
    public FGSosigManifest Manifest;
}

public static class FGSosigSpawner {
    public static Sosig SpawnMySosig(FGSosigMandate mandate) 
    {
        if (mandate == null || mandate.Manifest == null || mandate.SpawnPoint == null)
        {
            Debug.LogError("Invalid SosigMandate! Ensure Mandate, Manifest, and SpawnPoint are assigned.");
            return null;
        }

        FGSosigManifest manifest = mandate.Manifest;
        Vector3 position = mandate.SpawnPoint.position;
        Quaternion rotation = mandate.SpawnPoint.rotation;

        // Spawn the Sosig using the manifest's enemyId
        Sosig sosig = SosigAPI.Spawn(
            ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)manifest.EnemyId], 
            new SosigAPI.SpawnOptions(),
            position, 
            rotation
        );

        if (sosig == null)
        {
            Debug.LogError("Sosig spawn failed!");
            return null;
        }

        // Set IFF (faction ID), fallback to random if unset (-1). -3 is civilian.
        int iff = manifest.IFF == -1 ? UnityEngine.Random.Range(8, 32) : manifest.IFF;
        sosig.SetIFF(iff);

        // Assign orders based on manifest
        sosig.CurrentOrder = (Sosig.SosigOrder)manifest.SosigOrder;
        sosig.FallbackOrder = (Sosig.SosigOrder)manifest.SosigOrder; 

        // Set up behavior and movement
        sosig.SetDominantGuardDirection(rotation * Vector3.forward);
        sosig.UpdateGuardPoint(position);
        sosig.UpdateAssaultPoint(position);
        sosig.SetGuardInvestigateDistanceThreshold(3f); // TODO: Replace magic number if configurable
        sosig.UpdateIdlePoint(position);
        sosig.SetDominantGuardDirection(sosig.transform.forward);

        // Default fallback order to first order.
        sosig.FallbackOrder = (Sosig.SosigOrder)manifest.SosigOrder;

        // If a path is assigned, apply it to the Sosig
        if (mandate.Path != null && mandate.Path.Path.Count > 0)
        {
            // Assuming there's a method to set a movement path for the Sosig
            // TODO: Command pathwith after all sosigs made.
            // TODO: Remove magic numbers.
            sosig.CommandPathTo(mandate.Path.Path, /*noise*/0.2f, /*lingerTimer*/new Vector2(1f, 10f), 
                                /*pt_tolerance*/1.2f, Sosig.SosigMoveSpeed.Walking, 
                                Sosig.PathLoopType.PingPong, /*pathwith*/null, 
                                /*lookLerpRange*/0.3f, /*lookCycleSpeed*/10f, 
                                /*isPatrol*/false, /*pathSkirmishThreshold*/20f);;
        }

        return sosig;
    }
}

} // namespace NGA