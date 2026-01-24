namespace TenJames.CompMap.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public class MapperGeneratorTests
{

    [Fact]
    public void MapperGenerator_ShouldRunWithoutErrors()
    {
        // Arrange
        var sourceCode = @"
using TenJames.CompMap.Attributes;

namespace TestNamespace
{
    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [MapFrom(typeof(Source))]
    public partial class Target
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";

        var compilation = CreateCompilation(sourceCode);
        var generators = new IIncrementalGenerator[] { new MapperGenerator() };
        var driver = CSharpGeneratorDriver.Create(generators);

        // Act
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation,
        out var outputCompilation,
        out var diagnostics);

        // Assert
        // Check no errors occurred during generation
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);

        // Check that some code was generated
        Assert.True(outputCompilation.SyntaxTrees.Any(), "Generator should produce additional syntax trees");
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ICollection<>).Assembly.Location)
        };

        return CSharpCompilation.Create(
        "TestCompilation",
        new[] { syntaxTree },
        references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}
