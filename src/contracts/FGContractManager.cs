using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

namespace NGA {
public class FGContractManager : MonoBehaviour
{
    public static FGContractManager Instance;
    private List<FGContract> activeContracts = new List<FGContract>();
    private List<FGContract> availableContracts = new List<FGContract>();

    public struct Config {
        public List<FGContract> activeContracts;
        public List<FGContract> availableContracts;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitFromConfig(Config config)
    {
        // Ensure activeContracts and availableContracts are not null
        if (config.activeContracts == null) {
            Debug.LogWarning("activeContracts is null, initializing as empty list.");
            config.activeContracts = new List<FGContract>(); // Initialize as empty list
        }
        
        if (config.availableContracts == null) {
            Debug.LogWarning("availableContracts is null, initializing as empty list.");
            config.availableContracts = new List<FGContract>(); // Initialize as empty list
        }

        // Assign the lists to the class variables
        activeContracts = config.activeContracts;
        availableContracts = config.availableContracts;

        // Prepare contracts in activeContracts list
        foreach (var c in activeContracts)
        {
            if (c != null) {
                c.PrepareFromLoad();
            } else {
                Debug.LogWarning("Found null contract in activeContracts list.");
            }
        }

        // Prepare contracts in availableContracts list
        foreach (var c in availableContracts)
        {
            if (c != null) {
                c.PrepareFromLoad();
            } else {
                Debug.LogWarning("Found null contract in availableContracts list.");
            }
        }
    }

    public Config GetConfig() {
        foreach (var c in activeContracts) {
            c.PrepareForSave();
        }
        foreach (var c in availableContracts) {
            c.PrepareForSave();
        }
        return new Config {
            activeContracts = activeContracts,
            availableContracts = availableContracts,
        };
    }

    public List<FGContract> GetActiveContracts() {
        return activeContracts;
    }

    public List<FGContract> GetAvailableContracts() {
        return availableContracts;
    }

    void Start () {
        FGContractTemplateFactory.RegisterDefaultContractTemplates();
    }

    private void OnEnable()
    {
        // TODO: Depending on if we want it to run in background, this should be
        // linked to start signal from FG_GM.

        // Subscribe to the GameTimeManager event when this script is enabled
        FGTimeSystem.Instance.OnTimeAdvanced += CheckContractExpirations;
        FGTimeSystem.Instance.OnTimeAdvanced += MaybeAddNewContract;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks or erroneous updates when the script is disabled
        FGTimeSystem.Instance.OnTimeAdvanced -= CheckContractExpirations;
        FGTimeSystem.Instance.OnTimeAdvanced -= MaybeAddNewContract;
    }

    // You can still call this method to generate new contracts
    private void GenerateContract(string factionID, float forPlayerReputation)
    {
        FGContractTemplate template = FGContractTemplateFactory.GetTemplateForFactionAndReputation(factionID, forPlayerReputation);

        if (template == null)
        {
            Debug.LogWarning($"No contract template found for faction {factionID} with reputation {forPlayerReputation}");
            return;
        }
        

        FGContract newContract = template.GenerateContract();
        if (newContract == null)
        {
            Debug.LogError($"Failed to generate contract from template {template.TemplateID} for faction {factionID}");
            return;
        }
        // TODO: Add an events system to notify when new contract added.
        availableContracts.Add(newContract);

        Debug.Log($"Generated contract from template: {newContract.DisplayName} for faction {newContract.HiringFactionID}");
    }

    public void AcceptContract(int uniqueID)
    {
        // Find the contract in the available contracts list using the uniqueID.
        FGContract contractToAccept = availableContracts.FirstOrDefault(c => c.uniqueID == uniqueID);

        if (contractToAccept != null)
        {
            availableContracts.Remove(contractToAccept);
            activeContracts.Add(contractToAccept);
            Debug.Log($"Contract with ID {uniqueID} has been accepted.");
        }
        else
        {
            Debug.LogWarning($"No contract found with uniqueID {uniqueID}.");
        }
    }

    private void CheckContractExpirations(DateTime currentTime)
    {
        for (int i = activeContracts.Count - 1; i >= 0; i--)
        {
            if (IsContractExpired(activeContracts[i]))
            {
                Debug.Log($"Contract expired: {activeContracts[i].DisplayName}");
                activeContracts.RemoveAt(i);
                // TOOD: Add an events system for new contract expired so Notifiaction class knows.
            }
        }
    }

    private void MaybeAddNewContract(DateTime currentTime) {
        // TODO: Beware magic number.
        if (activeContracts.Count >= 3) {
            return;
        }
        GenerateNewContract();
    }
    
    private bool IsContractExpired(FGContract contract)
    {
        DateTime inGameTime = FGTimeSystem.Instance.CurrentTime;
        DateTime contractExpirationTime = contract.ExpirationDateTime;

        return inGameTime >= contractExpirationTime; // Expired if current time is past or equal to expiration
    }

    private void GenerateNewContract()
    {
        // TODO: Connect this to Factions class to know who to generate quest from and what
        // reputation level quest to request.
        GenerateContract("Hollys", /*reputation=*/0.0f);
    }

    // TODO
    private void CheckContractsForCompletion()
    {
        ContractEvaluationContext context = new ContractEvaluationContext(); // TODO Build it.
        for (int i = activeContracts.Count - 1; i >= 0; i--)
        {
            // TODO: Perhaps only contracts from valid map being extracted from should be checked
            // it would stop us from having a more generic quest design.
            FGContract contract = activeContracts[i];
            context.myContract = contract;
            bool contractCompleted = true;
            int totalReward = 0;

            foreach (var constraintData in contract.ConstraintsAndRewards)
            {
                IContractConstraint constraint = ConstraintFactory.GetConstraint(constraintData.ConstraintID);

                if (constraint == null)
                {
                    Debug.LogWarning($"Constraint {constraintData.ConstraintID} not found.");
                    continue;
                }

                bool success = constraint.IsSatisfied(context);
                contract.ConstraintsAndRewards[i] = new FGContract.ConstraintAndReward
                {
                    ConstraintID = constraintData.ConstraintID,
                    constraintSuccess = success,
                    constraintViolated = !success,
                    rewardAddedIfSucceed = constraintData.rewardAddedIfSucceed,
                    rewardSubtractedIfFail = constraintData.rewardSubtractedIfFail
                };

                if (success)
                {
                    totalReward += constraintData.rewardAddedIfSucceed;
                }
                else
                {
                    totalReward -= constraintData.rewardSubtractedIfFail;
                    // TODO: Contract should be completed on fail or win of killalltargets constraint.
                    // Likely from the new optional value thing
                    contractCompleted = false;
                }
            }

            if (contractCompleted)
            {
                // TODO: Payment boy
                // TODO: Pay no less than 0?
                // Reputation boy
                Debug.Log($"Contract '{contract.DisplayName}' completed! Total reward: {totalReward}");
                activeContracts.RemoveAt(i);
            }
        }
    }
}
} // namespace NGA