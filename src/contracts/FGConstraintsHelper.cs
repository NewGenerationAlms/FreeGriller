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
}

public interface IContractConstraint
{
    void EvaluateAndUpdateContract(ContractEvaluationContext context);
    string GetDescription(); // Useful for UI display.
    void Init();
    void Exit();
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

    public static void EvaluateAndUpdateContractHelper
                        (ContractEvaluationContext context,
                            string constraintID, bool isConstraintMet,
                            bool isConstraintFailed)
    {
        int constraintIndex = context.myContract.ConstraintsAndRewards
                                .FindIndex(x => x.ConstraintID == constraintID);
        if (constraintIndex == -1)
        {
            Debug.LogError($"{constraintID} constraint not found in contract.");
            return;
        }
        FGContract.ConstraintAndReward constraintData = context.myContract.ConstraintsAndRewards[constraintIndex];
        bool isItMet = isConstraintMet;
        bool isItFailed = isConstraintFailed;
        context.myContract.ConstraintsAndRewards[constraintIndex] = 
            CreateConstraintAndReward(constraintData, isItMet, isItFailed);
    }
}

public class GrillViaProjectileConstraint : IContractConstraint
{
    public void EvaluateAndUpdateContract(ContractEvaluationContext context)
    {
        bool isItMet = false;
        bool isItFailed = false;
        ConstraintFactory
            .EvaluateAndUpdateContractHelper(context, "GrillViaProjectile", isItMet, isItFailed);
    }
    public string GetDescription() => "Eliminate the target with a projectile.";
    public void Init() { }
    public void Exit() { }
}

public class GrillAllTargetsConstraint : IContractConstraint
{
    public void EvaluateAndUpdateContract(ContractEvaluationContext context)
    {
        bool isItMet = false;
        bool isItFailed = false;
        ConstraintFactory
            .EvaluateAndUpdateContractHelper(context, "GrillAllTargets", isItMet, isItFailed);
    }
    public string GetDescription() => "Eliminate all targets.";
    public void Init() { }
    public void Exit() { }
}

} // namespace NGA