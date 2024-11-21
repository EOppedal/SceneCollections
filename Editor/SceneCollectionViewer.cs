#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneCollectionViewer {
    [InitializeOnLoad] public class SceneCollectionViewer : EditorWindow {
        [SerializeField] private VisualTreeAsset visualTreeAsset;
        [SerializeField] private Texture titleTexture;

        [SerializeField] private VisualTreeAsset sceneCollectionElementTemplate;

        private VisualElement _container;

        private const string FolderName = "SceneCollections";
        private const string ResourcesPath = "Assets/Resources";
        private const string IconPath = "";

        static SceneCollectionViewer() {
            if (!Directory.Exists(ResourcesPath)) Directory.CreateDirectory(ResourcesPath);
            var targetPath = Path.Join(ResourcesPath, FolderName);
            if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
        }

        [MenuItem("Tools/SceneCollections")]
        public static void ShowWindow() {
            var window = GetWindow<SceneCollectionViewer>("Scene Collections");
            // window.titleContent.image = AssetDatabase.LoadAssetAtPath<Texture>(IconPath);
        }

        private void CreateGUI() {
            rootVisualElement.Clear();
            visualTreeAsset.CloneTree(rootVisualElement);
            _container = rootVisualElement.Q<VisualElement>("SceneCollectionContainer");
            Populate();
            var updateButton = rootVisualElement.Q<Button>("UpdateButton");
            updateButton.RegisterCallback<ClickEvent>(_ => CreateGUI());
        }

        private void Populate() {
            var debugLoggers = ScrubUtils.GetAllScrubsInResourceFolder<SceneCollectionSO>(FolderName);
            foreach (var debugLogger in debugLoggers) {
                CreateElements(debugLogger);
            }
        }

        private void CreateElements(SceneCollectionSO dataSource) {
            var element = new VisualElement();
            sceneCollectionElementTemplate.CloneTree(element);
            element.dataSource = dataSource;
            element.Q<Label>("Label").text = dataSource.name;
            element.Q<Button>("LoadCollectionButton").clicked += dataSource.LoadSceneCollection;

            _container.Add(element);
        }
    }
}
#endif