using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ZeroIoC
{
    [Generator]
    public class ZeroIoCContainerGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {

        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ZeroIoCDeclarationReceiver());
        }
    }

    public class ZeroIoCDeclarationReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> Declarations { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            switch (syntaxNode)
            {
                case ClassDeclarationSyntax classDeclaration:
                    if (classDeclaration.BaseList?.Types
                        .Any(o => o.Type.ToString() == "ZeroIoCContainer") ?? false)
                    {
                        Declarations.Add(classDeclaration);
                    }
                    break;
            }
        }
    }
}
