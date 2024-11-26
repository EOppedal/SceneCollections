using System;
using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(
    fileName = nameof(SceneCollectionSO), 
    menuName = "Scriptable Objects/SceneCollections/Create " + nameof(SceneCollectionSO))]
public class SceneCollectionSO : ScriptableObject {
    public SceneInstance[] scenes;
    public SceneInstance[] notAllowedPersistentScenes;

    [ContextMenu(nameof(LoadSceneCollection))]
    public void LoadSceneCollection() {
#if UNITY_EDITOR
        if (!Application.isPlaying) {
            SceneCollectionManager.LoadSceneCollectionEditor(this);
            return;
        }
#endif
        
        _ = SceneCollectionManager.LoadSceneCollectionAsync(this);
    }

    [Serializable] public record SceneInstance {
        public SceneReference sceneReference;
        public bool activeScene;
        public bool getsReloadedWhenLoadedAgain;
        public bool persistentScene;
        
        public string Name => sceneReference.Name;
        public int BuildIndex => sceneReference.BuildIndex;
        public Scene LoadedScene => sceneReference.LoadedScene;
    }
}