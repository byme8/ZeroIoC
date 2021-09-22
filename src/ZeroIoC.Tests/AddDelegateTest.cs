using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests
{
    [TestClass]
    public class AddDelegateTest
    {
        [TestMethod]
        public async Task AddSingletonAsDelegate()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");

            var container = (ZeroIoCContainer)Activator.CreateInstance(containerType);
            container.AddDelegate(r => new MemoryStream(), Reuse.Singleton);
            using var scope1 = container.CreateScope();

            var stream1 = container.Resolve<MemoryStream>(); 
            var stream2 = container.Resolve<MemoryStream>(); 
            var stream3 = scope1.Resolve<MemoryStream>(); 

            Assert.AreEqual(stream1, stream2);
            Assert.AreEqual(stream1, stream3);
        }
        
        [TestMethod]
        public async Task AddScopedAsDelegate()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");

            var container = (ZeroIoCContainer)Activator.CreateInstance(containerType);
            container.AddDelegate(r => new MemoryStream(), Reuse.Scoped);

            using var scope1 = container.CreateScope();
            using var scope2 = container.CreateScope();
            
            var stream1 = scope1.Resolve<MemoryStream>(); 
            var stream2 = scope1.Resolve<MemoryStream>(); 
            var stream3 = scope2.Resolve<MemoryStream>(); 

            Assert.AreEqual(stream1, stream2);
            Assert.AreNotEqual(stream1, stream3);
        }
        
        [TestMethod]
        public async Task AddTransientAsDelegate()
        {
            var project = await TestProject.Project.ApplyToProgram(@"

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
            }
        }
");

            var newProject = await project.ApplyZeroIoCGenerator();

            var assembly = await newProject.CompileToRealAssembly();
            var containerType = assembly.GetType("TestProject.TestContainer");

            var container = (ZeroIoCContainer)Activator.CreateInstance(containerType);
            container.AddDelegate(r => new MemoryStream(), Reuse.Transient);

            using var scope1 = container.CreateScope();
            using var scope2 = container.CreateScope();
            
            var stream1 = container.Resolve<MemoryStream>(); 
            var stream2 = scope1.Resolve<MemoryStream>(); 
            var stream3 = scope2.Resolve<MemoryStream>(); 

            Assert.AreNotEqual(stream1, stream2);
            Assert.AreNotEqual(stream2, stream3);
            Assert.AreNotEqual(stream1, stream3);
        }
    }
}