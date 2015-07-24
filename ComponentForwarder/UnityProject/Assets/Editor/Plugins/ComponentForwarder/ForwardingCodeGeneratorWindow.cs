using UnityEditor;
using UnityEngine;

namespace ComponentForwarder
{
    /// <summary>
    /// 転送コンポーネントをコード生成するウィンドウ。
    /// </summary>
    public class ForwardingCodeGeneratorWindow : EditorWindow
    {
        [MenuItem("OC Tools/Component Forwarding/Generate Forwarding Code", priority = 0)]
        public static void ShowWindow()
        {
            var window = CreateInstance<ForwardingCodeGeneratorWindow>();
            window.titleContent = new GUIContent("Forwarding Code Genarator");
            window.Show();
        }

        ComponentRepository _repository;
        private Vector2 _currentSceneScroll;

        void OnProjectChange()
        {
            _repository = null;
        }

        void OnLostFocus()
        {
            _repository = null;
        }

        void OnGUI()
        {
            _repository = _repository ?? new ComponentRepository();

            GUILayout.BeginHorizontal();
            GUILayout.Label("生成コードの名前空間");
            _repository.ForwarderNamespace = GUILayout.TextField(_repository.ForwarderNamespace);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("チェックが入った型の転送コンポーネントを生成する"))
            {
                _repository.GenerateForwarder();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                _repository = null;
                return;
            }

            _currentSceneScroll = GUILayout.BeginScrollView(_currentSceneScroll);

            foreach (var c in _repository.DllComponents)
            {
                if (!c.HasForwarder)
                {
                    c.IsChecked = GUILayout.Toggle(c.IsChecked, c.DllType.Name);
                }
                else
                {
                    GUILayout.Label("(済) " + c.DllType.Name);
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
