﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ZeroIoCDeclarationReceiver());
        }

        private void GenerateContainer(GeneratorExecutionContext context, ClassDeclarationSyntax classDeclaration)
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

            var entries = new List<ServiceEntry>();
            foreach (var invocation in invocations)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax member &&
                    member.Name is GenericNameSyntax generic)
                {
                    switch (generic.Identifier.Text)
                    {
                        case "AddSingleton":
                            AddTypes(entries, ServiceEntry.LifetimeKind.Singleton, generic, semantic);
                            break;

                        case "AddTransient":
                            AddTypes(entries, ServiceEntry.LifetimeKind.Transient, generic, semantic);
                            break;

                        case "AddScoped":
                            AddTypes(entries, ServiceEntry.LifetimeKind.Scoped, generic, semantic);
                            break;
                    }
                }
            }

            var groupedEntries = entries
                .GroupBy(o => o.Interface)
                .ToArray();

            var transients = new HashSet<string>(entries
                .Where(o => o.Lifetime == ServiceEntry.LifetimeKind.Transient)
                .Select(o => o.Interface.ToGlobalName()));

            var containerType = semantic.GetDeclaredSymbol(classDeclaration);
            var source =
@$"using System;
using System.Linq;
using System.Collections.Generic;
using ZeroIoC;

namespace {containerType.ContainingNamespace}
{{
    {ZeroIoCAnalyzer.CodeGenerationAttribute}
    public sealed partial class {containerType.Name}
    {{{
        groupedEntries
            .Select(o =>
                $@"
        private struct {o.First().Interface.ToCreatorName()} : ICreator<{o.First().Interface.ToGlobalName()}>
        {{
            public {o.First().Interface.ToGlobalName()} Create(IZeroIoCResolver resolver)
            {{
                return {ResolveConstructor(o.First().Implementation, transients)};
            }}
        }}{
                    (o.Count() == 1 ? string.Empty :
        $@"
        private struct {o.First().Interface.ToCreatorName()}_Enumerable : ICreator<IEnumerable<{o.First().Interface.ToGlobalName()}>>
        {{
            public IEnumerable<{o.First().Interface.ToGlobalName()}> Create(IZeroIoCResolver resolver)
            {{
                return new [] {{ {o.Select(oo => ResolveConstructor(oo.Implementation, transients)).Join()} }};
            }}
        }}"
    )}")
            .JoinWithNewLine()}

        public {containerType.Name}()
        {{
{groupedEntries.Select(o =>
    {
        if (o.Count() == 1)
        {
            var entry = o.First();
            var (propertyToStore, resolver) = MapResolver(entry);

            return $@"          {propertyToStore}.Add(typeof({entry.Interface.ToGlobalName()}), new {resolver}<{entry.Interface.ToCreatorName()}, {entry.Interface.ToGlobalName()}>());";
        }

        return "";
    })
    .JoinWithNewLine()}
        }}

        protected {containerType.Name}(Dictionary<Type, IInstanceResolver> resolvers, Dictionary<Type, IInstanceResolver> scopedResolvers, bool scope = false)
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

        private (string, string) MapResolver(ServiceEntry entry)
        {
            switch (entry.Lifetime)
            {
                case ServiceEntry.LifetimeKind.Singleton:
                    return ("Resolvers", "SingletonResolver");
                case ServiceEntry.LifetimeKind.Transient:
                    return ("Resolvers", "TransientResolver");
                case ServiceEntry.LifetimeKind.Scoped:
                    return ("ScopedResolvers", "SingletonResolver");
                default:
                    return ("", "");
            }
        }

        private static string ResolveConstructor(ITypeSymbol typeSymbol, HashSet<string> transients)
        {
            var members = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(o => o.MethodKind == MethodKind.Constructor)
                .ToArray();

            if (members.Length > 1)
            {

            }

            var constructor = members.First();
            var arguments = constructor.Parameters.Select(o => o.Type).ToArray();
            var argumentsText = arguments.Select(o => transients.Contains(o.ToGlobalName()) ? $"default({o.ToCreatorName()}).Create(resolver)" : $"resolver.Resolve<{o.ToGlobalName()}>()");
            return $"new {typeSymbol.ToGlobalName()}({argumentsText.Join()})";
        }

        private static void AddTypes(List<ServiceEntry> singletons, ServiceEntry.LifetimeKind lifetimeKind, GenericNameSyntax generic, SemanticModel semantic)
        {
            if (generic.TypeArgumentList.Arguments.Count == 1)
            {
                var type = generic.TypeArgumentList.Arguments.First();
                var symbols = ExtractTypeSymbols(type, type);
                singletons.Add(new ServiceEntry(lifetimeKind, symbols.Interface, symbols.Implementation));
            }

            if (generic.TypeArgumentList.Arguments.Count == 2)
            {
                var interfaceType = generic.TypeArgumentList.Arguments.First();
                var implementationType = generic.TypeArgumentList.Arguments.Last();

                var symbols = ExtractTypeSymbols(interfaceType, implementationType);
                singletons.Add(new ServiceEntry(lifetimeKind, symbols.Interface, symbols.Implementation));
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

        private class ServiceEntry
        {
            public enum LifetimeKind
            {
                Singleton,
                Transient,
                Scoped,
            }

            public ServiceEntry(LifetimeKind lifetime, ITypeSymbol @interface, ITypeSymbol implementation)
            {
                Lifetime = lifetime;
                Interface = @interface;
                Implementation = implementation;
            }

            public LifetimeKind Lifetime { get; }
            public ITypeSymbol Interface { get; }
            public ITypeSymbol Implementation { get; }
        }
    }

    public class ZeroIoCDeclarationReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> Declarations { get; } = new();

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