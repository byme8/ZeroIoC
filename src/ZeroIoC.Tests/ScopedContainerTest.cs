using System;
using System.Threading.Tasks;
using Xunit;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests;

public class ScopedContainerTest
{
    [Fact]
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

        Assert.Throws<ScopedWithoutScopeException>(() => container.Resolve(serviceType));
    }

    [Fact]
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

        Assert.True(scopedFirstService != null && scopedSecondService != null && scopedFirstService.Equals(scopedSecondService));
        Assert.True(scopedFirstService2 != null && scopedSecondService2 != null && scopedFirstService2.Equals(scopedSecondService2));
        Assert.True(!scopedFirstService.Equals(scopedFirstService2));
    }

    [Fact]
    public async Task ServicesWithinTheScopeIsDisposed()
    {
        var project = await TestProject.Project.ApplyToProgram(@"

        public class SingletonService : IDisposable
        {
            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        public class Service : IDisposable
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
                bootstrapper.AddScoped<Service>();
                bootstrapper.AddSingleton<SingletonService>();
            }
        }
");

        var newProject = await project.ApplyZeroIoCGenerator();

        var assembly = await newProject.CompileToRealAssembly();
        var containerType = assembly.GetType("TestProject.TestContainer");
        var serviceType = assembly.GetType("TestProject.Service");
        var singletonServiceType = assembly.GetType("TestProject.SingletonService");

        var container = (IZeroIoCResolver)Activator.CreateInstance(containerType);

        object service = null;
        object singletonService = null;
        using (var scoped = container.CreateScope())
        {
            service = scoped.Resolve(serviceType);
            Assert.False((bool)service.ReflectionGetValue("Disposed"));

            singletonService = scoped.Resolve(singletonServiceType);
            Assert.False((bool)service.ReflectionGetValue("Disposed"));
        }

        Assert.True((bool)service.ReflectionGetValue("Disposed"));
        Assert.False((bool)singletonService.ReflectionGetValue("Disposed"));
    }
}