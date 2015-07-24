using System;
using System.Collections.Generic;
using System.Linq;

namespace ComponentForwarder
{
    /// <summary>
    /// - DLL由来の型かどうかを判定
    /// - その型に対して転送コンポーネントがあるかどうかを判定
    /// - すでにあれば転送コンポーネントの情報も持っておく
    /// </summary>
    public class ForwardingCompomentInfo
    {
        /// <summary>
        /// DLL由来の型情報。
        /// </summary>
        public Type DllType { get; private set; }

        /// <summary>
        /// <see cref="DllType"/> に対応する転送コンポーネントの型情報。
        /// まだ転送コンポーネントがない場合は null。
        /// </summary>
        public Type ForwarderType { get; private set; }

        /// <summary>
        /// 転送コンポーネントがすでにあるかどうか。あれば true。
        /// </summary>
        public bool HasForwarder { get { return ForwarderType != null; } }

        /// <summary>
        /// UI 用。
        /// 転送コンポーネントがまだない型を一覧表示して、その中から転送コンポーネントをコード生成したいものをトグルで選ぶ。
        /// </summary>
        public bool IsChecked { get; set; }

        /// <summary>
        /// <see cref="DllType"/> に対応する転送コンポーネントを
        /// <paramref name="csTypes"/> から探す。
        /// </summary>
        /// <param name="dllType"><see cref="DllType"/></param>
        /// <param name="csTypes">cs由来の型一覧。</param>
        public ForwardingCompomentInfo(Type dllType, IEnumerable<Type> csTypes)
        {
            DllType = dllType;
            ForwarderType = csTypes.FirstOrDefault(t => t.Name == dllType.Name && dllType.IsAssignableFrom(t));
        }

        /// <summary>
        /// dll由来の全型に対して
        /// </summary>
        /// <param name="dllTypes">DLl由来の型一覧。</param>
        /// <param name="csTypes">cs由来の型一覧。</param>
        /// <returns></returns>
        public static ForwardingCompomentInfo[] New(IEnumerable<Type> dllTypes, IEnumerable<Type> csTypes)
        {
            return dllTypes
                .Select(dllType => new ForwardingCompomentInfo(dllType, csTypes))
                .ToArray();
        }

        /// <summary>
        /// コード生成する。
        /// </summary>
        /// <param name="csNamespace">生成する転送コンポーネントの名前空間。</param>
        public void GenerateForwarder(string csNamespace)
        {
            CompomentGenerator.GenerateCodeFile(DllType, csNamespace);
        }
    }
}
