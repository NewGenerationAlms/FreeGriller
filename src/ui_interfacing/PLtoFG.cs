using System.Collections.Generic;
using UnityEngine;
using FistVR;
using Sodalite.Api;

namespace NGA {

// Represents configuration data for Sosigs.
public class PLtoFG
{
    public static bool TransformPLtoFG() {
        if (GM.IsAsyncLoading) {
			Debug.LogWarning("TransformPLtoFG Waiting IsAsyncLoading aka vault loading.");
			return false; // retry
		}
        NpcSpawnPt[] allNpcPts = GameObject.FindObjectsOfType<NpcSpawnPt>();
        if (allNpcPts.Length == 0)
        {
            Debug.LogWarning("No NpcSpawnPt objects found in the scene.");
            return true; // exit
        }
        List<NpcSpawnPt> genericPtList = new List<NpcSpawnPt>();
        List<NpcSpawnPt> mySpawnedPtList = new List<NpcSpawnPt>();
        FGTargetPosse.PosseConfig posseConfig = new FGTargetPosse.PosseConfig();
        
        foreach (var npcPt in allNpcPts) {
            if (string.IsNullOrEmpty(npcPt.tagId)) {
                genericPtList.Add(npcPt);
            } else {
                switch (npcPt.tagId.ToLower()) {
                    case "extra":
                        posseConfig.Extras.Add(CreateSosigMandate(npcPt, false, false, true));
                        mySpawnedPtList.Add(npcPt);
                        break;
                    case "target":
                        posseConfig.Targets.Add(CreateSosigMandate(npcPt, true, false, false));
                        mySpawnedPtList.Add(npcPt);
                        break;
                    case "guard":
                        posseConfig.Guards.Add(CreateSosigMandate(npcPt, false, true, false));
                        mySpawnedPtList.Add(npcPt);
                        break;
                    default:
                        genericPtList.Add(npcPt);
                        break;
                }
            }
        }

        // Set all non-target posse NpcSpawnPts to spawn.
        foreach(var genericNpc in genericPtList) {
            genericNpc.setActive(true);
        }
        // Hide the spawned npc points.
        foreach(var spawnedNpc in mySpawnedPtList) {
            spawnedNpc.gameObject.SetActive(false);
        }

        GameObject posseObject = new GameObject("FGTargetPosse");
        FGTargetPosse targetPosse = posseObject.AddComponent<FGTargetPosse>();
        targetPosse.AddPosseConfig(posseConfig);

        return true;
    }

    private static FGSosigMandate CreateSosigMandate(NpcSpawnPt npcPt, 
                                    bool isTarget, bool isGuard, bool isExtra) {
        return new FGSosigMandate {
            SpawnPoint = npcPt.transform,
            Path = null, // Assign path if applicable
            Manifest = new FGSosigManifest {
                UniqueId = npcPt.tagId,
                IFF = (int)npcPt.GetMyIFF(),
                SosigOrder = (int)npcPt.GetMyOrders(),
                EnemyId = (int)npcPt.GetEnemyID(),
                IsTarget = isTarget,
                IsGuard = isGuard,
                IsExtra = isExtra
            }
        };
    }
}

} // namespace NGA