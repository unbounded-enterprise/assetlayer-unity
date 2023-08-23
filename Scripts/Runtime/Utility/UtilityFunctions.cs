using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using Assetlayer.UnitySDK;
using System.Text.RegularExpressions;
using System;

namespace Assetlayer.UtilityFunctions
{


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

        public static string GetCurrentPlatformExpressionAttribute()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "AssetBundleStandaloneWindows";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return "AssetBundleStandaloneOSX";
                case RuntimePlatform.IPhonePlayer:
                    return "AssetBundleiOS";
                case RuntimePlatform.Android:
                    return "AssetBundleAndroid";
                case RuntimePlatform.WebGLPlayer:
                    return "AssetBundleWebGL";
                default:
                    return "AssetBundleStandaloneWindows";
            }
        }
        public static string GetExpressionValue(List<ExpressionValue> expressionValues, string expressionName)
        {
            var expressionValue = expressionValues
                                 .FirstOrDefault(ev => ev.expression.expressionName == expressionName);
            return expressionValue?.value;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Note: This is a simple regex for validation. You might find various regex patterns online 
                // with varying levels of complexity based on what you consider a 'valid' email address.
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static string GetExpressionValueAssetBundle(List<ExpressionValue> expressionValues, string expressionName)
        {
            string currentPlatformAttributeName = GetCurrentPlatformExpressionAttribute();
            var expressionValue = expressionValues
                                 .FirstOrDefault(ev => ev.expression.expressionName == expressionName && ev.expressionAttribute.expressionAttributeName == currentPlatformAttributeName);
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

        public static string GetExpressionValueByExpressionIdAssetBundle(List<ExpressionValue> expressionValues, string expressionId)
        {
            string currentPlatformAttributeName = GetCurrentPlatformExpressionAttribute();
            var expressionValue = expressionValues
                                 .FirstOrDefault(ev => ev.expression.expressionId == expressionId && ev.expressionAttribute.expressionAttributeName == currentPlatformAttributeName);
            return expressionValue?.value;
        }

        public static bool IsSceneBundle(AssetBundle bundle)
        {
            if (bundle == null) return false;

            string[] allAssetNames = bundle.GetAllAssetNames();

            foreach (string assetName in allAssetNames)
            {
                if (assetName.EndsWith(".unity"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}


