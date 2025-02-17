using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using static FistVR.ItemSpawnerV2;

namespace NGA
{
    public class FGFileIoHandler
    {
        // Function that goes to documents h3 save folder, and checks if our stuff is in it.
        public static bool LoadFGState(string saveSlotName, out FGState state)
        {
            state = null;
            try
            {
                string saveFolder = GetH3SaveFolder();
                string slotFolderPath = Path.Combine(saveFolder, "FreeGriller");
                slotFolderPath = Path.Combine(slotFolderPath, saveSlotName);
                string filePath = Path.Combine(slotFolderPath, saveSlotName + ".json");

                if (Directory.Exists(slotFolderPath) && File.Exists(filePath))
                {
                    string fileContents = File.ReadAllText(filePath);
                    state = JsonUtility.FromJson<FGState>(fileContents);
                    return state != null;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while checking if the file exists: {ex.Message}");
                return false;
            }
        }

        public static bool SaveFGState(string saveSlotName, FGState cgState)
        {
            try
            {
                string saveFolder = GetH3SaveFolder();
                string slotFolderPath = Path.Combine(saveFolder, "FreeGriller");
                slotFolderPath = Path.Combine(slotFolderPath, saveSlotName);
                string filePath = Path.Combine(slotFolderPath, saveSlotName + ".json");

                // Create the folder if it doesn't exist.
                if (!Directory.Exists(slotFolderPath))
                {
                    Directory.CreateDirectory(slotFolderPath);
                }

                string json = JsonUtility.ToJson(cgState);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while saving the file: {ex.Message}");
                return false;
            }
        }

        public static bool LoadHomeVaultFile(string saveSlotName, string sceneName, out VaultFile vf)
        {
            vf = null;
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, "FreeGriller");
                folderPath = Path.Combine(folderPath, saveSlotName);
                folderPath = Path.Combine(folderPath, "SceneConfigs");
                folderPath = Path.Combine(folderPath, sceneName);

                string filePath = Path.Combine(folderPath, "latest_" + sceneName + ".json");

                if (Directory.Exists(folderPath) && File.Exists(filePath))
                {
                    string fileContents = File.ReadAllText(filePath);
                    vf = JsonUtility.FromJson<VaultFile>(fileContents);

                    return vf != null;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while loading the Vault file: {ex.Message}");
                return false;
            }
        }

        public static bool SaveHomeVaultFile(string saveSlotName, string sceneName, VaultFile vf)
        {
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, "FreeGriller");
                folderPath = Path.Combine(folderPath, saveSlotName);
                folderPath = Path.Combine(folderPath, "SceneConfigs");
                folderPath = Path.Combine(folderPath, sceneName);

                // Create the folder if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, "latest_" + sceneName + ".json");

                string json = JsonUtility.ToJson(vf);
                File.WriteAllText(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while saving the Vault file: {ex.Message}");
                return false;
            }
        }

        public static bool LoadPlayerQuickbelt(string saveSlotName, out VaultFile vaultFile)
        {
            vaultFile = null;
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, "FreeGriller");
                folderPath = Path.Combine(folderPath, saveSlotName);
                folderPath = Path.Combine(folderPath, "Objects");
                folderPath = Path.Combine(folderPath, "Loadouts");

                string filePath = Path.Combine(folderPath, "player_loady.json");

                if (Directory.Exists(folderPath) && File.Exists(filePath))
                {
                    string fileContents = File.ReadAllText(filePath);
                    vaultFile = JsonUtility.FromJson<VaultFile>(fileContents);

                    return vaultFile != null;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while loading the player quickbelt file: {ex.Message}");
                return false;
            }
        }

        public static bool SavePlayerQuickbelt(string saveSlotName, VaultFile vaultFile)
        {
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, "FreeGriller");
                folderPath = Path.Combine(folderPath, saveSlotName);
                folderPath = Path.Combine(folderPath, "Objects");
                folderPath = Path.Combine(folderPath, "Loadouts");

                // Create the folder if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string filePath = Path.Combine(folderPath, "player_loady.json");

                // Serialize VaultFile to JSON and write it to the file
                string json = JsonUtility.ToJson(vaultFile);
                File.WriteAllText(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while saving the player quickbelt file: {ex.Message}");
                return false;
            }
        }
        
        // Function that returns filepath to game docs.
        public static string GetH3SaveFolder() {
            string sceneConfigsPath = "\\My Games\\H3VR\\Vault\\";
            string fullSceneConfigsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                                                    + sceneConfigsPath;
            return fullSceneConfigsPath;
        } 
    }
}
