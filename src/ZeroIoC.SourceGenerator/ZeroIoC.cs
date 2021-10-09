namespace ZeroIoC
{
    public class ZeroIoCAnalyzer
    {
        public static string Version { get; } = typeof(ZeroIoCAnalyzer).Assembly.GetName().Version.ToString();
        public static string CodeGenerationAttribute { get; } = $@"[System.CodeDom.Compiler.GeneratedCode(""ZeroIoc"", ""{Version}"")]";
    }
}