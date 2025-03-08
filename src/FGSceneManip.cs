using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BepInEx;
using System.IO;
using FistVR;
using System.Security.Cryptography;
using System;
using System.Text;
using System.Linq;

namespace NGA {
public class FGSceneManip : MonoBehaviour {


	void Start () {
	}

	void Update () {
	}

	// Loads the base home scene config if one exists. If none, it clears the scene either way.
	public bool InitHome(string saveSlotName, string sceneName) {
		if (GM.IsAsyncLoading) {
			Debug.LogWarning("Waiting InitHome, IsAsyncLoading aka vault loading.");
			return false;
		}
		string error = "";
		VaultSystem.ClearExistingSaveableObjects(/*clear non-saveloadable*/true);
		VaultFile sceneFile;
		// Load scene config
		if (!FGFileIoHandler.LoadHomeVaultFile(saveSlotName, sceneName, out sceneFile)) {
			Debug.LogError("Loading CGriller Home Scene file failed");
		} else if (VaultSystem.SpawnVaultFile(sceneFile, base.transform, false,
																	false, /*clearScene=*/true, out error,
																	Vector3.zero, null, false)) {
			Debug.LogWarning("Scene config loaded for home.");
		} else {
			Debug.LogError("Failed to load scene with error: " + error + "\n on " + sceneFile);
		}
		// Always loads quickbelt in case player left Home to non FG with stuff on them.
		GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, false);
		SpawnPlayerQb(saveSlotName);
		return true;
	}

	public bool InitArea(string saveSlotName, string sceneName) {
		if (GM.IsAsyncLoading) {
			Debug.LogWarning("Waiting InitArea, IsAsyncLoading aka vault loading.");
			return false;
		}

		// Loads default scene vault file if it exists, otherwise loads scene as is.
		bool area_civ_exists = FGFileIoHandler
			.DoesAreaVaultFileExists(sceneName, FGFileIoHandler.default_civ_vault_name);
		if (area_civ_exists) {
			SpawnAreaVaultFile(sceneName, FGFileIoHandler.default_civ_vault_name);
		}
		
		// Scan and transform any PlannerLite into Posse objects, then kickstart the
		// posse objects.
		StartCoroutine(FG_GM.TryWithRetries(() => {
			if (PLtoFG.TransformPLtoFG()) {
				ScanAndProcessModeObjects(/*contract*/null);
				return true;
			}
			return false;
		}, 15, 1f));

		// TODO: Config time of day based on calendar thing?

		// Load quickbelt config
		GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, false);
		SpawnPlayerQb(saveSlotName);
		return true;
	}

	public bool InitAreaFromContract(string saveSlotName, FGContract contract) {
		if (GM.IsAsyncLoading) {
			Debug.LogWarning("Waiting InitArea, IsAsyncLoading aka vault loading.");
			return false;
		}

		string sceneName = contract.SceneName;
		string contract_civ_vault = contract.SceneCivConfigName;
		if (FGFileIoHandler.DoesAreaVaultFileExists(sceneName, contract_civ_vault))
		{
			SpawnAreaVaultFile(sceneName, contract_civ_vault);
		}
		string contract_enemy_vault = contract.SceneEnemyConfigName == "" ? 
								FGFileIoHandler.default_enemy_vault_name 
								: contract.SceneEnemyConfigName;
		if (FGFileIoHandler.DoesAreaVaultFileExists(sceneName, contract_enemy_vault)) {
			SpawnAreaVaultFile(sceneName, contract_enemy_vault);
		}
		
		// Scan and transform any PlannerLite into Posse objects, then kickstart the
		// posse objects.
		StartCoroutine(FG_GM.TryWithRetries(() => {
			if (PLtoFG.TransformPLtoFG()) {
				ScanAndProcessModeObjects(contract);
				return true;
			}
			return false;
		}, 15, 1f));

		// TODO: Config time of day based on calendar thing?

		// Load quickbelt config
		GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, false);
		SpawnPlayerQb(saveSlotName);
		return true;
	}

	private void SpawnAreaVaultFile(string sceneName, string vault_file)
	{
		string error = "";
		VaultFile sceneFile;
		if (!FGFileIoHandler.LoadAreaVaultFile(sceneName,
								vault_file, out sceneFile))
		{
			Debug.LogError("Loading CGriller Area Scene file failed " + sceneName
							+ vault_file);
		}
		else if (VaultSystem.SpawnVaultFile(sceneFile, base.transform, false,
																	false, /*clearScene=*/true, out error,
																	Vector3.zero, null, false))
		{
			Debug.LogWarning("Scene config loaded for area " + sceneName + vault_file);
		}
		else
		{
			Debug.LogError("Failed to load scene with error: " + error + "\n on " + sceneFile + " " + vault_file);
		}
	}

	public void SpawnPlayerQb(string saveSlotName) {
		string empty = "";
		VaultFile qbFile;
		Debug.LogWarning("GM.IsAsyncLoading: " + GM.IsAsyncLoading);
		if (!FGFileIoHandler.LoadPlayerQuickbelt(saveSlotName, out qbFile)) {
			Debug.Log("Loading CGriller Quickbelt file failed");
		} else if (!GM.IsAsyncLoading && 
					VaultSystem.SpawnVaultFile(qbFile, base.transform, false, /*isLoadout*/true,
												false, out empty, Vector3.zero, null, false)) {
			Debug.Log("Succeeded loading player loady");
		} else {
			Debug.LogError("Failed to spawn player loadout for reason:" + empty);
		}
	}

	private void ScanAndProcessModeObjects(FGContract contract) {
		// Find all FGTargetPosse objects in the scene
        FGTargetPosse[] allPosseObjects = GameObject.FindObjectsOfType<FGTargetPosse>();
        if (allPosseObjects.Length == 0)
        {
            Debug.LogWarning("No FGTargetPosse objects found in the scene.");
            return;
        }

        // Pick a random TargetPosse.
        int randomIndex = UnityEngine.Random.Range(0, allPosseObjects.Length);
        FGTargetPosse selectedPosse = allPosseObjects[randomIndex];
        if (contract != null)
        {
            selectedPosse.SetContract(contract);
        }
        selectedPosse.SelectAndSpawnPosseConfig();
	}

}
} // namespace NGA
