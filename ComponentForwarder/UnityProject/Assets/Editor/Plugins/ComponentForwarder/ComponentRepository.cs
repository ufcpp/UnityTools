using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ComponentForwarder
{
    /// <summary>
    /// コンポーネントの型情報管理。
    /// </summary>
    /// <remarks>
    /// Unity の仕様で、DLL 中のコンポーネントを直接参照すると、DLL やその中の型のリネーム、ファイル移動でコンポーネントが外れてしまうので、その対策。
    /// 以下の手順で、DLL 中のコンポーネントを、C# ソースコード中のコンポーネントに擬似的に「転送」する。
    ///
    /// - Unity エディターが読み込んでいる DLL を分類
    ///   - (無関係) : "/Editor/", "-Editor" を含むものは関係ないとみなして除外(UnityEngine.dll とかの Unity 自身の DLL、もしくは、エディター拡張用の DLL)
    ///   - (DLL由来): <see cref="System.Reflection.Assembly.Location"/> に "/Dlls/" が含まれているものは外部でコンパイルして元々 DLL だったものと判定
    ///   - (cs由来) : それ以外は、.cs の状態でプロジェクトに含まれてて、Unity がコンパイルしたものと判定
    ///  - (転送コンポーネント): DLL由来のコンポーネントに対して、そのDLL由来コンポーネントを継承しただけの、同名・別名前空間のcs由来でコンポーネントを作る
    /// - Unity エディター上の全オブジェクトを探索して、オブジェクトについてるコンポーネントがDLL由来かcs由来かを判定
    /// - もし、DLL由来のコンポーネントが付いたオブジェクトがあった場合、転送コンポーネントに付け替える(まだなければ転送コンポーネントも作る)
    ///
    /// この手順のために、Unity エディターが読み込んでいるDLLを調べて型情報を抜き出すのがこのクラスの役割。
    /// </remarks>
    public class ComponentRepository
    {
        private Dictionary<Type, ForwardingCompomentInfo> _table;

        public ComponentRepository()
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();

            var _dllComponents = new List<Type>();
            var _csComponents = new List<Type>();

            foreach (var asm in asms)
            {
                var loc = asm.Location.Replace('\\', '/');
                if (loc.Contains("/Editor/")) continue;
                if (loc.Contains("-Editor")) continue;
                if (loc.Contains("/Dlls/"))
                {
                    _dllComponents.AddRange(asm.GetTypes().Where(t => typeof(Component).IsAssignableFrom(t)));
                }
                else
                {
                    _csComponents.AddRange(asm.GetTypes().Where(t => typeof(Component).IsAssignableFrom(t)));
                }
            }

            var compontents = ForwardingCompomentInfo.New(_dllComponents, _csComponents);
            _table = compontents.ToDictionary(x => x.DllType);

            var first = compontents.Select(x => x.ForwarderType).FirstOrDefault(x => x != null);
            ForwarderNamespace = first == null ? "Namespace" : first.Namespace;
        }

        /// <summary>
        /// DLL 由来コンポーネントの型情報一覧。
        /// </summary>
        public IEnumerable<ForwardingCompomentInfo> DllComponents { get { return _table.Values; } }

        /// <summary>
        /// <paramref name="dllType"/> に対応する転送コンポーネント情報を取得。
        /// </summary>
        /// <param name="dllType"></param>
        /// <returns>なければ null。</returns>
        public ForwardingCompomentInfo Find(Type dllType)
        {
            ForwardingCompomentInfo x;
            return _table.TryGetValue(dllType, out x) ? x : null;
        }

        /// <summary>
        /// 転送コンポーネントのC#コード生成。
        /// </summary>
        public void GenerateForwarder()
        {
            foreach (var c in DllComponents.Where(c => !c.HasForwarder && c.IsChecked))
            {
                c.GenerateForwarder(ForwarderNamespace);
            }
        }

        /// <summary>
        /// UI 用。
        /// 転送コンポーネントをコード生成する際に使う名前空間。
        /// </summary>
        public string ForwarderNamespace { get; set; }
    }
}
