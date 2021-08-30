using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests
{
    [TestClass]
    public class BasicContainerTest
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
        public async Task CanResolveSimpleSingleton()
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
                bootstrapper.AddSingleton<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");

            var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);
            var firstService = container.Resolve(serviceType);
            var secondService = container.Resolve(serviceType);

            Assert.IsTrue(firstService != null && secondService != null && firstService.Equals(secondService));
        }

        [TestMethod]
        public async Task CanResolveSimpleTransient()
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
                bootstrapper.AddTransient<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");
            var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);
            var firstService = container.Resolve(serviceType);
            var secondService = container.Resolve(serviceType);

            Assert.IsTrue(firstService != null && secondService != null && !firstService.Equals(secondService));
        }

        [TestMethod]
        public async Task HandlesMultipleContainersInTheSameTime()
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
                bootstrapper.AddTransient<IService, Service>();
            }
        }

        public partial class SingleContainer : ZeroIoCContainer
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

            var container1 = (IZeroIoCResolver)Activator.CreateInstance(containerType1);
            var container2 = (IZeroIoCResolver)Activator.CreateInstance(containerType2);

            var firstService1 = container1.Resolve(serviceType);
            var secondService1 = container1.Resolve(serviceType);

            var firstService2 = container2.Resolve(serviceType);
            var secondService2 = container2.Resolve(serviceType);

            Assert.IsTrue(!firstService1.Equals(secondService1));
            Assert.IsTrue(!firstService1.Equals(firstService2));
            Assert.IsTrue(!firstService1.Equals(secondService2));
            Assert.IsTrue(firstService2.Equals(secondService2));
        }

        [TestMethod]
        public async Task SingletonsAreTheSameBetweenScopes()
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
                bootstrapper.AddSingleton<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");
            var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);
            var firstService = container.Resolve(serviceType);

            var scoped = container.CreateScope();
            var secondService = scoped.Resolve(serviceType);

            Assert.IsTrue(firstService != null && secondService != null && firstService.Equals(secondService));
        }

        [TestMethod]
        public async Task RegisterOnlyImplementation()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public class Service
        {
        }

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddSingleton<Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.Service");

            var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);
            var service = container.Resolve(serviceType);

            Assert.IsNotNull(service);
        }

        [TestMethod]
        public async Task TypedOnlyAPartOfServiceName()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public class Service
        {
        }

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddSingleton<Servi>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

        }

        [TestMethod]
        public async Task BootstrapMethodIsMissing()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public class Service
        {
        }

        public partial class TestContainer : ZeroIoCContainer
        {
           
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

        }
    }
}