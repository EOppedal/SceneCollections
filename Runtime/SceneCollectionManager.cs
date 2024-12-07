using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public static class SceneCollectionManager {
    #region ---Fields---
    private const string SOFolder = "SceneCollections";
    private const string SOName = "SceneCollectionManagerSO";
    private const string ResourcesPath = "Assets/Resources";
    
    private static SceneCollectionManagerSO SceneCollectionManagerSO {
        get {
            if (_sceneCollectionManagerSO != null) return _sceneCollectionManagerSO;

            _sceneCollectionManagerSO = ScrubUtils
                .GetAllScrubsInResourceFolder<SceneCollectionManagerSO>(SOFolder)
                .GetByName(SOName);

#if UNITY_EDITOR
            if (_sceneCollectionManagerSO == null) {
                var so = ScriptableObject.CreateInstance<SceneCollectionManagerSO>();
                
                var folderPath = Path.Join(ResourcesPath, SOFolder);

                if (!Directory.Exists(folderPath)) {
                    Directory.CreateDirectory(folderPath);
                }

                var path = Path.Join(ResourcesPath, SOFolder, SOName) + ".asset";

                AssetDatabase.CreateAsset(so, path);

                _sceneCollectionManagerSO = so;
            }
#endif
            Assert.IsNotNull(_sceneCollectionManagerSO);
            Debug.Log("Scene Collection Manager SO loaded manually");

            return _sceneCollectionManagerSO;
        }
    }

#if UNITY_EDITOR
    public static SceneCollectionSO CurrentSceneCollection;
#endif

    private static SceneCollectionManagerSO _sceneCollectionManagerSO;

    private static List<SceneCollectionSO.SceneInstance> ActiveScenes => SceneCollectionManagerSO.activeScenes;
    private static List<SceneCollectionSO.SceneInstance> PersistentScenes => SceneCollectionManagerSO.persistentScenes;
    #endregion

    public static void AddToActiveScenes(SceneCollectionSO.SceneInstance scene) => ActiveScenes.Add(scene);
    
    public static void ClearActiveScenes() => ActiveScenes.Clear();

    public static async Task LoadSceneCollectionAsync(SceneCollectionSO sceneCollectionSO) {
#if UNITY_EDITOR
        if (ActiveScenes.Count == 0) {
            Debug.LogWarning("*** No ActiveScenes! ***");
            Debug.Log(CurrentSceneCollection != null);

            foreach (var sceneInstance in CurrentSceneCollection.scenes) {
                AddSceneInstanceToLists(sceneInstance);
            }
        }

        EnsureSceneInBuildSettings("Packages/com.erlend-eiken-oppedal.scenecollections/Runtime/EmptyScene.unity");

#endif

        await SceneManager.LoadSceneAsync("EmptyScene"!, LoadSceneMode.Additive);

        foreach (var sceneInstance in sceneCollectionSO.notAllowedPersistentScenes) {
            foreach (var instance in PersistentScenes.Where(x => x.LoadedScene == sceneInstance.LoadedScene)) {
                PersistentScenes.Remove(instance);
                await SceneManager.UnloadSceneAsync(instance.LoadedScene);
            }
        }

        foreach (var sceneInstance in ActiveScenes.ToArray()) {
            if (sceneInstance.persistentScene) continue;
            if (sceneCollectionSO.scenes.Contains(sceneInstance) &&
                !sceneInstance.getsReloadedWhenLoadedAgain) continue;

            await SceneManager.UnloadSceneAsync(sceneInstance.BuildIndex, UnloadSceneOptions.None);
            Debug.Log($"[{nameof(SceneCollectionManager)}] Unloading: {sceneInstance.Name}");
            ActiveScenes.Remove(sceneInstance);
        }

        foreach (var sceneInstance in sceneCollectionSO.scenes) {
            if (!ActiveScenes.Contains(sceneInstance)) {
                await SceneManager.LoadSceneAsync(sceneInstance.BuildIndex, LoadSceneMode.Additive);
                Debug.Log($"[{nameof(SceneCollectionManager)}] Loading: {sceneInstance.Name}");

                AddSceneInstanceToLists(sceneInstance);
            }

            if (sceneInstance.activeScene) {
                SceneManager.SetActiveScene(sceneInstance.LoadedScene);
                Debug.Log($"[{nameof(SceneCollectionManager)}] Set Active Scene: {sceneInstance.Name}");
            }
        }

        SceneManager.UnloadSceneAsync("EmptyScene");
    }

    private static void AddSceneInstanceToLists(SceneCollectionSO.SceneInstance sceneInstance) {
        if (sceneInstance.persistentScene) {
            if (PersistentScenes.All(x => x.sceneReference.LoadedScene != sceneInstance.LoadedScene)) {
                PersistentScenes.Add(sceneInstance);
            }
        }

        ActiveScenes.Add(sceneInstance);
    }

#if UNITY_EDITOR
    private static void EnsureSceneInBuildSettings(string scenePath) {
        if (!File.Exists(scenePath)) {
            Debug.LogError($"Scene not found at path: {scenePath}");
            return;
        }

        var scenes = EditorBuildSettings.scenes;
        var isSceneInBuildSettings = scenes.Any(scene => scene.path == scenePath);

        if (!isSceneInBuildSettings) {
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++) {
                newScenes[i] = scenes[i];
            }
            
            newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);

            EditorBuildSettings.scenes = newScenes;

            Debug.Log($"Scene added to Build Settings: {scenePath}");
        }
        else {
            Debug.Log($"Scene already in Build Settings: {scenePath}");
        }
    }

    public static void LoadSceneCollectionEditor(SceneCollectionSO sceneCollectionSO) {
        CurrentSceneCollection = sceneCollectionSO;

        ActiveScenes.Clear();

        EditorLoadScene(sceneCollectionSO.scenes.First(), false);

        for (var i = 1; i < sceneCollectionSO.scenes.Length; i++) {
            var sceneInstance = sceneCollectionSO.scenes[i];

            EditorLoadScene(sceneInstance);
        }

        foreach (var sceneInstance in sceneCollectionSO.notAllowedPersistentScenes) {
            foreach (var instance in PersistentScenes.Where(x => x.LoadedScene == sceneInstance.LoadedScene)) {
                PersistentScenes.Remove(instance);
                EditorSceneManager.CloseScene(instance.LoadedScene, true);
            }
        }

        foreach (var sceneInstance in PersistentScenes.Where(sceneInstance => !ActiveScenes.Contains(sceneInstance))) {
            EditorSceneManager.OpenScene(sceneInstance.sceneReference.Path, OpenSceneMode.Additive);
            Debug.Log("Adding persistent scene not active");
        }
    }

    private static void EditorLoadScene(SceneCollectionSO.SceneInstance sceneInstance, bool additive = true) {
        AddSceneInstanceToLists(sceneInstance);

        if (additive) {
            EditorSceneManager.OpenScene(sceneInstance.sceneReference.Path, OpenSceneMode.Additive);
        }
        else {
            EditorSceneManager.OpenScene(sceneInstance.sceneReference.Path);
        }

        if (sceneInstance.activeScene) {
            SceneManager.SetActiveScene(sceneInstance.LoadedScene);
        }

        Debug.Log("Adding scene");
    }

    // private static IEnumerable<Scene> GetAllActiveScenes() {
    //     var sceneCount = SceneManager.sceneCount;
    //
    //     for (var i = 0; i < sceneCount; i++) {
    //         yield return SceneManager.GetSceneAt(i);
    //     }
    // }
#endif
}