using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ComponentForwarder
{
    public class PrefabComponentForwardingWindow : EditorWindow
    {
        [MenuItem("OC Tools/Component Forwarding/Forward Components in all Prefabs", priority = 2)]
        public static void ShowWindow()
        {
            var window = CreateInstance<PrefabComponentForwardingWindow>();
            window.titleContent = new GUIContent("(Prefabs) Component Forwarding");
            window.Show();
        }

        private ComponentRepository _repository;
        private IEnumerable<PrefabInfo> _prefabs;

        private static readonly GUILayoutOption _width100 = GUILayout.Width(100);
        private static readonly GUILayoutOption _width200 = GUILayout.Width(200);
        private Vector2 _currentSceneScroll;

        void OnHierarchyChange()
        {
            _repository = null;
            _prefabs = null;
        }

        void OnProjectChange()
        {
            _repository = null;
            _prefabs = null;
        }

        void OnLostFocus()
        {
            _repository = null;
            _prefabs = null;
        }

        void OnGUI()
        {
            if (_prefabs == null || _repository == null)
            {
                _repository = new ComponentRepository();
                _currentSceneScroll = Vector2.zero;
                _prefabs = PrefabInfo.GetAllPrefabs(_repository);
            }

            if (GUILayout.Button("転送"))
            {
                _prefabs.Forward();
                _prefabs.Apply();
                _prefabs = null;
                return;
            }

            GUILayout.Label("プレハブのコンポーネントの転送の可否:");
            _currentSceneScroll = GUILayout.BeginScrollView(_currentSceneScroll);

            foreach (var prefab in _prefabs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("", _width100);
                GUILayout.Label(prefab.Name);
                GUILayout.EndHorizontal();

                foreach (var obj in prefab.GameObjects)
                {
                    if (!obj.DllComponents.Any()) continue;

                    var message = obj.DllComponents.All(x => x.HasForwarder)
                        ? "転送可能"
                        : "不可";

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("", _width100);
                    GUILayout.Label(obj.Target.name, _width100);
                    GUILayout.Label(message);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("", _width200);
                    var valid = string.Join(", ", obj.DllComponents.Where(x => x.HasForwarder).Select(x => x.DllType.Name).ToArray());
                    GUILayout.Label("可能: " + valid);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("", _width200);
                    var invalid = string.Join(", ", obj.DllComponents.Where(x => !x.HasForwarder).Select(x => x.DllType.Name).ToArray());
                    GUILayout.Label("不可: " + invalid);
                    GUILayout.EndHorizontal();

                    GUILayout.Space(4);
                }
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
