using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests;

public class StatetmentsAnalyzerTest
{
    [Fact]
    public async Task IfNotAllowed()
    {
        var project = await TestProject.Project.ApplyToProgram(@"

        public interface IService
        {

        }

        public class Service : IService
        {

        }

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                if(true)
                {
                    bootstrapper.AddTransient<IService, Service>();
                }
            }
        }
");

        var diagnostics = await project.ApplyAnalyzer(new ZeroIoCContainerAnalyzer());

        Assert.True(diagnostics.Any(o => o.Id == Descriptors.StatementsNotAllowed.Id));

    }

    [Fact]
    public async Task WhileNotAllowed()
    {
        var project = await TestProject.Project.ApplyToProgram(@"

        public interface IService
        {

        }

        public class Service : IService
        {

        }

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                while(true)
                {
                    bootstrapper.AddTransient<IService, Service>();
                }
            }
        }
");

        var diagnostics = await project.ApplyAnalyzer(new ZeroIoCContainerAnalyzer());

        Assert.True(diagnostics.Any(o => o.Id == Descriptors.StatementsNotAllowed.Id));
    }

    [Fact]
    public async Task ForNotAllowed()
    {
        var project = await TestProject.Project.ApplyToProgram(@"

        public interface IService
        {

        }

        public class Service : IService
        {

        }

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                for(int i = 0; i < 10; i++)
                {
                    bootstrapper.AddTransient<IService, Service>();
                }
            }
        }
");

        var diagnostics = await project.ApplyAnalyzer(new ZeroIoCContainerAnalyzer());

        Assert.True(diagnostics.Any(o => o.Id == Descriptors.StatementsNotAllowed.Id));
    }
}