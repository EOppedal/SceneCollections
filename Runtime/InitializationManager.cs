using System.Threading.Tasks;
using Attributes;
using UnityEngine;

public class InitializationManager : MonoBehaviour {
    #region ---Fields---
    [Tooltip("NB! Should only be one scene!")]
    [SerializeField, Required] private SceneCollectionSO thisSceneCollectionSO;
    [SerializeField, Required] private SceneCollectionSO startSceneCollectionSO;
    #endregion

    private void Start() {
        _ = AwaitLoading();
    }

    private async Task AwaitLoading() {
        foreach (var sceneInstance in thisSceneCollectionSO.scenes) {
            SceneCollectionManager.AddToActiveScenes(sceneInstance);
        }
        
        await SceneCollectionManager.LoadSceneCollectionAsync(startSceneCollectionSO);
    }
}