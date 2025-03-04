using System.Collections.Generic;
using Eflatun.SceneReference;
using UnityEngine;

namespace SceneCollections {
    public class SceneCollectionManagerSO : ScriptableObject {
        public SceneReference emptyScene;
        
        public List<SceneCollectionSO.SceneInstance> activeScenes = new();
        public List<SceneCollectionSO.SceneInstance> persistentScenes = new();
    }
}