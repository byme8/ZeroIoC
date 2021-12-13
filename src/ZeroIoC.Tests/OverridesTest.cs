using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests;

[TestClass]
public class OverridesTest
{
    [TestMethod]
    public async Task ConstructorOverridesWorks()
    {
        var project = await TestProject.Project.ApplyToProgram(@"

        public class Service
        {
            public string Value { get; set; }

            public Service(string value)
            {
                this.Value = value;
            }
        }

        public partial class TestContainer : ZeroIoCContainer
        {
            protected override void Bootstrap(IZeroIoCContainerBootstrapper bootstrapper)
            {
                bootstrapper.AddTransient<Service>();
            }
        }
");

        var newProject = await project.ApplyZeroIoCGenerator();

        var assembly = await newProject.CompileToRealAssembly();
        var containerType = assembly.GetType("TestProject.TestContainer");
        var serviceType = assembly.GetType("TestProject.Service");

        var container = (ZeroIoCContainer)Activator.CreateInstance(containerType);
        
        var initialValue = "not override";
        container.AddInstance(initialValue);

        var overrides = new Overrides
        {
            Constructor =
            {
                Overrides =
                {
                    {"value", "override"} 
                } 
            }
        };
        
        var service = container.Resolve(serviceType, overrides);
        
        var value = service.ReflectionGetValue("Value");
        Assert.AreNotEqual(initialValue, value);
    }

}