﻿using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroIoC.Tests.Data;
using ZeroIoC.Tests.Utils;

namespace ZeroIoC.Tests
{
    [TestClass]
    public class StatetmentsAnalyzerTest
    {
        [TestMethod]
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

            Assert.IsTrue(diagnostics.Any(o => o.Id == Descriptors.StatementsNotAllowed.Id));
        }
    }
}