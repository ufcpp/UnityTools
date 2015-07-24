using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ComponentForwarder
{
    /// <summary>
    /// <see cref="SerializeField"/> 属性が付いた private フィールドをリフレクションで読み書きするためのクラス。
    /// </summary>
    public class ForwardingFieldInfo
    {
        private Component _component;
        private FieldInfo _fieldInfo;

        public ForwardingFieldInfo(Component component, FieldInfo fieldInfo)
        {
            _component = component;
            _fieldInfo = fieldInfo;
        }

        public object Value
        {
            get { return _fieldInfo.GetValue(_component); }
            set { _fieldInfo.SetValue(_component, value); }
        }

        public static IEnumerable<ForwardingFieldInfo> GetFields(Component component)
        {
            var t = component.GetType();

            foreach (var f in t.GetSerializeFieldInfo())
            {
                yield return new ForwardingFieldInfo(component, f);
            }
        }
    }

    public static class TypeExtensions
    {
        //todo: public get/set 持ちプロパティもやる？
        // うちのプロジェクトは、インスペクターで設定したい項目は全部 [SerializeField] 付きの private フィールドでやってるけども。
        // Unity 的には public プロパティでもいいはずなので。

        /// <summary>
        /// <see cref="SerializeField"/> 属性が付いた private フィールドの <see cref="FieldInfo"/> を取りだす。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static IEnumerable<FieldInfo> GetSerializeFieldInfo(this Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttributes(true).Any(a => a is SerializeField));
        }
    }
}
