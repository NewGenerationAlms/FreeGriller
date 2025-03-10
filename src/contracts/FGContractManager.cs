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
    private List<FGContract> completedContracts = new List<FGContract>();

    public struct Config {
        public List<FGContract> activeContracts;
        public List<FGContract> availableContracts;
        public List<FGContract> completedContracts;
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
            config.activeContracts = new List<FGContract>();
        }
        if (config.availableContracts == null) {
            Debug.LogWarning("availableContracts is null, initializing as empty list.");
            config.availableContracts = new List<FGContract>();
        }
        if (config.completedContracts == null) {
            Debug.LogWarning("completedContracts is null, initializing as empty list.");
            config.completedContracts = new List<FGContract>();
        }
        activeContracts = config.activeContracts;
        availableContracts = config.availableContracts;
        completedContracts = config.completedContracts;

        // Prepare contracts from load.
        foreach (var contract in activeContracts)
        {
            if (contract != null) {
                contract.PrepareFromLoad();
            } else {
                Debug.LogWarning("Found null contract in activeContracts list.");
            }
        }
        foreach (var contract in availableContracts)
        {
            if (contract != null) {
                contract.PrepareFromLoad();
            } else {
                Debug.LogWarning("Found null contract in availableContracts list.");
            }
        }
        foreach (var contract in completedContracts)
        {
            if (contract != null) {
                contract.PrepareFromLoad();
            } else {
                Debug.LogWarning("Found null contract in completedContracts list.");
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
        foreach (var c in completedContracts) {
            c.PrepareForSave();
        }
        return new Config {
            activeContracts = activeContracts,
            availableContracts = availableContracts,
            completedContracts = completedContracts
        };
    }

    public List<FGContract> GetActiveContracts() {
        return activeContracts;
    }

    public List<FGContract> GetAvailableContracts() {
        return availableContracts;
    }

    void Start () {
        // Default contract templates loaded through FGExternalLoader.
    }

    private void OnEnable()
    {
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
        FGContractTemplate template = 
            FGContractTemplateFactory.GetTemplateForFactionAndReputation(factionID, forPlayerReputation);
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
            activeContracts.Add(contractToAccept);
            availableContracts.Remove(contractToAccept);
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
                AddToCompletedContracts(activeContracts[i]);
                activeContracts.RemoveAt(i);
                // TOOD: Add an events system for new contract expired so Notifiaction class knows.
            }
        }
        for (int i = availableContracts.Count - 1; i >= 0; i--)
        {
            if (IsContractExpired(availableContracts[i]))
            {
                Debug.Log($"Contract expired: {availableContracts[i].DisplayName}");
                availableContracts.RemoveAt(i);
            }
        }
    }

    private void MaybeAddNewContract(DateTime currentTime) {
        // TODO: Beware magic number.
        if (availableContracts.Count >= 3) {
            return;
        }
        GenerateNewContract();
    }

    private void AddToCompletedContracts(FGContract contract)
    {
        // Appends to the front of the list. Always maintain max 5 completed contracts, removing oldest.
        completedContracts.Insert(0, contract);
        int maxCompletedContracts = 10;
        while (completedContracts.Count > maxCompletedContracts)
        {
            completedContracts.RemoveAt(maxCompletedContracts);
        }
    }
    
    private bool IsContractExpired(FGContract contract)
    {
        DateTime inGameTime = FGTimeSystem.Instance.CurrentTime;
        DateTime contractExpirationTime = contract.ExpirationDateTime;
        // Expired if current time is past or equal to expiration
        return inGameTime >= contractExpirationTime;
    }

    private void GenerateNewContract()
    {
        // TODO: Connect this to Factions class to know who to generate quest from and what
        // reputation level quest to request.
        GenerateContract("Hollys", /*reputation=*/0.0f);
    }

    // NOTE: Contracts only marked as completed/failed when changing scenes.
    public void EvaluateAndUpdateActiveContractsOnEvent(FGContractEvent contractEvent) {
        ContractEvaluationContext context = new ContractEvaluationContext();
        context.eventsInSession = FG_GM.Instance.eventsRecorder.CurrentSessionEvents;
        for (int contractIx = activeContracts.Count - 1; contractIx >= 0; contractIx--)
        {
            FGContract contract = activeContracts[contractIx];
            context.myContract = contract;
            for (int constraintIx = 0; constraintIx < contract.ConstraintsAndRewards.Count; constraintIx++)
            {
                var constraintData = contract.ConstraintsAndRewards[constraintIx];
                IContractConstraint constraintChecker = 
                    ConstraintFactory.GetConstraint(constraintData.ConstraintID);
                if (constraintChecker == null)
                {
                    Debug.LogWarning($"Constraint {constraintData.ConstraintID} not found.");
                    continue;
                }

                constraintChecker.EvaluateAndUpdateContract(context);
            }
        }
    }


    public void CheckContractCompletionOnAreaExit()
    {
        ContractEvaluationContext context = new ContractEvaluationContext();
        context.eventsInSession = FG_GM.Instance.eventsRecorder.CurrentSessionEvents;
        for (int contractIx = activeContracts.Count - 1; contractIx >= 0; contractIx--)
        {
            FGContract contract = activeContracts[contractIx];
            context.myContract = contract;
            context.isFinalCheck = true;
            bool contractCompleted = false;
            bool contractFailed = false;
            int numMandatoryConstraints = contract.ConstraintsAndRewards.Count(c => c.optional == false);
            int numMetMandatoryConstraints = 0;
            int totalReward = 0;

            for (int constraintIx = 0; constraintIx < contract.ConstraintsAndRewards.Count; constraintIx++)
            {
                var constraintData = contract.ConstraintsAndRewards[constraintIx];
                IContractConstraint constraintChecker = 
                    ConstraintFactory.GetConstraint(constraintData.ConstraintID);
                if (constraintChecker == null)
                {
                    Debug.LogWarning($"Constraint {constraintData.ConstraintID} not found.");
                    continue;
                }

                constraintChecker.EvaluateAndUpdateContract(context);
                var updatedConstraintData = contract.ConstraintsAndRewards[constraintIx];
                if (updatedConstraintData.constraintSuccess)
                {
                    totalReward += constraintData.rewardAddedIfSucceed;
                    if (constraintData.optional == false)
                    {
                        numMetMandatoryConstraints++;
                    }
                }
                else if (updatedConstraintData.constraintViolated)
                {
                    // NOTE: It's possible for a contract to not have anything relevant to evaluate,
                    // thus not a failure nor success.
                    totalReward -= constraintData.rewardSubtractedIfFail;
                    if (updatedConstraintData.optional == false)
                    {
                        Debug.LogWarning($"Contract '{contract.DisplayName}' failed due to constraint {constraintData.ConstraintID}");
                        contractFailed = true;
                        break;
                    }
                } else if (ConstraintFactory.IsContractInAnyPosse(contract) 
                            && updatedConstraintData.optional == false)
                {
                    // Mandatory constraint not met upon contract completion when it could've
                    // been completed.
                    contractFailed = true;
                    break;
                }
            }
            if (numMetMandatoryConstraints < numMandatoryConstraints)
            {
                contractFailed = true;
            } else {
                contractCompleted = true;
            }
            if (totalReward < 0)
            {
                totalReward = 0;
            }

            if (contractCompleted)
            {
                // Pay the player, always at least 0.
                FG_GM.Instance.bank.IncrementPlyBalance(totalReward);
                // Award all ReputationRewards in the contract.
                foreach (var repReward in contract.ReputationRewards)
                {
                    FG_GM.Instance.factionStance.TryAdjustReputation(repReward.FactionID, repReward.Rep);
                }
                Debug.Log($"Contract '{contract.uniqueID}' completed! Total reward: {totalReward}");
                AddToCompletedContracts(contract);
                activeContracts.RemoveAt(contractIx);
            } else if (contractFailed)
            {
                AddToCompletedContracts(contract);
                activeContracts.RemoveAt(contractIx);
            }
        }
    }
}
} // namespace NGA