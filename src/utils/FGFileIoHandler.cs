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
        public static string rootSaveFolder = "FreeGrillerAlpha1";
        public static string default_civ_vault_name = "default_civ";
        public static string default_enemy_vault_name = "default_enemy";
        
        // Function that goes to documents h3 save folder, and checks if our stuff is in it.
        public static bool LoadFGState(string saveSlotName, out FGState state)
        {
            state = null;
            try
            {
                string saveFolder = GetH3SaveFolder();
                string slotFolderPath = Path.Combine(saveFolder, rootSaveFolder);
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
                string slotFolderPath = Path.Combine(saveFolder, rootSaveFolder);
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
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
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
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
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

        public static bool DoesAreaVaultFileExists(string sceneName, string vaultFileName) {
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
                folderPath = Path.Combine(folderPath, "SceneConfigs"); // Not saveslot specific.
                folderPath = Path.Combine(folderPath, sceneName);

                string filePath = Path.Combine(folderPath, vaultFileName + ".json");

                if (Directory.Exists(folderPath) && File.Exists(filePath))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while loading the Vault file: {ex.Message}");
                return false;
            }
        }

        public static bool LoadAreaVaultFile(string sceneName, string vaultFileName, out VaultFile vf)
        {
            vf = null;
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
                folderPath = Path.Combine(folderPath, "SceneConfigs"); // Not saveslot specific.
                folderPath = Path.Combine(folderPath, sceneName);

                string filePath = Path.Combine(folderPath, vaultFileName + ".json");

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

        public static bool CopyAreaConfigFile(string sceneName, string fullConfigFileName)
        {
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
                folderPath = Path.Combine(folderPath, "SceneConfigs"); // Not saveslot specific.
                folderPath = Path.Combine(folderPath, sceneName);

                // Create the folder if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = Path.GetFileName(fullConfigFileName);
                string destinationFilePath = Path.Combine(folderPath, fileName);

                File.Copy(fullConfigFileName, destinationFilePath, true);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while copying the config file: {ex.Message}");
                return false;
            }
        }

        public static bool CopyDefaultAreaConfigFile(string sceneName, string fullConfigFileName,
                                                        bool isEnemy)
        {
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
                folderPath = Path.Combine(folderPath, "SceneConfigs"); // Not saveslot specific.
                folderPath = Path.Combine(folderPath, sceneName);

                // Create the folder if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fileName = isEnemy ? default_enemy_vault_name : default_civ_vault_name;
                string destinationFilePath = Path.Combine(folderPath, fileName + ".json");

                File.Copy(fullConfigFileName, destinationFilePath, true);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while copying the default config file: {ex.Message}");
                return false;
            }
        }

        public static bool LoadPlayerQuickbelt(string saveSlotName, out VaultFile vaultFile)
        {
            vaultFile = null;
            try
            {
                string saveFolder = GetH3SaveFolder();
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
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
                string folderPath = Path.Combine(saveFolder, rootSaveFolder);
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