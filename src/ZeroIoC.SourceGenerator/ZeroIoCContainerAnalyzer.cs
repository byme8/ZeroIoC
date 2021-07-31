using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ZeroIoC
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ZeroIoCContainerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray<DiagnosticDescriptor>.Empty
                .Add(Descriptors.StatementsNotAllowed)
                .Add(Descriptors.BootstrapIsNotOverrided)
                .Add(Descriptors.ClassIsNotPartial)
                .Add(Descriptors.CreateScopeIsOverrided);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.ClassDeclaration);
        }
        private void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is ClassDeclarationSyntax classDeclaration &&
                (classDeclaration.BaseList?.Types.Any(o => o.Type.ToString() == "ZeroIoCContainer") ?? false))
            {
                var isPartial = classDeclaration.Modifiers.Any(o => o.IsKind(SyntaxKind.PartialKeyword));
                if (!isPartial)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.ClassIsNotPartial,
                            classDeclaration.GetLocation(),
                            classDeclaration.Identifier.Text));
                }


                var methods = classDeclaration
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .ToArray();
                
                var bootstrapMethod = methods.FirstOrDefault(o => o.Identifier.Text == "Bootstrap");
                var createScopeMethod = methods.FirstOrDefault(o => o.Identifier.Text == "CreateScope");

                if (createScopeMethod != null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.CreateScopeIsOverrided,
                            createScopeMethod.GetLocation(),
                            classDeclaration.Identifier.Text));
                }

                if (bootstrapMethod == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.BootstrapIsNotOverrided,
                            classDeclaration.GetLocation(),
                            classDeclaration.Identifier.Text));
                    return;
                }

                var statements = bootstrapMethod
                    .Body?
                    .DescendantNodes()
                    .Where(o => !o.IsKind(SyntaxKind.Block) && !o.IsKind(SyntaxKind.ExpressionStatement))
                    .OfType<StatementSyntax>()
                    .ToArray() ?? Enumerable.Empty<StatementSyntax>();

                if (statements.Any())
                {
                    foreach (var statement in statements)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Descriptors.StatementsNotAllowed, 
                                statement.GetLocation()));
                    }
                }
            }
        }

    }
}