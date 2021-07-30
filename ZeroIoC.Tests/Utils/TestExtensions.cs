using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace ZeroIoC.Tests.Utils
{
    public static class TestExtensions
    {
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
                    .Select(o => new { o.SyntaxTree, o.HintName }));


            foreach (var file in results)
            {
                project = project
                    .AddDocument(file.HintName,file.SyntaxTree.ToString())
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

        public static object ReflectionCall(this object @object, string name)
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
    }
}
