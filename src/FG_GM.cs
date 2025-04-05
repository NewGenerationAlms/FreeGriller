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
        public FGContractEventsRecorder eventsRecorder;
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
            eventsRecorder = new FGContractEventsRecorder();
            
            // Add event callbacks.
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (!GM.CurrentSceneSettings.QuitReceivers.Contains(gameObject))
            {
                GM.CurrentSceneSettings.QuitReceivers.Add(gameObject);
            }
            
            Init();
            FGExternalLoader.LoadManifestsFromBepinex(); // Load after init so new templates/factions are added.
        }

        private void Init() {
            StartCoroutine(SpawnWristMenuWithRetries(40, 3f));
            InitSaveState();
        }
        private void InitSaveState() {
            try {
                if (saveState == null)
                {
                    FGFileIoHandler.LoadFGState(saveSlotName, out FGState loadedState);
                    if (loadedState != null)
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
                        SaveGameState();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in InitSaveState: {ex.Message}\n{ex.StackTrace}");
            }
        }
        private void InitTimeSysFromSave() {
            if (saveState == null || string.IsNullOrEmpty(saveState.timeSysConfig)) {
                Debug.LogError("Save state or time system config is null. Cannot initialize time system.");
                return;
            }
            FGTimeSystem.Config result = JsonUtility
                .FromJson<FGTimeSystem.Config>(saveState.timeSysConfig);
            timeSys.InitFromConfig(result);
        }
        private void InitContractManFromSave() {
            if (saveState == null || string.IsNullOrEmpty(saveState.contractManConfig)) {
                Debug.LogError("Save state or contract manager config is null. Cannot initialize contract manager.");
                return;
            }
            FGContractManager.Config result = JsonUtility
                .FromJson<FGContractManager.Config>(saveState.contractManConfig);
            contractMan.InitFromConfig(result);
        }
        private void InitBankFromSave() {
            if (saveState == null || string.IsNullOrEmpty(saveState.bankConfig)) {
                Debug.LogError("Save state or bank config is null. Cannot initialize bank.");
                return;
            }
            FGBank.Config result = JsonUtility
                .FromJson<FGBank.Config>(saveState.bankConfig);
            bank.InitFromConfig(result);
        }
        private void InitFactionStanceFromSave() {
            if (saveState == null || string.IsNullOrEmpty(saveState.factionStanceConfig)) {
                Debug.LogError("Save state or faction stance config is null. Cannot initialize faction stance.");
                return;
            }
            FGFactionStance.Config result = JsonUtility
                .FromJson<FGFactionStance.Config>(saveState.factionStanceConfig);
            factionStance.InitFromConfig(result);
        }

        private void SaveGameState() {
            saveState.timeSysConfig = JsonUtility.ToJson(timeSys.GetConfig());
            saveState.contractManConfig = JsonUtility.ToJson(contractMan.GetConfig());
            saveState.bankConfig = JsonUtility.ToJson(bank.GetConfig());
            saveState.factionStanceConfig = JsonUtility.ToJson(factionStance.GetConfig());
            FGFileIoHandler.SaveFGState(saveSlotName, saveState);
        }

        private bool VerifyTransitionOk(string goto_scene)
        {
            if (causedInTransitioningLevels)
            {
                return false; // Skip if new level already being loaded.
            }
            if (!Application.CanStreamedLevelBeLoaded(goto_scene))
            {
                Debug.LogError("Scene name requested does not exist.");
                return false;
            }
            if (!saveState.IsValidHome(goto_scene) && !saveState.IsValidArea(goto_scene))
            {
                Debug.LogError("Character not allowed to travel to that area.");
                return false;
            }
            return true;
        }

        private void HandleContractCompletion()
        {
            // Checks if active contracts completed. Constraints checkers know to ignore
            // if no contracts had relevant info updated.
            // Implicitly checks that traveling between scenes is safe (through travel area).
            contractMan.CheckContractCompletionOnAreaExit();
            eventsRecorder.WipeSession();
            eventsRecorder.OnEventRegistered -= contractMan.EvaluateAndUpdateActiveContractsOnEvent;
        }

        // Kicks off level transition and marks internal "loading" state.
        public void TransitionToLevel(string goto_scene)
		{
            if (!VerifyTransitionOk(goto_scene))
            {
                return;
            }
            VaultSaveBeforeTransition(goto_scene);
            HandleContractCompletion();

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
            StartCoroutine(SpawnWristMenuWithRetries(60, 1f));
            SaveGameState();
            // If scene transitioned due to FreeGriller.
            if (causedInTransitioningLevels) {
                eventsRecorder.StartSession();
                eventsRecorder.OnEventRegistered += contractMan.EvaluateAndUpdateActiveContractsOnEvent;
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
        // Assumes you always "travel-to" home and don't start the game there.
        private void VaultSaveBeforeTransition(string goto_scene) {
            string currSceneName = SceneManager.GetActiveScene().name;
            if (lastTransitionedToSceneName != currSceneName) {
                Debug.LogWarning("Scene we're traveling from was not arrived at through FG_GM " +
                 "- FG_GM last took us to " + lastTransitionedToSceneName);
            }
            // Saves QB state or home scene if we're traveling from Home or Area which we arrived at
            // through FG_GM.
            bool comingFromFGTraveledPlace = lastTransitionedToSceneName == currSceneName;
            if (comingFromFGTraveledPlace) {
                if (saveState.IsValidHome(lastTransitionedToSceneName)) {
                    SaveHomeSceneConfigToFile(currSceneName);
                    SavePlayerQuickbetToFile();
                } else if (saveState.IsValidArea(lastTransitionedToSceneName)) {
                    SavePlayerQuickbetToFile();
                } else {
                    Debug.LogError("We're transitioning from an FG traveled-to scene that's neither area nor home: " + currSceneName);
                }
            } else {
                Debug.LogWarning("We're not transitioning from an FG traveled-to scene: " + currSceneName);
            }
        }

        private void QUIT() {
            if (saveState.IsValidHome(lastTransitionedToSceneName)) {
                SaveHomeSceneConfigToFile(lastTransitionedToSceneName);
                SavePlayerQuickbetToFile();
            }
            SaveGameState();
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
            // False means quickbelt is empty.
            if (!VaultSystem.FindAndScanObjectsInQuickbelt(curQb)) {
                Debug.LogWarning("Empty quickbelt!");
            }
            // Allows empty loadouts to be saved.
            if (!FGFileIoHandler.SavePlayerQuickbelt(saveSlotName, curQb))
            {
                Debug.LogError("Failed to write player QB.");
                return;
            }
            Debug.LogWarning("Succeeded saving player QB.");
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
        private bool SpawnWristMenu() {
            FVRObject wristUiFvr;
            if (!IM.OD.TryGetValue(wristUiSpawnId, out wristUiFvr))
            {
                Debug.LogError(wristUiSpawnId + " not found in IM.OD!");
                return false;
            }
            try
            {
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(wristUiFvr.GetGameObject(), Vector3.zero, Quaternion.identity);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to spawn wrist menu: {ex.Message}");
                return false;
            }
            Debug.LogWarning($"{wristUiSpawnId} supposedly spawned");
            return true;
        }
        private IEnumerator SpawnWristMenuWithRetries(int maxRetries, float delay)
        {
            int attempts = 0;
            bool finalCheck = false;

            while (attempts < maxRetries)
            {
                if (finalCheck) {
                    FGWristUi2[] wristUIs = FindObjectsOfType<FGWristUi2>();
                    if (wristUIs.Length > 0) {
                        for (int i = 1; i < wristUIs.Length; i++) {
                            Destroy(wristUIs[i].gameObject); // Destroy additional instances
                        }
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
