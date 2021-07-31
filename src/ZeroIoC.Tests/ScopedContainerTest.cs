using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests
{
    [TestClass]
    public class ScopedContainerTest
    {
        [TestMethod]
        public async Task FailsWhenScopedServiceCreatedWithourScope()
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
                bootstrapper.AddScoped<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");

            var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);

            Assert.ThrowsException<ScopedWithoutScopeException>(() => container.Resolve(serviceType));
        }

        [TestMethod]
        public async Task CanResolveSimpleScoped()
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
                bootstrapper.AddScoped<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");

            var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);

            var scoped = container.CreateScope();
            var scopedFirstService = scoped.Resolve(serviceType);
            var scopedSecondService = scoped.Resolve(serviceType);

            var scoped2 = container.CreateScope();
            var scopedFirstService2 = scoped2.Resolve(serviceType);
            var scopedSecondService2 = scoped2.Resolve(serviceType);

            Assert.IsTrue(scopedFirstService != null && scopedSecondService != null && scopedFirstService.Equals(scopedSecondService));
            Assert.IsTrue(scopedFirstService2 != null && scopedSecondService2 != null && scopedFirstService2.Equals(scopedSecondService2));
            Assert.IsTrue(!scopedFirstService.Equals(scopedFirstService2));
        }

        [TestMethod]
        public async Task ServicesWithinTheScopeIsDisposed()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public interface IService : IDisposable
        {
            bool Disposed { get; set; }
        }

        public class Service : IService
        {
            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddScoped<IService, Service>();
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");
            var serviceType = assembly.GetType("TestProject.IService");

            var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);

            object service = null;
            using (var scoped = container.CreateScope())
            {
                service = scoped.Resolve(serviceType);
                var initialValue = (bool)service.ReflectionGetValue("Disposed");

                Assert.IsFalse(initialValue);
            }

            var value = (bool)service.ReflectionGetValue("Disposed");
            Assert.IsTrue(value);
        }
    }
}
