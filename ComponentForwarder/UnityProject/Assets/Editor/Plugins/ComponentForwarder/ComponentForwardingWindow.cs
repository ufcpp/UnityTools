using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ComponentForwarder
{
    public class ComponentForwardingWindow : EditorWindow
    {
        [MenuItem("Tools/Component Forwarding/Forward Components in Scene")]
        public static void ShowWindow()
        {
            var window = CreateInstance<ComponentForwardingWindow>();
            window.titleContent = new GUIContent("Component Forwarding");
            window.Show();
        }

        ComponentRepository _repository;
        private IEnumerable<ForwardingGameObjectInfo> _objects;

        private static readonly GUILayoutOption _width100 = GUILayout.Width(100);
        private Vector2 _currentSceneScroll;

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
            _repository = _repository ?? new ComponentRepository();
            if (_objects == null)
            {
                _currentSceneScroll = Vector2.zero;
                _objects = ForwardingGameObjectInfo.FromCurrentScene(_repository);
            }

            if (GUILayout.Button("転送"))
            {
                _objects.Forward();
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
    }
}
