using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NGA
{
    public class FG_GM : MonoBehaviour
    {
        public static FG_GM Instance { get; private set; }
        private FGSceneManip mapLoader = new FGSceneManip();
        private FGContractManager contractMan; 
        private FGTimeSystem timeSys;
        private bool isTransitioningToNewLevel = false;
        private FGState saveState;
        private string saveSlotName = "SaveSlot0"; // TODO: Write code to be able to change this.
        public string wristUiSpawnId = "NGA_FgWristUi";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Singleton pattern.
            }
            else
            {
                Destroy(gameObject); // Prevent duplicates
            }
        }

        private void Start()
        {
            contractMan = this.gameObject.AddComponent<FGContractManager>();
            timeSys = this.gameObject.AddComponent<FGTimeSystem>();
            Init();
            // TODO: Add callbacks to events?
        }

        private void Init() {
            SpawnWristMenu();
            // Add quit receivers.
            GM.CurrentSceneSettings.QuitReceivers.Clear();
			GM.CurrentSceneSettings.QuitReceivers.Add(base.gameObject); // TODO: What do these do?
            InitSaveState();
        }
        private void SpawnWristMenu() {
            FVRObject wristUiFvr;
            if (!IM.OD.TryGetValue(wristUiSpawnId, out wristUiFvr))
            {
                Debug.LogError($"{wristUiSpawnId} not found in IM.OD!");
                return;
            }
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(wristUiFvr.GetGameObject(), Vector3.zero, Quaternion.identity);
			gameObject.GetComponent<CGrillerWristMenu>().SetHandsAndFace(GM.CurrentPlayerBody.RightHand.GetComponent<FVRViveHand>(), 
                GM.CurrentPlayerBody.LeftHand.GetComponent<FVRViveHand>(), GM.CurrentPlayerBody.EyeCam.transform);
        }
        private void InitSaveState() {
            if (saveState == null)
            {
                if (FGFileIoHandler.LoadFGState(saveSlotName, out FGState loadedState))
                {
                    saveState = loadedState;
                    InitTimeSysFromSave();
                    InitContractManFromSave();
                }
                else
                {
                    // TODO: New saveslot code should go here.
                    saveState = FGState.GetDefaultSave();
                    PrepareAndSave();
                }
            }
        }
        private void InitTimeSysFromSave() {
            FGTimeSystem.Config result = JsonUtility
                .FromJson<FGTimeSystem.Config>(saveState.timeSysConfig);
            timeSys.InitFromConfig(result);
        }
        private void InitContractManFromSave() {
            FGContractManager.Config result = JsonUtility
                .FromJson<FGContractManager.Config>(saveState.contractManConfig);
            contractMan.InitFromConfig(result);
        }

        private void PrepareAndSave() {
            saveState.timeSysConfig = JsonUtility.ToJson(timeSys.GetConfig());
            saveState.contractManConfig = JsonUtility.ToJson(contractMan.GetConfig());
            FGFileIoHandler.SaveFGState(saveSlotName, saveState);
        }

        // Handles the following scenarios and takes care of vault saving if needed.
        // Outside <-> Home
        // Outside <-> Area
        // Home <-> Area
        public void TransitionToLevel(string goto_scene)
		{
			if (this.isTransitioningToNewLevel)
			{
				return; // Skip if new level already being loaded.
			}
            string currSceneName = SceneManager.GetActiveScene().name; 
            bool currIsArea = saveState.IsValidArea(currSceneName);
            bool currIsHome = saveState.IsValidHome(currSceneName);
		    
            if (currIsArea)
            {
                // TODO: Call ProcessActiveContractsForSuccessAndFailure()
            }

            // Saves the home scene.
            if (currIsHome) {
                VaultFile currHomeCfg = new VaultFile();
                if (!VaultSystem.FindAndScanObjectsInScene(currHomeCfg)
                    || !FGFileIoHandler.SaveHomeVaultFile(saveSlotName, currSceneName, currHomeCfg))
                {
                    Debug.LogError("Failed to scan or write player QB durin level transition.");
                }
            }

            // Saves player loadout if they're currently in either home or area.
            if (currIsArea || currIsHome) {
                VaultFile curQb = new VaultFile();
                if (!VaultSystem.FindAndScanObjectsInQuickbelt(curQb)
                    || !FGFileIoHandler.SavePlayerQuickbelt(saveSlotName, curQb))
                {
                    Debug.LogError("Failed to scan or write player QB durin level transition.");
                }
            }

            // Loads next level.
            isTransitioningToNewLevel = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SteamVR_LoadLevel.Begin(goto_scene, false, 0.5f, 0f, 0f, 0f, 1f);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            isTransitioningToNewLevel = false;
            SpawnWristMenu(); // Wrist-menu is spawned on every scene.

            // Calls map loader depending on type of map.
            if (saveState.IsValidArea(scene.name)) {
                mapLoader.InitArea(saveSlotName, scene.name);
            } else if (saveState.IsValidHome(scene.name)) {
                mapLoader.InitHome(saveSlotName, scene.name);
            }
        }

        private void Update()
        {}

        private void QUIT() {
            // TODO: Possibly save scene if in home? likely not safe idk..
            PrepareAndSave();
        }
    }
}
