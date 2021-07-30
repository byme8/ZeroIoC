using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        [TestMethod]
        public async Task SimpleSingleton()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public interface IService
        {

        }

        public class Service : IService
        {

        }

        public partial class TestContainer : ZeroIoCContainer<TestContainer>
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddSingleton<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");
            var container = Activator.CreateInstance(containerType);
            var firstService = container.ReflectionCall("Resolve", serviceType);
            var secondService = container.ReflectionCall("Resolve", serviceType);

            Assert.IsTrue(firstService != null && secondService != null && firstService.Equals(secondService));
        }

        [TestMethod]
        public async Task SimpleTransient()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public interface IService
        {

        }

        public class Service : IService
        {

        }

        public partial class TestContainer : ZeroIoCContainer<TestContainer>
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddTransient<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");
            var container = Activator.CreateInstance(containerType);
            var firstService = container.ReflectionCall("Resolve", serviceType);
            var secondService = container.ReflectionCall("Resolve", serviceType);

            Assert.IsTrue(firstService != null && secondService != null && !firstService.Equals(secondService));
        }

        [TestMethod]
        public async Task MultipleContainers()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public interface IService
        {

        }

        public class Service : IService
        {

        }

        public partial class TestContainer : ZeroIoCContainer<TestContainer>
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddTransient<IService, Service>();
            }
        }

        public partial class SingleContainer : ZeroIoCContainer<SingleContainer>
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddSingleton<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType1 = assembly.GetType("TestProject.TestContainer");
            var containerType2 = assembly.GetType("TestProject.SingleContainer");

            var serviceType = assembly.GetType("TestProject.IService");

            var container1 = Activator.CreateInstance(containerType1);
            var container2 = Activator.CreateInstance(containerType2);

            var firstService1 = container1.ReflectionCall("Resolve", serviceType);
            var secondService1 = container1.ReflectionCall("Resolve", serviceType);

            var firstService2 = container2.ReflectionCall("Resolve", serviceType);
            var secondService2 = container2.ReflectionCall("Resolve", serviceType);

            Assert.IsTrue(!firstService1.Equals(secondService1));
            Assert.IsTrue(!firstService1.Equals(firstService2));
            Assert.IsTrue(!firstService1.Equals(secondService2));
            Assert.IsTrue(firstService2.Equals(secondService2));
        }
    }
}
