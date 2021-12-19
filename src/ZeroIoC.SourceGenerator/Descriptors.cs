using Microsoft.CodeAnalysis;

namespace ZeroIoC
{
    public class Descriptors
    {
        public static readonly DiagnosticDescriptor ClassIsNotPartial = new(
            "ZI001",
            "ZeroContainer has to be a partial class",
            "The {0} ia not partial class. It is essential to enable source generation.",
            "ZeroIoc",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor BootstrapIsNotOverrided = new(
            "ZI002",
            "ZeroContainer does not override the Bootstrap method",
            "The {0} does not override the Bootstrap method. Override the Bootstrap method to enable source generation.",
            "ZeroIoc",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor CreateScopeIsOverrided = new(
            "ZI003",
            "ZeroContainer has override for the CreateScope method",
            "The {0} has override for the CreateScope method. There is no need to override the CreateScope. It will be overrided by the source generator.",
            "ZeroIoc",
            DiagnosticSeverity.Error,
            true);

        public static readonly DiagnosticDescriptor StatementsNotAllowed = new(
            "ZI004",
            "The Bootstrap method does not allow statements",
            "The Bootstrap method does not allow statements. Use only method calls from the IZeroIoCContainerBootstrapper.",
            "ZeroIoc",
            DiagnosticSeverity.Error,
            true);
        
        public static readonly DiagnosticDescriptor MultipleTypeRegistrationsNotAllowed = new(
            "ZI005",
            "The multiple type registrations are not allowed",
            "The multiple type registrations are not allowed",
            "ZeroIoc",
            DiagnosticSeverity.Error,
            true);
        
        public static readonly DiagnosticDescriptor OnlyOneConstructorWithArgumentAllowed = new(
            "ZI006",
            "Only one constructor with argument allowed",
            "Only one constructor with argument allowed",
            "ZeroIoc",
            DiagnosticSeverity.Error,
            true);
    }
}