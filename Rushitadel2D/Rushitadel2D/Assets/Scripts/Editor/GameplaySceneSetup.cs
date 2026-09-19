#if UNITY_EDITOR
using System;
using Rushitadel.Data;
using UnityEditor;
using UnityEngine;

namespace Rushitadel.Editor
{
    /// <summary>Checks authored content without recreating or overwriting designer assets.</summary>
    public static class GameplaySceneSetup
    {
        [MenuItem("Rushitadel 2D/Validate Gameplay Content")]
        public static void ValidateContent()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:GameplayDefinition", new[] { "Assets/Data" }))
            {
                var definition = AssetDatabase.LoadAssetAtPath<GameplayDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var copy = UnityEngine.Object.Instantiate(definition);
                copy.name = definition.name;
                string before = JsonUtility.ToJson(copy);
                copy.Validate();
                bool valid = before == JsonUtility.ToJson(copy);
                UnityEngine.Object.DestroyImmediate(copy);
                if (!valid) throw new InvalidOperationException("Invalid definition values: " + definition.name);
                count++;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
                ValidateRoot(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects()) ValidateRoot(root);
            Debug.Log($"CONTENT_VALIDATION_SUCCESS: {count} definitions, all prefabs and loaded scenes.");
        }

        private static void ValidateRoot(GameObject root)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    throw new InvalidOperationException("Missing script: " + child.name);
                foreach (var component in child.GetComponents<MonoBehaviour>())
                {
                    var dataProperty = component.GetType().GetProperty("Definition");
                    if (dataProperty != null && dataProperty.GetValue(component) == null)
                        throw new InvalidOperationException($"Missing definition: {child.name}/{component.GetType().Name}");
                    var serialized = new SerializedObject(component);
                    foreach (string field in new[] { "enemyPrefab", "projectilePrefab", "playerBulletPrefab", "experiencePickupPrefab", "interactionSettings" })
                    {
                        var property = serialized.FindProperty(field);
                        if (property != null && property.objectReferenceValue == null)
                            throw new InvalidOperationException($"Missing prefab/settings: {child.name}/{field}");
                    }
                }
            }
        }
    }
}
#endif