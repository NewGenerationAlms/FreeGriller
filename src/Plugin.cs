using BepInEx;
using BepInEx.Logging;
using FistVR;
using UnityEngine;
using HarmonyLib;
using BepInEx.Configuration;
using Sodalite.ModPanel;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using static FistVR.ItemSpawnerV2;

namespace NGA
{
    //[BepInAutoPlugin]
    [BepInPlugin("NGA.FreeGrillerPatchy", "FreeGrillerPatchy", "0.0.1")]
    [BepInDependency("nrgill28.Sodalite", "1.4.1")]
    [BepInProcess("h3vr.exe")]
    public partial class FreeGrillerPatchy : BaseUnityPlugin
    {
		private static ConfigEntry<bool> GameEnabled;
        private static ConfigEntry<float> SprintSpeedCap;
        

        private void Awake()
        {
            Logger = base.Logger;

            Harmony harmony = new Harmony("NGA.FreeGrillerPatchy");
            Logger.LogMessage("New harmony");
            SetUpConfigFields();
            Logger.LogMessage("Setted the fields");
            harmony.PatchAll();
            Logger.LogMessage($"Hello, world! Sent from NGA.FreeGrillerPatchy");
        }

        // Assigns player-set variables.
        private void SetUpConfigFields()
        {
            // Overall.
            GameEnabled = Config.Bind<bool>("Overall", "ON/OFF", true, "Completely enable/disable mod");
            SprintSpeedCap = Config.Bind<float>("TwinStickArmSwing",
                                            "Sprint Added speed v2",
                                            2f, 
                                            new ConfigDescription("Sprint speed soft-cap on armswing. Bigger than jog.", 
                                            new AcceptableValueFloatRangeStep(0f, 20f, 0.05f), new object[0]));
        }

        private static bool CheckSkip() {
            return !GameEnabled.Value; //if (CheckSkip()) return;
        }

        [HarmonyPatch(typeof(GM))]
        [HarmonyPatch("Awake")]
        public class FG_GM_Initializer
        {
            // Creates singleton Manager that will live on across scenes.
            private static void Postfix(GM __instance)
            {
                if (FG_GM.Instance == null)
                {
                    GameObject fgGMObj = new GameObject("FG_GM");
                    fgGMObj.AddComponent<FG_GM>();                    
                    Object.DontDestroyOnLoad(fgGMObj);
                }
            }
        }
        
        // For scenes that have default vault files, we need to skip this step
        // if we're transitioning with FG_GM.
        [HarmonyPatch(typeof(FVRSceneSettings))]
		[HarmonyPatch("LoadDefaultSceneRoutine")]
		class FVRSceneSettingsLoadDefaultSceneRoutineHook
		{
            static bool Prefix(FVRSceneSettings __instance) {
                if (FG_GM.Instance.causedInTransitioningLevels
                    || FG_GM.Instance.lastTransitionedToSceneName == SceneManager.GetActiveScene().name) {
                    return false;
                }
                return true;
            }
		}



        internal new static ManualLogSource Logger { get; private set; }
    }
}
