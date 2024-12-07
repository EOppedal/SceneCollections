using Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Assertions;
#endif

public class InitializationManager : MonoBehaviour {
    #region ---Fields---
    [Tooltip("NB! Should only be one scene!")]
    [SerializeField, Required] private SceneCollectionSO thisSceneCollection;
    [SerializeField, Required] private SceneCollectionSO startSceneCollection;
    #endregion

    private void Start() {
#if UNITY_EDITOR
        Assert.IsNotNull(thisSceneCollection);

        if (SceneCollectionManager.CurrentSceneCollection == null ||
            SceneCollectionManager.CurrentSceneCollection != thisSceneCollection) {
            SceneCollectionManager.CurrentSceneCollection = thisSceneCollection;
        }
#endif

        SceneCollectionManager.ClearActiveScenes();

        foreach (var sceneInstance in thisSceneCollection.scenes) {
            SceneCollectionManager.AddToActiveScenes(sceneInstance);
        }

        Debug.Log("Loading initial scene collection...");

        _ = SceneCollectionManager.LoadSceneCollectionAsync(startSceneCollection);
    }

#if UNITY_EDITOR
    [ContextMenu(nameof(DebugSceneCollection))]
    public void DebugSceneCollection() {
        if (SceneCollectionManager.CurrentSceneCollection == null) {
            Debug.Log(nameof(SceneCollectionManager.CurrentSceneCollection) + " is null!");
        }
    }
#endif
}