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
	public void InitHome(string saveSlotName, string sceneName) {
		GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, false);
		string empty = "";
		// Load scene config
		if (!FGFileIoHandler.LoadHomeVaultFile(saveSlotName, sceneName, out VaultFile sceneFile)) {
			Debug.LogError("Loading CGriller Home Scene file failed");
		} else if (!GM.IsAsyncLoading && VaultSystem.SpawnVaultFile(sceneFile, base.transform, false,
																	false, /*clearScene=*/true, out empty,
																	Vector3.zero, null, true)) {
			// Scene loaded successfully
		} else {
			VaultSystem.ClearExistingSaveableObjects(true);
			Debug.LogWarning(empty);
		}
		// Always loads quickbelt in case player left Home to non FG with stuff on them.
		SpawnPlayerQb(saveSlotName);
	}

	// TODO:
	public void InitArea(string saveSlotName, string sceneName) {
		// TODO: Use ContractConfig to know how to populate scene.
		GM.CurrentMovementManager.TeleportToPoint(GM.CurrentSceneSettings.DeathResetPoint.position, false);
		// TODO: Load base scene config
		
		// TODO: Load enemy scene config on top.

		// TODO: Config time of day based on calendar thing?

		// Load quickbelt config
		SpawnPlayerQb(saveSlotName);
	}

	public void SpawnPlayerQb(string saveSlotName) {
		string empty = "";
		if (!FGFileIoHandler.LoadPlayerQuickbelt(saveSlotName, out VaultFile qbFile)) {
			Debug.Log("Loading CGriller Quickbelt file failed");
		} else if (!GM.IsAsyncLoading && VaultSystem.SpawnVaultFile(qbFile, base.transform, false, /*isLoadout*/true,
																	false, out empty, Vector3.zero, null, false)) {
			// Quickbelt loaded successfully
		} else {
			Debug.LogError(empty);
		}
	}


}
} // namespace NGA
