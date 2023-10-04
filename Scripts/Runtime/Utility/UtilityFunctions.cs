using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Threading.Tasks;
using AssetLayer.SDK.Expressions;
using System.Reflection;
using System.Text;

namespace AssetLayer.Unity
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

        public static void CopyProperties(object source, object destination)
        {
            if (source == null || destination == null)
                throw new ArgumentNullException("Source or/and Destination Objects are null");

            Type typeSource = source.GetType();
            Type typeDestination = destination.GetType();

            PropertyInfo[] sourceProperties = typeSource.GetProperties();
            PropertyInfo[] destinationProperties = typeDestination.GetProperties();

            foreach (PropertyInfo sourceProperty in sourceProperties)
            {
                foreach (PropertyInfo destinationProperty in destinationProperties)
                {
                    if (sourceProperty.Name == destinationProperty.Name &&
                        destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        destinationProperty.SetValue(destination, sourceProperty.GetValue(source, null), null);
                        break;
                    }
                }
            }
        }

        public static IEnumerator WaitForTask(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogError(task.Exception);
                // Handle the exception
            }
        }

        public static string Generate24CharHexID()
        {
            var random = new System.Random();
            var bytes = new byte[12];
            random.NextBytes(bytes);
            StringBuilder sb = new StringBuilder(24);
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
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


