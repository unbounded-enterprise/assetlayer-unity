using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;


public static class UtilityFunctions
{
    public static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeepChild(child, name);

            if (result != null)
                return result;
        }

        return null;
    }
    public static string GetExpressionValue(List<ExpressionValue> expressionValues, string attributeName)
    {
        var expressionValue = expressionValues
                             .FirstOrDefault(ev => ev.expression.expressionName == attributeName);
        return expressionValue?.value;
    }

    public static string GetExpressionValueByAttributeId(List<ExpressionValue> expressionValues, string attributeId)
    {
        var expressionValue = expressionValues
                             .FirstOrDefault(ev => ev.expressionAttribute.expressionAttributeId == attributeId);
        return expressionValue?.value;
    }
    public static string GetExpressionValueByExpressionId(List<ExpressionValue> expressionValues, string expressionId)
    {
        var expressionValue = expressionValues
                             .FirstOrDefault(ev => ev.expression.expressionId == expressionId);
        return expressionValue?.value;
    }
}


