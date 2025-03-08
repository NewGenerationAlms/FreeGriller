using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using Valve.VR;

namespace NGA
{
    public class FG_GM : MonoBehaviour
    {
        public static FG_GM Instance { get; private set; }
        public FGSceneManip mapLoader;
        public FGContractManager contractMan; 
        public FGTimeSystem timeSys;
        public FGBank bank;
        public FGFactionStance factionStance;
        public bool causedInTransitioningLevels = false;
        public string lastTransitionedToSceneName = "";
        public FGState saveState { get; private set; }
        private string saveSlotName = "SaveSlot0"; // TODO: Write code to be able to change this.
        public string wristUiSpawnId = "NGA_FgWristUi";
        private FGContract contractForTransition;
        public FGMapsContainer MapContainer { get; private set; }

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
            // Initialize Monobehavior scripts.
            timeSys = this.gameObject.AddComponent<FGTimeSystem>(); // First because others need its callbacks.
            mapLoader = this.gameObject.AddComponent<FGSceneManip>();
            contractMan = this.gameObject.AddComponent<FGContractManager>();
            MapContainer = new FGMapsContainer();
            bank = new FGBank();
            factionStance = new FGFactionStance();
            
            // Add event callbacks.
            SceneManager.sceneLoaded += OnSceneLoaded;
            SteamVR_Events.Loading.Listen(OnSceneChangeRequested);
            
            Init();
            FGExternalLoader.LoadManifestsFromBepinex(); // Load after init so new templates/factions are added.
        }

