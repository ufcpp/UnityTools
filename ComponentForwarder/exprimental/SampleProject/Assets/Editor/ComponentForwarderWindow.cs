using UnityEditor;
using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace ComponentForwarder
{
    public class ComponentForwarderWindow : EditorWindow
    {
        [MenuItem("Tools/Component Forwarding")]
        public static void ShowWindow()
        {
            var window = CreateInstance<ComponentForwarderWindow>();
            window.titleContent = new GUIContent("Component Behaviour");
            window.Show();
        }

        enum UIState
        {
            Initial = 0,
            Top,
            Forwarder,
            CurrentScene,
            Prefabs,
        }

        UIState _state;

        void OnProjectChange()
        {
            _state = UIState.Initial;
        }

        void OnLostFocus()
        {
            _state = UIState.Top;
        }

        void OnGUI()
        {
            switch (_state)
            {
                default:
                    Initialize();
                    _state = UIState.Top;
                    goto case UIState.Top;
                case UIState.Top:
                    if (GUILayout.Button("転送コンポーネントの生成")) _state = UIState.Forwarder;
                    GUILayout.Label("DLL中のコンポーネントを直接参照しているオブジェクトを探します");
                    if (GUILayout.Button("現在のシーンから"))
                    {
                        InitializeCurrentScene();
                        _state = UIState.CurrentScene;
                    }
                    if (GUILayout.Button("プレハブから")) _state = UIState.Prefabs;
                    break;
                case UIState.Forwarder:
                    OnForwarderGUI();
                    break;
                case UIState.CurrentScene:
                    OnCurrentSceneGUI();
                    break;
                case UIState.Prefabs:
                    OnPrefabsGUI();
                    break;
            }

            //Debug.Log("#levels " + Application.levelCount);

            //foreach (var scene in EditorBuildSettings.scenes)
            //{
            //    var a = AssetDatabase.LoadMainAssetAtPath(scene.path);
            //    Debug.Log("scene " + a);

            //}
        }

        private void Initialize()
        {
            _repository = new ComponentRepository();
        }

        private void OnForwarderGUI()
        {
            if (GUILayout.Button("チェックが入った型の転送コンポーネントを生成する"))
            {
                _repository.GenerateForwarder();
                _state = UIState.Top;
            }

            foreach (var c in _repository.DllComponents)
            {
                if (c.HasForwarder)
                {
                    c.IsChecked = GUILayout.Toggle(c.IsChecked, c.DllType.Name);
                }
                else
                {
                    GUILayout.Label("(済) " + c.DllType.Name);
                }
            }
        }

        private void OnPrefabsGUI()
        {
            //var paths = AssetDatabase.GetAllAssetPaths();

            //foreach (var item in paths.Where(p => p.Contains(".prefab")))
            //{
            //    var go = AssetDatabase.LoadAssetAtPath<GameObject>(item);
            //    Debug.Log("prefab " + go.name + ", " + go.GetType().Name);

            //    var transforms = go.GetComponentsInChildren<Transform>(true);
            //    foreach (var t in transforms)
            //    {
            //        Debug.Log("    obj " + t.name);

            //        foreach (var c in t.gameObject.GetComponents<Component>())
            //        {
            //            var ct = c.GetType();
            //            if (_dllComponents.Contains(ct))
            //            {
            //                Debug.Log("        component " + c.GetType().FullName);
            //            }
            //        }
            //    }
            //}
            if (GUILayout.Button("OK"))
            {
                _state = UIState.Top;
            }
        }

        private IEnumerable<ForwardingGameObjectInfo> _objects;

        private void InitializeCurrentScene()
        {
            _currentSceneScroll = Vector2.zero;
            _objects = ForwardingGameObjectInfo.FromCurrentScene(_repository);
        }

        private static readonly GUILayoutOption _width100 = GUILayout.Width(100);
        private Vector2 _currentSceneScroll;

        private void OnCurrentSceneGUI()
        {
            if (GUILayout.Button("転送"))
            {
                _objects.Forward();
                _state = UIState.Top;
            }

            _currentSceneScroll = GUILayout.BeginScrollView(_currentSceneScroll);

            foreach (var obj in _objects)
            {
                if (!obj.DllComponents.Any()) continue;

                var message = obj.DllComponents.All(x => x.HasForwarder)
                    ? "転送可能"
                    : "不可";

                GUILayout.BeginHorizontal();
                GUILayout.Label(obj.Target.name, _width100);
                GUILayout.Label(message);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("", _width100);
                var valid = string.Join(", ", obj.DllComponents.Where(x => x.HasForwarder).Select(x => x.DllType.Name).ToArray());
                GUILayout.Label("可能: " + valid);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("", _width100);
                var invalid = string.Join(", ", obj.DllComponents.Where(x => !x.HasForwarder).Select(x => x.DllType.Name).ToArray());
                GUILayout.Label("不可: " + invalid);
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();
        }

        IEnumerable<GameObject> Descendants(GameObject go)
        {
            yield return go;

            var count = go.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = go.transform.GetChild(i);

                foreach (var x in Descendants(child.gameObject))
                    yield return x;
            }
        }

        ComponentRepository _repository;
    }
}
