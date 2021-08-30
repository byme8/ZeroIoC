using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ZeroIoC.Tests.Utils
{
    public static class TestExtensions
    {
        public static async Task<ImmutableArray<Diagnostic>> ApplyAnalyzer(this Project project, DiagnosticAnalyzer analyzer)
        {
            var compilation = await project.GetCompilationAsync();
            var newCompilation = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
            var diagnostics = await newCompilation.GetAllDiagnosticsAsync();

            return diagnostics;
        }

        public static async Task<Project> ApplyToProgram(this Project project, string newText)
        {
            return await project.ReplacePartOfDocumentAsync("Program.cs", "// place to replace", newText);
        }

        public static async Task<Project> ReplacePartOfDocumentAsync(this Project project, string documentName, string textToReplace, string newText)
        {
            var document = project.Documents.First(o => o.Name == documentName);
            var text = await document.GetTextAsync();
            return document
                .WithText(SourceText.From(text.ToString().Replace(textToReplace, newText)))
                .Project;
        }

        public static async Task<Project> ApplyZeroIoCGenerator(this Project project)
        {
            var newProject = await project.RunSourceGenerator(new ZeroIoCContainerGenerator());

            return newProject;
        }

        public static async Task<Project> RunSourceGenerator<TGenerator>(this Project project, TGenerator generator)
            where TGenerator : ISourceGenerator
        {

            var compilation = await project.GetCompilationAsync();
            var driver = CSharpGeneratorDriver.Create(generator);
            var results = driver
                .RunGenerators(compilation)
                .GetRunResult()
                .Results
                .SelectMany(o => o
                    .GeneratedSources
                    .Select(o => new { o.SyntaxTree, o.HintName, }));


            foreach (var file in results)
            {
                project = project
                    .AddDocument(file.HintName, file.SyntaxTree.ToString())
                    .Project;
            }

            return project;
        }

        public static object ReflectionGetValue(this object @object, string name)
        {
            var nonPublic = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var member = @object.GetType().GetField(name, nonPublic);
            if (member is null)
            {
                return @object
                    .GetType()
                    .GetProperty(name, nonPublic)
                    ?.GetValue(@object);
            }

            return member.GetValue(@object);
        }

        public static object ReflectionCall(this object @object, string name, params object[] args)
        {
            var nonPublic = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var member = @object.GetType().GetMethod(name, nonPublic);
            if (member is null)
            {
                return @object
                    .GetType()
                    .GetProperty(name, nonPublic)
                    ?.GetValue(@object);
            }

            return member.Invoke(@object, args);
        }

        public static async Task<Assembly> CompileToRealAssembly(this Project project)
        {
            var compilation = await project.GetCompilationAsync();
            var error = compilation.GetDiagnostics().FirstOrDefault(o => o.Severity == DiagnosticSeverity.Error);
            if (error != null)
            {
                throw new Exception(error.GetMessage());
            }

            using (var memoryStream = new MemoryStream())
            {
                compilation.Emit(memoryStream);
                var bytes = memoryStream.ToArray();
                var assembly = Assembly.Load(bytes);

                return assembly;
            }
        }
    }
}