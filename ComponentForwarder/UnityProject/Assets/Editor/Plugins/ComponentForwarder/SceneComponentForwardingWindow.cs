using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ComponentForwarder
{
    public class SceneComponentForwardingWindow : EditorWindow
    {
        [MenuItem("OC Tools/Component Forwarding/Forward Components in the Loaded Scene", priority = 1)]
        public static void ShowWindow()
        {
            var window = CreateInstance<SceneComponentForwardingWindow>();
            window.titleContent = new GUIContent("(Scene) Component Forwarding");
            window.Show();
        }

        private ComponentRepository _repository;
        private IEnumerable<ForwardingGameObjectInfo> _objects;

        private static readonly GUILayoutOption _width100 = GUILayout.Width(100);
        private Vector2 _currentSceneScroll;

        void OnHierarchyChange()
        {
            _repository = null;
            _objects = null;
        }

        void OnProjectChange()
        {
            _repository = null;
            _objects = null;
        }

        void OnLostFocus()
        {
            _repository = null;
            _objects = null;
        }

        void OnGUI()
        {
            if (_objects == null || _repository == null)
            {
                _repository = new ComponentRepository();
                _currentSceneScroll = Vector2.zero;
                _objects = ForwardingGameObjectInfo.FromCurrentScene(_repository);
            }

            if (GUILayout.Button("転送"))
            {
                _objects.Forward();
                _objects = null;
                return;
            }

            GUILayout.Label("このシーン中のコンポーネントの転送の可否:");
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
    }
}
