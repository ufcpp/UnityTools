using System;
using System.IO;
using UnityEngine;

namespace ComponentForwarder
{
    /// <summary>
    /// 転送コンポーネントを作る(C#コード生成する)ためのクラス。
    /// </summary>
    public class CompomentGenerator
    {
        public static void GenerateCodeFile(Type dllType, string csNamespace)
        {
            var code = GenerateCode(dllType.Name, dllType.Namespace, csNamespace);

            var path = Application.dataPath;
            path = Path.Combine(path, "Scripts");
            path = Path.Combine(path, "Forwarders");
            path = Path.Combine(path, dllType.Name + ".cs");

            File.WriteAllText(path, code);
        }

        public static string GenerateCode(string typeName, string dllNamespace, string csNamespace)
        {
            return string.Format(@"namespace {2}
{{
    public class {0} : {1}.{0} {{ }}
}}
", typeName, dllNamespace, csNamespace);
        }
    }
}
