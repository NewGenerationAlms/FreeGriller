using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGA
{

[Serializable]
public class FGState
{
    // List of safehouses and arenas (can be extended later to other types of scenes)
    [SerializeField] private List<string> valid_homes = new List<string>();
    [SerializeField] private List<string> valid_areas = new List<string>();
    public string timeSysConfig;
    public string contractManConfig;
    public string bankConfig;
    public string factionStanceConfig;

    // TODO: Add saveslot code.

    // Constructor (if needed, but optional)
    public FGState()
    {
        
    }

    public static FGState GetDefaultSave() {
        FGState sigma = new FGState();
        sigma.AddValidHome("IndoorRange_Updated");
        sigma.AddValidArea("Grillhouse_2Story");
        return sigma;
    }

    public void AddValidArea(string sceneName)
    {
        if (!valid_areas.Contains(sceneName))
        {
            valid_areas.Add(sceneName);
        }
    }
    public bool IsValidArea(string sceneName) {
        return valid_areas.Contains(sceneName);
    }
    public bool IsValidHome(string sceneName) {
        return valid_homes.Contains(sceneName);
    }
    public void AddValidHome(string sceneName)
    {
        if (!valid_homes.Contains(sceneName))
        {
            valid_homes.Add(sceneName);
        }
    }
}

} // namespace NGA