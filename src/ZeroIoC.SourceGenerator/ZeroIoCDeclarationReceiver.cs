using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroIoC;

public class ZeroIoCDeclarationReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> Declarations { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        switch (syntaxNode)
        {
            case ClassDeclarationSyntax classDeclaration:
                if (classDeclaration.BaseList?.Types
                        .Any(o => o.Type.ToString().EndsWith("ZeroIoCContainer")) ?? false)
                {
                    Declarations.Add(classDeclaration);
                }

                break;
        }
    }
}