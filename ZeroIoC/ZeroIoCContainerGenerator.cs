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

            var semantic = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var singletons = new List<(ITypeSymbol Interface, ITypeSymbol Implementation)>();
            var transients = new List<(ITypeSymbol Interface, ITypeSymbol Implementation)>();
            var scoped = new List<(ITypeSymbol Interface, ITypeSymbol Implementation)> ();
            foreach (var invocation in invocations)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax member &&
                    member.Name is GenericNameSyntax generic)
                {
                    switch (generic.Identifier.Text)
                    {
                        case "AddSingleton":
                            AddTypes(singletons, generic, semantic);
                            break;

                        case "AddTransient":
                            AddTypes(transients, generic, semantic);
                            break;

                        case "AddScoped":
                            AddTypes(scoped, generic, semantic);
                            break;
                    }
                }
            }

            var containerType = semantic.GetDeclaredSymbol(classDeclaration);
            var source = @$"
using System;
using System.Linq;
using System.Collections.Generic;
using ZeroIoC;

namespace {containerType.ContainingNamespace}
{{
    public partial class {containerType.Name}
    {{

        public {containerType.Name}()
        {{
        {singletons.Select(o =>
$@"        Resolvers.Add(typeof({o.Interface.ToGlobalName()}), new SignletonResolver(() => new {o.Implementation.ToGlobalName()}()));").JoinWithNewLine()}
        {transients.Select(o =>
$@"        Resolvers.Add(typeof({o.Interface.ToGlobalName()}), new TransientResolver(() => new {o.Implementation.ToGlobalName()}()));").JoinWithNewLine()}
        {scoped.Select(o =>
$@"        ScopedResolvers.Add(typeof({o.Interface.ToGlobalName()}), new SignletonResolver(() => new {o.Implementation.ToGlobalName()}()));").JoinWithNewLine()}
        }}

        protected {containerType.Name}(Dictionary<Type, InstanceResolver> resolvers, Dictionary<Type, InstanceResolver> scopedResolvers, bool scope = false)
            : base(resolvers, scopedResolvers, scope)
        {{
        }}

         public override IZeroIoCResolver CreateScope()
         {{
            var newScope = ScopedResolvers.ToDictionary(o => o.Key, o => o.Value.Duplicate());
            return new {containerType.Name}(Resolvers, newScope, true);
         }}
    }}
}}
";
            context.AddSource(classDeclaration.Identifier.Text + "_ZeroIoCContainer", source);
        }

        private static void AddTypes(List<(ITypeSymbol Interface, ITypeSymbol Implementation)> singletons, GenericNameSyntax generic, SemanticModel semantic)
        {
            if (generic.TypeArgumentList.Arguments.Count == 1)
            {
                var type = generic.TypeArgumentList.Arguments.First();
                singletons.Add(ExtractTypeSymbols(type, type));
            }

            if (generic.TypeArgumentList.Arguments.Count == 2)
            {
                var interfaceType = generic.TypeArgumentList.Arguments.First();
                var implementationType = generic.TypeArgumentList.Arguments.Last();
                singletons.Add(ExtractTypeSymbols(interfaceType, implementationType));
            }

            (ITypeSymbol Interface, ITypeSymbol Implementation) ExtractTypeSymbols(TypeSyntax interfaceType, TypeSyntax implementationType)
            {
                var interfaceSymbol = semantic.GetSpeculativeTypeInfo(interfaceType.SpanStart, interfaceType,
                          SpeculativeBindingOption.BindAsTypeOrNamespace);
                var implementationSymbol = semantic.GetSpeculativeTypeInfo(implementationType.SpanStart,
                    implementationType, SpeculativeBindingOption.BindAsTypeOrNamespace);

                return (interfaceSymbol.Type!, implementationSymbol.Type!);
            }

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
