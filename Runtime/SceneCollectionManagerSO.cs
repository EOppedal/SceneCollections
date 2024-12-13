using System.Collections.Generic;
using UnityEngine;

namespace SceneCollections {
    public class SceneCollectionManagerSO : ScriptableObject {
        public List<SceneCollectionSO.SceneInstance> activeScenes = new();
        public List<SceneCollectionSO.SceneInstance> persistentScenes = new();
    }
}