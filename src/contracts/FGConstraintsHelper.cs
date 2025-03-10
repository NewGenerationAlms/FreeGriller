using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NGA {

public class ContractEvaluationContext
{
    public FGContract myContract;
    public List<FGContractEvent> eventsInSession;
    public bool isFinalCheck = false;
}

public interface IContractConstraint
{
    void EvaluateAndUpdateContract(ContractEvaluationContext context);
    string GetDescription(); // Useful for UI display.
}

public static class ConstraintFactory
{
    private static Dictionary<string, Func<IContractConstraint>> constraintRegistry = new Dictionary<string, Func<IContractConstraint>>
    {
        { "GrillViaProjectile", () => new GrillViaProjectileConstraint() },
        { "GrillAllTargets", () => new GrillAllTargetsConstraint() }
    };

    public static IContractConstraint GetConstraint(string id)
    {
        return constraintRegistry.ContainsKey(id) ? constraintRegistry[id]() : null;
    }

    public static void AddConstraint(string id, Func<IContractConstraint> constructor)
    {
        if (!constraintRegistry.ContainsKey(id))
        {
            constraintRegistry[id] = constructor;
        }
        else
        {
            Debug.LogWarning($"Constraint '{id}' is already registered.");
        }
    }

    public static FGContract.ConstraintAndReward CreateConstraintAndReward(FGContract.ConstraintAndReward constraintData, bool isItMet, bool isItFailed)
    {
        return new FGContract.ConstraintAndReward
        {
            ConstraintID = constraintData.ConstraintID,
            optional = constraintData.optional,
            constraintSuccess = isItMet,
            constraintViolated = isItFailed,
            rewardAddedIfSucceed = constraintData.rewardAddedIfSucceed,
            rewardSubtractedIfFail = constraintData.rewardSubtractedIfFail
        };
    }

    public static void UpdateConstraintInContract
                        (ContractEvaluationContext context,
                            string constraintID, bool isConstraintMet,
                            bool isConstraintFailed)
    {
        var constraintIndexes = context.myContract.ConstraintsAndRewards
                                .FindAll(x => x.ConstraintID == constraintID);
        if (constraintIndexes.Count == 0)
        {
            Debug.LogError($"{constraintID} constraint not found in contract.");
            return;
        }
        foreach (var constraintData in constraintIndexes)
        {
            int index = context.myContract.ConstraintsAndRewards.IndexOf(constraintData);
            context.myContract.ConstraintsAndRewards[index] = 
                CreateConstraintAndReward(constraintData, isConstraintMet, isConstraintFailed);
        }
    }

    public static bool IsConstraintInConstract(FGContract contract, string constraintID)
    {
        return contract.ConstraintsAndRewards.Exists(x => x.ConstraintID == constraintID);
    }

    public static bool AreThereTargetsInScene(){
        return FG_GM.Instance.mapLoader.TargetPosses.Count > 0;
    }
    public static bool IsContractInAnyPosse(FGContract contract)
    {
        foreach (FGTargetPosse posse in FG_GM.Instance.mapLoader.TargetPosses)
        {
            if (posse.contract.uniqueID == contract.uniqueID)
            {
                return true;
            }
        }
        return false;
    }
}

public class GrillViaProjectileConstraint : IContractConstraint
{
    public string constraintKey = "GrillViaProjectile";
    public void EvaluateAndUpdateContract(ContractEvaluationContext context)
    {
        if (!ConstraintFactory.IsConstraintInConstract(context.myContract, constraintKey)
            || !ConstraintFactory.AreThereTargetsInScene() 
            || !ConstraintFactory.IsContractInAnyPosse(context.myContract)) {
            return;
        }
        FGTargetPosse contractPosse = FG_GM.Instance.mapLoader.TargetPosses
                                        .Find(x => x.contract.uniqueID 
                                                == context.myContract.uniqueID);
        int numTargets = contractPosse.trackedTargets.Count;
        List<FGContractEvent> sosigKillEvents = 
            context.eventsInSession.FindAll(x => x.EventKey == "OnSosigKill");
        int numDeadTargetsTotal = 0;
        
        HashSet<Sosig> targetsDiedByProjectile = new HashSet<Sosig>();

        foreach (FGContractEvent contractEvent in sosigKillEvents)
        {
            if (contractEvent.OnSosigKill == null)
            {
                continue;
            }
            Sosig sosig = contractEvent.OnSosigKill.Sosig;
            if (sosig == null)
            {
                Debug.LogError("Unexpected sosig null OnSosigKill.");
                continue;
            }
            if (targetsDiedByProjectile.Contains(sosig))
            {
                Debug.LogWarning("Sosig already killed by projectile - unexpected event again.");
                continue;
            }
            FGTrackedSosig trackedSosig = contractPosse.FindSosig(sosig);
            if (trackedSosig?.Manifest?.IsTarget != true)
            {
                continue;
            }
            numDeadTargetsTotal++;
            Damage.DamageClass diedFromClass = sosig.GetDiedFromClass();
            if (diedFromClass == Damage.DamageClass.Projectile)
            {
                targetsDiedByProjectile.Add(sosig);
            }
            
        }
        bool isItMet = numDeadTargetsTotal == targetsDiedByProjectile.Count;
        bool isItFailed = false;
        if (context.isFinalCheck) {
            isItFailed = numDeadTargetsTotal != targetsDiedByProjectile.Count;
        }
        ConstraintFactory
            .UpdateConstraintInContract(context, constraintKey, isItMet, isItFailed);
    }
    public string GetDescription() => "Eliminate all targets with a projectile.";
}

public class GrillAllTargetsConstraint : IContractConstraint
{
    public string constraintKey = "GrillAllTargets";
    public void EvaluateAndUpdateContract(ContractEvaluationContext context)
    {
        if (!ConstraintFactory.IsConstraintInConstract(context.myContract, constraintKey)
            || !ConstraintFactory.AreThereTargetsInScene() 
            || !ConstraintFactory.IsContractInAnyPosse(context.myContract)) {
            return;
        }
        FGTargetPosse contractPosse = FG_GM.Instance.mapLoader.TargetPosses
                                        .Find(x => x.contract.uniqueID 
                                                == context.myContract.uniqueID);
        int numTargets = contractPosse.trackedTargets.Count;
        List<FGContractEvent> sosigKillEvents = 
            context.eventsInSession.FindAll(x => x.EventKey == "OnSosigKill");
        HashSet<Sosig> targetsDied = new HashSet<Sosig>();

        foreach (FGContractEvent contractEvent in sosigKillEvents)
        {
            if (contractEvent.OnSosigKill == null)
            {
                continue;
            }
            Sosig sosig = contractEvent.OnSosigKill.Sosig;
            if (sosig == null)
            {
                Debug.LogError("Unexpected sosig null OnSosigKill.");
                continue;
            }
            if (targetsDied.Contains(sosig))
            {
                Debug.LogWarning("Sosig already killed - unexpected event again.");
                continue;
            }
            FGTrackedSosig trackedSosig = contractPosse.FindSosig(sosig);
            if (trackedSosig?.Manifest?.IsTarget != true)
            {
                continue;
            }
            targetsDied.Add(sosig);
        }
        bool isItMet = numTargets == targetsDied.Count;
        bool isItFailed = false;
        if (context.isFinalCheck) {
            isItFailed = numTargets != targetsDied.Count;
        }
        ConstraintFactory
            .UpdateConstraintInContract(context, constraintKey, isItMet, isItFailed);
    }
    public string GetDescription() => "Eliminate all targets.";
}

} // namespace NGA