using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests
{
    [TestClass]
    public class ZeroIoCResolverTest
    {
        [TestMethod]
        public async Task CompilesWithoutErrors()
        {
            var project = TestProject.Project;

            var newProject = await project.ApplyZeroIoCGenerator();

            var compilation = await newProject.GetCompilationAsync();
            var errors = compilation.GetDiagnostics()
                .Where(o => o.Severity == DiagnosticSeverity.Error)
                .ToArray();

            Assert.IsFalse(errors.Any(), errors.Select(o => o.GetMessage()).JoinWithNewLine());
        }
    }
}
