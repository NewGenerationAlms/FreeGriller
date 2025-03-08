using System;
using FistVR;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace NGA {

public class ContractEvaluationContext
{
    public FGContract myContract;
}

public interface IContractConstraint
{
    bool IsSatisfied(ContractEvaluationContext context);
    string GetDescription(); // Useful for UI display
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
}

public class GrillViaProjectileConstraint : IContractConstraint
{
    public bool IsSatisfied(ContractEvaluationContext context)
    {
        return true;
    }
    public string GetDescription() => "Eliminate the target with a projectile.";
}

public class GrillAllTargetsConstraint : IContractConstraint
{
    public bool IsSatisfied(ContractEvaluationContext context)
    {
        return true;
    }
    public string GetDescription() => "Eliminate all targets.";
}

} // namespace NGA