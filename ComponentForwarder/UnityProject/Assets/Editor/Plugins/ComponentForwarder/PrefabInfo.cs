using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ComponentForwarder
{
    /// <summary>
    /// プレハブに関する情報。
    /// </summary>
    public class PrefabInfo
    {
        private GameObject _prefabRoot;
        private ComponentRepository _repository;

        public string Name { get { return _prefabRoot.name; } }

        public PrefabInfo(string prefabPath, ComponentRepository repository)
        {
            _prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            _repository = repository;
        }

        /// <summary>
        /// プレハブにかけた変更を、元のプレハブに適用。
        /// </summary>
        public void Apply()
        {
            var prefab = PrefabUtility.GetPrefabParent(_prefabRoot) as GameObject;
            PrefabUtility.ReplacePrefab(_prefabRoot, prefab, ReplacePrefabOptions.ConnectToPrefab);
        }

        public void Forward()
        {
            GameObjects.Forward();
        }

        /// <summary>
        /// プレハブ中の全オブジェクトから <see cref="ForwardingGameObjectInfo"/> を抽出。
        /// </summary>
        /// <param name="repo"></param>
        /// <returns></returns>
        public IEnumerable<ForwardingGameObjectInfo> GameObjects { get { return _gameObjects ?? (_gameObjects = GetObjectsInternal().ToArray()); } }
        private IEnumerable<ForwardingGameObjectInfo> _gameObjects;

        private IEnumerable<ForwardingGameObjectInfo> GetObjectsInternal()
        {
            foreach (var t in _prefabRoot.Descendants())
            {
                yield return new ForwardingGameObjectInfo(t.gameObject, _repository);
            }
        }

        /// <summary>
        /// プロジェクト中の全プレハブを取得。
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<PrefabInfo> GetAllPrefabs(ComponentRepository repository)
        {
            var prefabPaths = AssetDatabase.GetAllAssetPaths().Where(x => x.EndsWith(".prefab"));

            foreach (var path in prefabPaths)
            {
                yield return new PrefabInfo(path, repository);
            }
        }
    }

    public static class PrefabInfoExtensions
    {
        public static void Forward(this IEnumerable<PrefabInfo> prefabs)
        {
            foreach (var x in prefabs)
            {
                x.Forward();
            }
        }

        public static void Apply(this IEnumerable<PrefabInfo> prefabs)
        {
            foreach (var x in prefabs)
            {
                x.Apply();
            }
        }
    }
}
