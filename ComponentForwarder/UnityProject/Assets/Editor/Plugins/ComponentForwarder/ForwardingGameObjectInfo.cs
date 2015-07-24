using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ComponentForwarder
{
    /// <summary>
    /// <see cref="GameObject"/> を与えて、
    /// DLL由来のコンポーネントを持っている
    /// </summary>
    public class ForwardingGameObjectInfo
    {
        /// <summary>
        /// 対象のゲームオブジェクト。
        /// </summary>
        public GameObject Target { get; private set; }

        /// <summary>
        /// このゲームオブジェクトに付いてる DLL 由来のコンポーネント一覧。
        /// </summary>
        public IEnumerable<ForwardingCompomentInfo> DllComponents { get; private set; }

        public ForwardingGameObjectInfo(GameObject target, ComponentRepository repo)
        {
            Target = target;
            DllComponents = GetDllComponents(target, repo).ToArray();
        }

        private static IEnumerable<ForwardingCompomentInfo> GetDllComponents(GameObject go, ComponentRepository repo)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;

                var ct = c.GetType();
                var ci = repo.Find(ct);
                if (ci != null)
                    yield return ci;
            }
        }

        /// <summary>
        /// 現在開いているシーン中の全オブジェクトから
        /// </summary>
        /// <param name="repo"></param>
        /// <returns></returns>
        public static IEnumerable<ForwardingGameObjectInfo> FromCurrentScene(ComponentRepository repo)
        {
            return FromCurrentSceneInternal(repo).ToArray();
        }

        private static IEnumerable<ForwardingGameObjectInfo> FromCurrentSceneInternal(ComponentRepository repo)
        {
            var transforms = Object.FindObjectsOfType<Transform>();

            foreach (var t in transforms)
            {
                yield return new ForwardingGameObjectInfo(t.gameObject, repo);
            }
        }

        public void Forward()
        {
            foreach (var c in DllComponents.Where(x => x.HasForwarder))
            {
                var dllType = c.DllType;
                var csType = c.ForwarderType;

                var dllComponent = Target.GetComponent(dllType);
                var csComponent = Target.AddComponent(csType);

                if (csComponent == null)
                    continue;

                foreach (var f in dllType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(f => f.GetCustomAttributes(true).Any(a => a is SerializeField)))
                {
                    var value = f.GetValue(dllComponent);
                    f.SetValue(csComponent, value);
                }

                //todo: public get/set 持ちプロパティ

                Object.DestroyImmediate(dllComponent);
            }
        }
    }

    public static class ForwardingGameObjectInfoExtensions
    {
        public static void Forward(this IEnumerable<ForwardingGameObjectInfo> objects)
        {
            foreach (var obj in objects)
            {
                obj.Forward();
            }
        }
    }
}
