using System.Collections.Generic;
using UnityEngine;
using FistVR;
using Sodalite.Api;
using System;
using System.IO;

namespace NGA {

[Serializable]
public class FGMap {
    public string sceneName;
    public string DisplayName;
    public int price; // only used for homes
    public int mapType; // 0: Home, 1: Area
    // -- Area specific configs -- \\
    public string defaultCivSceneConfigFileName; // overrides "default_civ" config for map
    public string defaultEnemySceneConfigFileName; // overrides "default_enemy" config for map
    public List<string> otherSceneConfigFileNames; // can be referenced by name in FGContracts for this scene
}

public class FGMapsContainer {
    public List<FGMap> maps = new List<FGMap>();

    public void Init() {
        RegisterDefaultMaps();
        FGExternalLoader.LoadManifestsFromBepinex();
    }

    public void RegisterDefaultMaps() {
        // TODO: Add any future default maps here, right now they're passed through manifest.
    }

    public bool IsHomeRegistered(string sceneName) {
        return maps.Exists(map => map.sceneName == sceneName && map.mapType == 0);
    }

    public bool IsSceneRegistered(string sceneName) {
        return maps.Exists(map => map.sceneName == sceneName);
    }

    public bool IsAreaRegistered(string sceneName) {
        return maps.Exists(map => map.sceneName == sceneName && map.mapType == 1);
    }

    // REQ: Assumes you checked it's already a home.
    public int GetHomePrice(string sceneName) {
        return maps.Find(map => map.sceneName == sceneName).price;
    }

    public void RegisterMap(FGMap map, string sourceFolderName) {
        maps.Add(map);
        foreach (var configFileName in map.otherSceneConfigFileNames) {
            FGFileIoHandler.CopyAreaConfigFile(map.sceneName, Path.Combine(sourceFolderName, configFileName));
        }
    }
}

} // namespace NGA