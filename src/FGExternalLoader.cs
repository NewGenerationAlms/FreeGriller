using System.Collections.Generic;
using UnityEngine;
using FistVR;
using Sodalite.Api;
using System;
using System.IO;
using BepInEx;

namespace NGA {

[Serializable]
public class FGLoadManifest {
    public List<FGMap> maps;
    public List<FGFaction> factions;
    public List<FGContractTemplate> contractTemplates;
}

public class FGExternalLoader {
    public static void LoadManifestsFromBepinex() {
        try {
            string pluginsFolder = Paths.PluginPath;
            if (Directory.Exists(pluginsFolder)) {
                foreach (var directory in Directory.GetDirectories(pluginsFolder)) {
                    string manifestFile = Path.Combine(directory, "fgLoadManifest.json");
                    if (File.Exists(manifestFile)) {
                        FGLoadManifest manifest = ExtractManifestFromFile(manifestFile);
                        if (manifest != null) {
                            if (manifest.maps != null) {
                                foreach (var map in manifest.maps) {
                                    FG_GM.Instance.MapContainer?.RegisterMap(map, directory);
                                }
                            } else {
                                Debug.LogError($"No maps found in manifest: {manifestFile}");
                            }
                            if (manifest.factions != null) {
                                foreach (var faction in manifest.factions) {
                                    FG_GM.Instance.factionStance?.RegisterFaction(faction);
                                }
                            } else {
                                Debug.LogError($"No factions found in manifest: {manifestFile}");
                            }
                            if (manifest.contractTemplates != null) {
                                foreach (var contractTemplate in manifest.contractTemplates) {
                                    FGContractTemplateFactory.RegisterTemplate(contractTemplate);
                                }
                            } else {
                                Debug.LogError($"No contract templates found in manifest: {manifestFile}");
                            }
                        }
                    }
                }
            }
        } catch (Exception ex) {
            Debug.LogError($"Failed to load manifests from Bepinex: {ex.Message}");
        }
    }

    public static FGLoadManifest ExtractManifestFromFile(string fullFileName) {
        try {
            string fileContents = File.ReadAllText(fullFileName);
            return JsonUtility.FromJson<FGLoadManifest>(fileContents);
        } catch (Exception ex) {
            Debug.LogError($"Failed to load manifest from {fullFileName}: {ex.Message}");
            return null;
        }
    }
}

} // namespace NGA