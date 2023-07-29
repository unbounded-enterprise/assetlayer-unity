using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

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
}
