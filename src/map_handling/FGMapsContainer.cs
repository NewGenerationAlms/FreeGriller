using System.Collections.Generic;
using UnityEngine;
using FistVR;
using Sodalite.Api;
using System;
using System.IO;
using System.Linq;

namespace NGA {

[Serializable]
public class FGMap {
    public string sceneName;
    public string DisplayName;
    public int price = 0; // only used for homes
    public int mapType; // 0: Home, 1: Area
    // -- Area specific configs -- \\
    public string defaultCivSceneConfigFileName; // overrides "default_civ" config for map
    public string defaultEnemySceneConfigFileName; // overrides "default_enemy" config for map
    public List<string> otherSceneConfigFileNames = new List<string>(); // can be referenced by name in FGContracts for this scene

    // Copy constructor
    public FGMap(FGMap other) {
        sceneName = other.sceneName;
        DisplayName = other.DisplayName;
        price = other.price;
        mapType = other.mapType;
        defaultCivSceneConfigFileName = other.defaultCivSceneConfigFileName;
        defaultEnemySceneConfigFileName = other.defaultEnemySceneConfigFileName;
        otherSceneConfigFileNames = new List<string>(other.otherSceneConfigFileNames ?? Enumerable.Empty<string>());
    }
}

public class FGMapsContainer {
    public List<FGMap> maps = new List<FGMap>();

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
        if (map == null) {
            Debug.LogError("Cannot register null map: " + sourceFolderName);
            return;
        }
        var existingMap = maps.Find(m => m.sceneName == map.sceneName);
        if (existingMap == null) {
            FGMap newmap = new FGMap(map);
            maps.Add(newmap);
            newmap.defaultCivSceneConfigFileName = 
                string.IsNullOrEmpty(map.defaultCivSceneConfigFileName) ? string.Empty : Path.Combine(sourceFolderName, map.defaultCivSceneConfigFileName);
            newmap.defaultEnemySceneConfigFileName = 
                string.IsNullOrEmpty(map.defaultEnemySceneConfigFileName) ? string.Empty : Path.Combine(sourceFolderName, map.defaultEnemySceneConfigFileName);
        } else {
            // Update existing map with new data
            existingMap.DisplayName = map.DisplayName;
            existingMap.price = map.price;
            existingMap.mapType = map.mapType;
            existingMap.defaultCivSceneConfigFileName = 
                string.IsNullOrEmpty(map.defaultCivSceneConfigFileName) ? string.Empty : Path.Combine(sourceFolderName, map.defaultCivSceneConfigFileName);
            existingMap.defaultEnemySceneConfigFileName = 
                string.IsNullOrEmpty(map.defaultEnemySceneConfigFileName) ? string.Empty : Path.Combine(sourceFolderName, map.defaultEnemySceneConfigFileName);
            existingMap.otherSceneConfigFileNames.AddRange(map.otherSceneConfigFileNames?
                .Select(fileName => string.IsNullOrEmpty(fileName) ? string.Empty : Path.Combine(sourceFolderName, fileName)) ?? Enumerable.Empty<string>());
        }
        FGMap addedMap = maps.Find(m => m.sceneName == map.sceneName);
        
        // Copy default configs
        if (!string.IsNullOrEmpty(addedMap.defaultCivSceneConfigFileName)) {
            FGFileIoHandler.CopyDefaultAreaConfigFile(addedMap.sceneName, addedMap.defaultCivSceneConfigFileName, false);
        }
        if (!string.IsNullOrEmpty(map.defaultEnemySceneConfigFileName)) {
            FGFileIoHandler.CopyDefaultAreaConfigFile(addedMap.sceneName, addedMap.defaultEnemySceneConfigFileName, true);
        }

        // Copy other configs
        foreach (var configFileName in addedMap.otherSceneConfigFileNames ?? Enumerable.Empty<string>()) {
            FGFileIoHandler.CopyAreaConfigFile(addedMap.sceneName, configFileName);
        }
        if (map.mapType == 1) {
            FG_GM.Instance.saveState.AddValidArea(addedMap.sceneName);
        }
    }
}

} // namespace NGA