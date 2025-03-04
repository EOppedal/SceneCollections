using System;
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

namespace SceneCollections {
    public static class SceneCollectionManager {
        #region ---Fields---
        private const string SOFolder = "SceneCollections";
        private const string SOName = "SceneCollectionManagerSO";
        private const string ResourcesPath = "Assets";
        
        private const string EmptyScenePath = SOFolder + "/" + "EmptyScene";

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

        private static List<SceneCollectionSO.SceneInstance> PersistentScenes =>
            SceneCollectionManagerSO.persistentScenes;
        #endregion

        public static void AddToActiveScenes(SceneCollectionSO.SceneInstance scene) => ActiveScenes.Add(scene);

        public static void ClearActiveScenes() => ActiveScenes.Clear();

        public static async Awaitable LoadSceneCollectionAsync(SceneCollectionSO sceneCollectionSO, Action callback = null) {
#if UNITY_EDITOR
            if (ActiveScenes.Count == 0) {
                Debug.LogWarning("*** No ActiveScenes! ***");
                Debug.Log(CurrentSceneCollection != null);

                foreach (var sceneInstance in CurrentSceneCollection.scenes) {
                    AddSceneInstanceToLists(sceneInstance);
                }
            }
            
            // var emptyScene = Resources.Load(EmptyScenePath, typeof(Scene));
            //
            // EnsureSceneInBuildSettings(emptyScene.name + ".unity");
#endif

            await SceneManager.LoadSceneAsync(SceneCollectionManagerSO.emptyScene.BuildIndex, LoadSceneMode.Additive);
            var activeScene = SceneManager.GetSceneByName("EmptyScene");
            SceneManager.SetActiveScene(activeScene);

            await UnloadLoadedScenes(sceneCollectionSO.notAllowedPersistentScenes);

            await LoadAllScenesInCollection(sceneCollectionSO);

            SetActiveSceneFromActiveScenes();

            await SceneManager.UnloadSceneAsync(SceneCollectionManagerSO.emptyScene.BuildIndex);

            callback?.Invoke();
        }

        #region ---Unloading---
        private static Task UnloadLoadedScenes(SceneCollectionSO.SceneInstance[] notAllowedPersistentScenes) {
            var unloadTasks = new List<Task>();
            
            foreach (var sceneInstance in ActiveScenes.ToArray()) {
                if (sceneInstance.persistentScene) {
                    if (notAllowedPersistentScenes.Any(x => x.name == sceneInstance.Name)) {
                        PersistentScenes.Remove(sceneInstance);
                    }
                    else {
                        continue;
                    }
                }
                
                ActiveScenes.Remove(sceneInstance);

                unloadTasks.Add(UnloadSceneAsync(sceneInstance.LoadedScene));
                
                Debug.Log($"[{nameof(SceneCollectionManager)}] Unloading: {sceneInstance.Name}");
            }

            return Task.WhenAll(unloadTasks);
        }

        private static Task UnloadSceneAsync(Scene scene) {
            var operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null) return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            operation.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }

        #endregion

        #region ---Loading---
        private static async Task LoadAllScenesInCollection(SceneCollectionSO sceneCollectionSO) {
            // var loadTasks = new List<Task>();

            foreach (var sceneInstance in sceneCollectionSO.scenes) {
                if (ActiveScenes.Any(x => x.BuildIndex == sceneInstance.BuildIndex)) continue;
                
                AddSceneInstanceToLists(sceneInstance);
                // loadTasks.Add(LoadSceneAsync(sceneInstance.BuildIndex));
                await LoadSceneAsync(sceneInstance.BuildIndex);
                Debug.Log($"[{nameof(SceneCollectionManager)}] Loading: {sceneInstance.Name}");
            }

            // await Task.WhenAll(loadTasks);
        }

        private static Task LoadSceneAsync(int buildIndex) {
            var operation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);

            if (operation == null) return Task.CompletedTask;

            var tcs = new TaskCompletionSource<bool>();
            operation.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }
        #endregion

        private static void SetActiveSceneFromActiveScenes() {
            var activeScene = ActiveScenes.Where(sceneInstance => sceneInstance.activeScene).ToArray();

            if (activeScene.Length > 1) {
                Debug.LogWarning($"[{nameof(SceneCollectionManager)}] More than one Active Scenes");
            }

            var activeSceneInstance = activeScene.Last();
            SceneManager.SetActiveScene(activeSceneInstance.LoadedScene);
            Debug.Log($"[{nameof(SceneCollectionManager)}] Set Active Scene: {activeSceneInstance.Name}");
        }

        private static void AddSceneInstanceToLists(SceneCollectionSO.SceneInstance sceneInstance) {
            if (sceneInstance.persistentScene) {
                PersistentScenes.Add(sceneInstance);
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
                for (var i = 0; i < scenes.Length; i++) {
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
                foreach (var instance in PersistentScenes.Where(x => x.LoadedScene == sceneInstance.LoadedScene)
                             .ToArray()) {
                    PersistentScenes.Remove(instance);
                    EditorSceneManager.CloseScene(instance.LoadedScene, true);
                }
            }

            foreach (var sceneInstance in
                     PersistentScenes.Where(sceneInstance => !ActiveScenes.Contains(sceneInstance))) {
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
#endif
    }
}