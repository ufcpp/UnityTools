namespace ComponentForwarder
{
    /// <summary>
    /// 転送コンポーネントを作る(C#コード生成する)ためのクラス。
    /// </summary>
    public class CompomentGenerator
    {
        public string GenerateCode(string typeName, string dllNamespace, string csNamespace)
        {
            return string.Format(@"namespace {2}
{{
    class {0} : {1}.{2} {{ }}
}}
", typeName, dllNamespace, csNamespace);
        }
    }
}