        private void Init() {
            StartCoroutine(SpawnWristMenuWithRetries(40, 3f)); // Retry up to 10 times, every 10 seconds
            InitSaveState();
        }
        private bool SpawnWristMenu() {
            FVRObject wristUiFvr;
            if (!IM.OD.TryGetValue(wristUiSpawnId, out wristUiFvr))
            {
                Debug.LogError(wristUiSpawnId + " not found in IM.OD!");
                return false;
            }
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(wristUiFvr.GetGameObject(), Vector3.zero, Quaternion.identity);
            Debug.LogWarning($"{wristUiSpawnId} supposedly spawned");
            return true;
        }
        private void InitSaveState() {
            if (saveState == null)
            {
                if (FGFileIoHandler.LoadFGState(saveSlotName, out FGState loadedState))
                {
                    saveState = loadedState;
                    InitTimeSysFromSave();
                    InitContractManFromSave();
                    InitBankFromSave();
                    InitFactionStanceFromSave();
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
        private void InitBankFromSave() {
            FGBank.Config result = JsonUtility
                .FromJson<FGBank.Config>(saveState.bankConfig);
            bank.InitFromConfig(result);
        }
        private void InitFactionStanceFromSave() {
            FGFactionStance.Config result = JsonUtility
                .FromJson<FGFactionStance.Config>(saveState.factionStanceConfig);
            factionStance.InitFromConfig(result);
        }

        private void PrepareAndSave() {
            saveState.timeSysConfig = JsonUtility.ToJson(timeSys.GetConfig());
            saveState.contractManConfig = JsonUtility.ToJson(contractMan.GetConfig());
            saveState.bankConfig = JsonUtility.ToJson(bank.GetConfig());
            saveState.factionStanceConfig = JsonUtility.ToJson(factionStance.GetConfig());
            FGFileIoHandler.SaveFGState(saveSlotName, saveState);
        }

        // Kicks off level transition and marks internal "loading" state.
        public void TransitionToLevel(string goto_scene)
		{
			if (causedInTransitioningLevels)
			{
				return; // Skip if new level already being loaded.
			}
            if (!Application.CanStreamedLevelBeLoaded(goto_scene)) {
                Debug.LogError("Scene name requested does not exist.");
                return;
            }
            // Fails if traveling to unallowed scene.
            if (!saveState.IsValidHome(goto_scene) &&
                !saveState.IsValidArea(goto_scene)) {
                Debug.LogError("Character not allowed to travel to that area.");
                return;
            }

            // Loads next level.
            causedInTransitioningLevels = true;
            SteamVR_LoadLevel.Begin(goto_scene, false, 0.5f, 0f, 0f, 0f, 1f);
        }

        // Transitions level from contract info.
        public void TransitionToLevelFromContract(FGContract contract) {
            if (causedInTransitioningLevels)
			{
				return; // Skip if new level already being loaded.
			}
            string sceneName = contract.SceneName;
            TransitionToLevel(sceneName);
            if (causedInTransitioningLevels) {
                contractForTransition = contract;
            }
        }

        // Spawns wristmenu, inits scene based on type, updates transition state.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.LogWarning("-------> OnSceneLoaded");
            if (!GM.CurrentSceneSettings.QuitReceivers.Contains(gameObject))
            {
                GM.CurrentSceneSettings.QuitReceivers.Add(gameObject);
            }
            StartCoroutine(SpawnWristMenuWithRetries(60, 1f)); // Needs retries bc of default scene loader.
            PrepareAndSave();
            // If scene transitioned due to FreeGriller.
            if (causedInTransitioningLevels) {
                // Calls map loader depending on type of map.
                if (saveState.IsValidArea(scene.name)) {
                    if (contractForTransition == null) {
                        StartCoroutine(TryWithRetries(() => mapLoader.InitArea(saveSlotName, scene.name),
                                        10, 1f));
                    } else {
                        StartCoroutine(TryWithRetries(() => mapLoader.InitAreaFromContract(saveSlotName,
                                                                        contractForTransition),
                                        10, 1f)); 
                        contractForTransition = null;
                    }
                } else if (saveState.IsValidHome(scene.name)) {
                    StartCoroutine(TryWithRetries(() => mapLoader.InitHome(saveSlotName, scene.name),
                                     10, 1f));
                }
                causedInTransitioningLevels = false;
                lastTransitionedToSceneName = scene.name;
            } else {
                // Resets if scenes changed but not through FG.
                lastTransitionedToSceneName = "";
            }
        }

        // Handles QB and scene saving during FG_GM traversals.
        private void OnSceneChangeRequested(bool isLoading) {
            string currSceneName = SceneManager.GetActiveScene().name;
            if (lastTransitionedToSceneName != currSceneName) {
                Debug.LogWarning("Scene we're traveling from was not arrived at through FG_GM " +
                 "- FG_GM last took us to " + lastTransitionedToSceneName);
            }
            // Saves QB state or home scene if we're traveling from Home or Area which we arrived at
            // through FG_GM.
            bool comingFromHomeProper = lastTransitionedToSceneName == currSceneName;
            if (comingFromHomeProper
                    && saveState.IsValidHome(lastTransitionedToSceneName)) {
                SaveHomeSceneConfigToFile(currSceneName);
                SavePlayerQuickbetToFile();
            } else if (comingFromHomeProper && saveState.IsValidArea(lastTransitionedToSceneName)) {
                // TODO: Find place where we updated valid areas / homes?
                SavePlayerQuickbetToFile();
            } else {
                Debug.LogWarning("We're not transitioning from a traveled-to home: " + currSceneName);
            }
        }

        private void Update()
        {}

        private void QUIT() {
            if (saveState.IsValidHome(lastTransitionedToSceneName)) {
                SaveHomeSceneConfigToFile(lastTransitionedToSceneName);
                SavePlayerQuickbetToFile();
            }
            PrepareAndSave();
        }

        // -- Utilities -- \\
        public void SaveHomeSceneConfigToFile(string currSceneName) {
            VaultFile currHomeCfg = new VaultFile();
            if (!VaultSystem.FindAndScanObjectsInScene(currHomeCfg)
                || !FGFileIoHandler.SaveHomeVaultFile(saveSlotName, currSceneName, currHomeCfg))
            {
                Debug.LogError("Failed to scan or write player QB durin level transition.");
            } else {
                Debug.LogWarning("Succeeded saving home name: " + currSceneName);
            }
        }
        public void SavePlayerQuickbetToFile() {
            VaultFile curQb = new VaultFile();
            VaultSystem.FindAndScanObjectsInQuickbelt(curQb);
            // Allows empty loadouts to be saved.
            if (!FGFileIoHandler.SavePlayerQuickbelt(saveSlotName, curQb))
            {
                Debug.LogError("Failed to scan or write player QB durin level transition.");
            }
        }

        public static IEnumerator TryWithRetries(Func<bool> retryMethod, int maxRetries, float delay)
        {
            int attempts = 0;

            while (attempts < maxRetries)
            {
                if (retryMethod()) // Call the provided method dynamically
                {
                    Debug.Log($"Operation succeeded on attempt {attempts + 1}.");
                    yield break; // Exit if successful
                }

                attempts++;
                Debug.LogWarning($"Operation failed. Retrying {attempts}/{maxRetries} in {delay} seconds...");
                yield return new WaitForSeconds(delay);
            }

            Debug.LogError($"Operation failed after {maxRetries} attempts.");
        }
        private IEnumerator SpawnWristMenuWithRetries(int maxRetries, float delay)
        {
            int attempts = 0;
            bool finalCheck = false;

            while (attempts < maxRetries)
            {
                if (finalCheck) {
                    FGWristUi2 wristUI = FindObjectOfType<FGWristUi2>();
                    if (wristUI != null) {
                        Debug.Log($"Wrist menu spawned successfully for sure on attempt {attempts + 1}.");
                        yield break; // Exit coroutine if successful
                    }
                    finalCheck = false;
                }
                if (SpawnWristMenu()) 
                {
                    Debug.Log($"Wrist menu spawned successfully on attempt {attempts + 1}. Checking...");
                    finalCheck = true;
                    yield return new WaitForSeconds(delay); // Need final check
                }
                
                attempts++;
                Debug.LogWarning($"Wrist menu spawn failed. Retrying {attempts}/{maxRetries} in {delay} seconds...");
                yield return new WaitForSeconds(delay);
            }

            Debug.LogError($"Wrist menu failed to spawn after {maxRetries} attempts.");
        }
    }
}
