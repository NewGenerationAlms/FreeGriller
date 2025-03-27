using System.Collections.Generic;
using UnityEngine;
using FistVR;
using Sodalite.Api;

namespace NGA {

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

} // namespace NGA