using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace TenJames.CompMap.Tests;

public class MapperGeneratorTests
{
    [Fact]
    public void AttributeGenerator_ShouldGenerateMapFromAttribute()
    {
        // Arrange
        var attributeGenerator = new AttributeGenerator();
        var driver = CSharpGeneratorDriver.Create(attributeGenerator);
        var compilation = CSharpCompilation.Create(
            nameof(AttributeGenerator_ShouldGenerateMapFromAttribute),
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
        );

        // Act
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        var generatedAttribute = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("MapFromAttribute.g.cs", System.StringComparison.Ordinal));

        Assert.NotNull(generatedAttribute);
        var generatedCode = generatedAttribute.GetText().ToString();
        Assert.Contains("public class MapFromAttribute", generatedCode);
        Assert.Contains("Type sourceType", generatedCode);
    }

    [Fact]
    public void AttributeGenerator_ShouldGenerateMapToAttribute()
    {
        // Arrange
        var attributeGenerator = new AttributeGenerator();
        var driver = CSharpGeneratorDriver.Create(attributeGenerator);
        var compilation = CSharpCompilation.Create(
            nameof(AttributeGenerator_ShouldGenerateMapToAttribute),
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
        );

        // Act
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        var generatedAttribute = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("MapToAttribute.g.cs", System.StringComparison.Ordinal));

        Assert.NotNull(generatedAttribute);
        var generatedCode = generatedAttribute.GetText().ToString();
        Assert.Contains("public class MapToAttribute", generatedCode);
        Assert.Contains("Type destinationType", generatedCode);
    }

    [Fact]
    public void AttributeGenerator_ShouldGenerateMapperInterface()
    {
        // Arrange
        var attributeGenerator = new AttributeGenerator();
        var driver = CSharpGeneratorDriver.Create(attributeGenerator);
        var compilation = CSharpCompilation.Create(
            nameof(AttributeGenerator_ShouldGenerateMapperInterface),
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }
        );

        // Act
        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // Assert
        var generatedMapper = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("Mapper.g.cs", System.StringComparison.Ordinal));

        Assert.NotNull(generatedMapper);
        var generatedCode = generatedMapper.GetText().ToString();
        Assert.Contains("public interface IMapper", generatedCode);
        Assert.Contains("public class BaseMapper : IMapper", generatedCode);
        Assert.Contains("TDestination Map<TDestination>(object source)", generatedCode);
    }

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
        var generators = new IIncrementalGenerator[] { new AttributeGenerator(), new MapperGenerator() };
        var driver = CSharpGeneratorDriver.Create(generators);

        // Act
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Assert
        // Check no errors occurred during generation
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);

        // Check that some code was generated
        Assert.True(outputCompilation.SyntaxTrees.Count() > 1, "Generator should produce additional syntax trees");
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.ICollection<>).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestCompilation",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}
