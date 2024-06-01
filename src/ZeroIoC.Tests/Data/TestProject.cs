﻿using System;
using System.Buffers;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ZeroIoC.Tests.Data;

public static class TestProject
{
    public const string ProgramCs = @"
using System;
using ZeroIoC;

namespace TestProject 
{
    // place to replace

    class Program
    {
        static void Main(string[] args)
        {

        }
    }   
}
";

    static TestProject()
    {
        var workspace = new AdhocWorkspace();
        Project = workspace
            .AddProject("TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(GetReferences())
            .AddDocument("Program.cs", ProgramCs).Project;
    }

    public static Project Project { get; }

    private static MetadataReference[] GetReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        return new MetadataReference[]
        {
            MetadataReference.CreateFromFile(assemblies.Single(a => a.GetName().Name == "netstandard").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Buffers").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ArrayPool<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ZeroIoCContainer).Assembly.Location),
        };
    }
}