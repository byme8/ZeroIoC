using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            if (context.SyntaxReceiver is not ZeroIoCDeclarationReceiver receiver)
            {
                return;
            }

            if (!receiver.Declarations.Any())
            {
                return;
            }

            foreach (var classDeclaration in receiver.Declarations)
            {
                GenerateContainer(context, classDeclaration);
            }
        }

        private static void GenerateContainer(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
        {
            var bootstrapMethod = classDeclaration
                            .DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(o => o.Identifier.Text == "Bootstrap");

            if (bootstrapMethod == null)
            {
                return;
            }

            var invocations = bootstrapMethod
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .ToArray();

            var singletons = new List<(TypeSyntax Interface, TypeSyntax Implmentation)>();
            var transients = new List<(TypeSyntax Interface, TypeSyntax Implmentation)>();
            foreach (var invocation in invocations)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax member &&
                    member.Name is GenericNameSyntax generic)
                {
                    switch (generic.Identifier.Text)
                    {
                        case "AddSingleton":
                            {
                                if (generic.TypeArgumentList.Arguments.Count == 1)
                                {
                                    var type = generic.TypeArgumentList.Arguments.First();
                                    singletons.Add((type, type));
                                }

                                if (generic.TypeArgumentList.Arguments.Count == 2)
                                {
                                    var interfaceType = generic.TypeArgumentList.Arguments.First();
                                    var implementationType = generic.TypeArgumentList.Arguments.Last();
                                    singletons.Add((interfaceType, implementationType));
                                }
                                break;
                            }

                        case "AddTransient":
                            {
                                if (generic.TypeArgumentList.Arguments.Count == 1)
                                {
                                    var type = generic.TypeArgumentList.Arguments.First();
                                    transients.Add((type, type));
                                }

                                if (generic.TypeArgumentList.Arguments.Count == 2)
                                {
                                    var interfaceType = generic.TypeArgumentList.Arguments.First();
                                    var implementationType = generic.TypeArgumentList.Arguments.Last();
                                    transients.Add((interfaceType, implementationType));
                                }
                                break;
                            }
                    }
                }
            }

            var semantic = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var containerType = semantic.GetDeclaredSymbol(classDeclaration);
            var singletonSymbols = singletons
                .Select(o =>
                {
                    var interfaceSymbol = semantic.GetSpeculativeTypeInfo(o.Interface.SpanStart, o.Interface,
                        SpeculativeBindingOption.BindAsTypeOrNamespace);
                    var implementationSymbol = semantic.GetSpeculativeTypeInfo(o.Implmentation.SpanStart,
                        o.Implmentation, SpeculativeBindingOption.BindAsTypeOrNamespace);

                    return new { Interface = interfaceSymbol.Type!, Implementation = implementationSymbol.Type! };
                })
                .ToArray();

            var transientSymbols = transients
                .Select(o =>
                {
                    var interfaceSymbol = semantic.GetSpeculativeTypeInfo(o.Interface.SpanStart, o.Interface,
                        SpeculativeBindingOption.BindAsTypeOrNamespace);
                    var implementationSymbol = semantic.GetSpeculativeTypeInfo(o.Implmentation.SpanStart,
                        o.Implmentation, SpeculativeBindingOption.BindAsTypeOrNamespace);

                    return new { Interface = interfaceSymbol.Type!, Implementation = implementationSymbol.Type! };
                })
                .ToArray();

            var source = @$"
using System;
using System.Collections.Generic;
using ZeroIoC;

namespace {containerType.ContainingNamespace}
{{
    public partial class {containerType.Name}
    {{
        public {containerType.Name}()
        {{
        {singletonSymbols.Select(o =>
$@"        StaticResolvers.Add(typeof({o.Interface.ToGlobalName()}), new SignletonResolver(() => new {o.Implementation.ToGlobalName()}()));").JoinWithNewLine()}
        {transientSymbols.Select(o =>
$@"        StaticResolvers.Add(typeof({o.Interface.ToGlobalName()}), new TransientResolver(() => new {o.Implementation.ToGlobalName()}()));").JoinWithNewLine()}
        }}
    }}
}}
";
            context.AddSource(classDeclaration.Identifier.Text + "_ZeroIoCContainer", source);
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
                        .Select(o => o.Type)
                        .OfType<GenericNameSyntax>()
                        .Any(o => o.Identifier.Text == "ZeroIoCContainer") ?? false)
                    {
                        Declarations.Add(classDeclaration);
                    }
                    break;
            }
        }
    }
}
