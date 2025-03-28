using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System.Linq;

namespace NGA {

// Used only inside maps that have a contract spawned in them. 
// Dynamically assigns IFFs to factions in the scene.
public class FGMapFactionAssigner
{
    private Dictionary<string, int> factionToIFF;
    private int nextIFF;

    public FGMapFactionAssigner()
    {
        factionToIFF = new Dictionary<string, int>();
        nextIFF = 1; // Player is always 0
        factionToIFF["player"] = 0;
    }

    public int AssignIFF(string factionId)
    {
        if (factionToIFF.ContainsKey(factionId))
        {
            return factionToIFF[factionId];
        }

        if (nextIFF > 32)
        {
            Debug.LogError("Exceeded maximum number of IFFs - seting to -3");
            return -3; // Civilian.
        }

        factionToIFF[factionId] = nextIFF;
        return nextIFF++;
    }

    public void SetSosigFriendlyToFaction(Sosig s, string factionId, bool isFriendly)
    {
        int iff = AssignIFF(factionId);
        s.Priority.IFFChart[iff] = isFriendly;
    }

    public void SetSosigThreatableToFaction(Sosig s, string factionId, bool isThreatable)
    {
        int iff = AssignIFF(factionId);
        s.Priority.ThreatableChart[iff] = isThreatable;
    }

    public static void MakeSosigThreatenableToAll(Sosig s)
    {
        for (int i = 0; i < s.Priority.ThreatableChart.Length; i++)
        {
            s.Priority.ThreatableChart[i] = true;
        }
    }
    
} // class FGMapFactionAssigner

} // namespace NGA