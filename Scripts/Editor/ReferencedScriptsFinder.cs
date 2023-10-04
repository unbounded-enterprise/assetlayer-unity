using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System;

namespace AssetLayer.Unity
{

    public class ReferencedScriptsFinder : EditorWindow
    {
        [MenuItem("Assets/Assetlayer/Find Referenced Scripts")]
        public static void FindScripts()
        {
            UnityEngine.Object selectedObject = Selection.activeObject;

            if (selectedObject is GameObject)
            {
                FindScriptsInGameObject(selectedObject as GameObject);
            }
            else if (selectedObject is SceneAsset)
            {
                FindScriptsInScene(selectedObject as SceneAsset);
            }
            else if (selectedObject is GameObject && PrefabUtility.IsPartOfAnyPrefab(selectedObject))
            {
                FindScriptsInPrefab(selectedObject as GameObject);
            }
            else
            {
                Debug.LogWarning("Selected object is not recognized as a GameObject, Scene, or Prefab!");
            }
        }

        public static void FindScriptsInObject(UnityEngine.Object obj)
        {
            if (obj is GameObject)
            {
                FindScriptsInGameObject(obj as GameObject);
            }
            else if (obj is SceneAsset)
            {
                FindScriptsInScene(obj as SceneAsset);
            }
            else if (obj is GameObject && PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                FindScriptsInPrefab(obj as GameObject);
            }
            else
            {
                Debug.LogWarning("Provided object is not recognized as a GameObject, Scene, or Prefab!");
            }
        }

        private static void FindScriptsInGameObject(GameObject gameObject)
        {
            List<string> scriptNames = GetScriptsFromGameObject(gameObject);
            // For demonstration, print to the console.
            Debug.Log("Scripts attached to " + gameObject.name + ": " + string.Join(", ", scriptNames));
        }

        private static void FindScriptsInScene(SceneAsset sceneAsset)
        {
            string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
            EditorSceneManager.OpenScene(scenePath);

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            HashSet<string> scriptNames = new HashSet<string>();

            foreach (var obj in allObjects)
            {
                List<string> names = GetScriptsFromGameObject(obj);
                foreach (var name in names)
                {
                    scriptNames.Add(name);
                }
            }

            // For demonstration, print to the console.
            Debug.Log("Scripts in the selected scene: " + string.Join(", ", scriptNames));
        }

        private static void FindScriptsInPrefab(GameObject prefab)
        {
            List<string> scriptNames = GetScriptsFromGameObject(prefab);
            // For demonstration, print to the console.
            Debug.Log("Scripts in the selected prefab: " + string.Join(", ", scriptNames));
        }

        private static List<string> GetScriptsFromGameObject(GameObject gameObject)
        {
            MonoBehaviour[] scripts = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
            List<string> scriptNamesWithNamespace = new List<string>();

            foreach (var script in scripts)
            {
                if (script)
                {
                    Type scriptType = script.GetType();
                    string fullName = string.IsNullOrEmpty(scriptType.Namespace)
                        ? scriptType.Name
                        : scriptType.Namespace + "." + scriptType.Name;

                    scriptNamesWithNamespace.Add(fullName);
                }
            }

            return scriptNamesWithNamespace;
        }
    }
}