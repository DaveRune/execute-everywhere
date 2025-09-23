using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace KnightForge.Editor.ExecuteEverywhere
{
    public class ExecuteEverywhereWindow : EditorWindow
    {
        [SerializeField]
        private MonoScript _target;

        private Type _targetType;
        private bool _includeDerivedTypes = true;

        private bool _includeScenes = true;
        private bool _includePrefabs = true;
        private bool _ignorePlugins = true;

        [SerializeField]
        private ExecutionAction[] _actions = Array.Empty<ExecutionAction>();

        [SerializeField]
        private List<GameObject> _prefabs = new();

        private readonly Dictionary<GameObject, string> _prefabPaths = new();

        [SerializeField]
        private List<SceneAsset> _scenes = new();

        private readonly Dictionary<SceneAsset, string> _scenePaths = new();

        private SerializedObject _serializedObject;
        private SerializedProperty _actionsProperty;
        private SerializedProperty _prefabsProperty;
        private SerializedProperty _scenesProperty;
        private SerializedProperty _targetProperty;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Execute Everywhere")]
        public static void ShowWindow()
        {
            GetWindow<ExecuteEverywhereWindow>("Execute Everywhere");
        }

        private void OnEnable()
        {
            _serializedObject = new SerializedObject(this);
            _actionsProperty = _serializedObject.FindProperty("_actions");
            _prefabsProperty = _serializedObject.FindProperty("_prefabs");
            _scenesProperty = _serializedObject.FindProperty("_scenes");
            _targetProperty = _serializedObject.FindProperty("_target");
        }

        private void OnGUI()
        {
            GUILayout.Label("Search project for usages of:", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _serializedObject.Update();
            EditorGUILayout.PropertyField(_targetProperty, true);
            _serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck() && _target)
                _targetType = _target.GetClass();

            if (!_target)
                EditorGUILayout.HelpBox("Please assign a target.", MessageType.Warning);

            var originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 25;
            _includeDerivedTypes = EditorGUILayout.Toggle("Include Derived Types", _includeDerivedTypes);
            GUILayout.Width(EditorGUIUtility.currentViewWidth - 20);

            EditorGUILayout.Space(10);

            _includePrefabs = EditorGUILayout.Toggle("Search in Prefabs", _includePrefabs);
            _includeScenes = EditorGUILayout.Toggle("Search in Scenes", _includeScenes);
            _ignorePlugins = EditorGUILayout.Toggle("Ignore Plugins/ folders", _includeScenes);
            GUILayout.Width(EditorGUIUtility.currentViewWidth - 20);
            EditorGUIUtility.labelWidth = originalLabelWidth;

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!_target);
            if (GUILayout.Button("Find Usages"))
                FindUsages();

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _serializedObject.Update();
            EditorGUILayout.PropertyField(_prefabsProperty, true);
            EditorGUILayout.PropertyField(_scenesProperty, true);
            _serializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            GUILayout.Label("Apply actions:", EditorStyles.boldLabel);
            _serializedObject.Update();
            EditorGUILayout.PropertyField(_actionsProperty, true);
            _serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup((!_prefabs.Any() && !_scenes.Any()) || !_target || !_actions.Any());
            if (GUILayout.Button("Execute Actions"))
                ExecuteActions();

            EditorGUI.EndDisabledGroup();
        }

        private void FindUsages()
        {
            var originallyloadedScenePath = EditorSceneManager.GetActiveScene().path;

            try
            {
                if (_includePrefabs)
                    SearchPrefabs();

                if (_includeScenes)
                    SearchScenes();
            }
            catch (Exception _)
            {
                Debug.LogException(_);
            }

            EditorSceneManager.OpenScene(originallyloadedScenePath);
        }

        private void SearchPrefabs()
        {
            _prefabs.Clear();
            _prefabPaths.Clear();

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var guid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!AssetDatabase.IsOpenForEdit(prefabPath))
                    continue;

                if (prefabPath.StartsWith("Packages/"))
                    continue;

                if (_ignorePlugins && (prefabPath.Contains("Plugins/") || prefabPath.Contains("plugins/")))
                    continue;

                try
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    if (!prefab)
                        continue;

                    var allComponents = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                    if (!allComponents.Any())
                        continue;

                    MonoBehaviour component;

                    if (_includeDerivedTypes)
                        component = allComponents.FirstOrDefault(foundComponent => foundComponent && _targetType
                            .IsAssignableFrom(foundComponent.GetType()));
                    else
                        component = allComponents.FirstOrDefault(foundComponent => foundComponent && _targetType == foundComponent.GetType());

                    if (!component)
                        continue;

                    _prefabs.Add(prefab);
                    _prefabPaths.Add(prefab, prefabPath);
                }
                catch (Exception)
                {
                    // Ignored
                }
            }
        }

        private void SearchScenes()
        {
            _scenes.Clear();
            _scenePaths.Clear();

            var sceneGuids = AssetDatabase.FindAssets("t:Scene");

            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);

                if (!AssetDatabase.IsOpenForEdit(scenePath))
                    continue;

                if (scenePath.StartsWith("Packages/"))
                    continue;

                if (_ignorePlugins && (scenePath.Contains("Plugins/") || scenePath.Contains("plugins/")))
                    continue;

                try
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

                    var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                    MonoBehaviour component;

                    if (_includeDerivedTypes)
                        component = allComponents.FirstOrDefault(foundComponent => foundComponent && _targetType
                            .IsAssignableFrom(foundComponent.GetType()));
                    else
                        component = allComponents.FirstOrDefault(foundComponent => foundComponent && _targetType == foundComponent.GetType());

                    if (!component)
                        continue;

                    _scenes.Add(sceneAsset);
                    _scenePaths[sceneAsset] = scenePath;
                }
                catch (Exception)
                {
                    // Ignored
                }
            }
        }

        private void ExecuteActions()
        {
            var originallyloadedScenePath = EditorSceneManager.GetActiveScene().path;

            foreach (var action in _actions)
            {
                action.SetActiveContext(ExecutionAction.ContextType.Prefab);
                
                foreach (var prefabAsset in _prefabs)
                {
                    var prefabPath = _prefabPaths[prefabAsset];
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                    var allComponents = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                    var components = allComponents
                        .Where(component => component && _targetType
                            .IsAssignableFrom(component.GetType()))
                        .ToArray();

                    foreach (var component in components)
                        action.Execute(component);

                    EditorUtility.SetDirty(prefab);

                    action.Execute(prefab);

                    AssetDatabase.SaveAssetIfDirty(prefab);
                }

                action.SetActiveContext(ExecutionAction.ContextType.Scene);
                
                foreach (var sceneAsset in _scenes)
                {
                    var scenePath = _scenePaths[sceneAsset];
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                    var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    var components = allComponents
                        .Where(component => component && _targetType
                            .IsAssignableFrom(component.GetType()))
                        .ToArray();

                    foreach (var component in components)
                        action.Execute(component);

                    EditorSceneManager.SaveScene(scene);
                }
            }

            EditorSceneManager.OpenScene(originallyloadedScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}