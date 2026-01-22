using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using TenJames.CompMap.Properties;


namespace TenJames.CompMap;

[Generator]
public class MapperGenerator : IIncrementalGenerator {

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
            (s, _) => s is ClassDeclarationSyntax,
            (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t is not null);

        // Generate the source code.
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
        ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
    }

    private static MappingOptions? GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // Go through all attributes of the class.
        foreach (var attributeSyntax in classDeclarationSyntax.AttributeLists.SelectMany(attributeListSyntax => attributeListSyntax.Attributes))
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue; // if we can't get the symbol, ignore it

            var attributeName = attributeSymbol.ContainingType.ToDisplayString();

            if (AttributeDefinitions.GetAllAttributes().Select(x => x.Name).Any(x => attributeName.Contains(x)))
                return MappingOptions.Create(context, classDeclarationSyntax);
        }

        return null;
    }

    private void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<MappingOptions> mappingOptions)
    {
        // generate partial classes with mapping methods
        foreach (var ma in mappingOptions)
        {
            var className = ma.ClassDeclarationSyntax.Identifier.Text;


            var sourceText = new SourceBuilder();
            sourceText.AppendLine($"using {Consts.MapperNamespace};");

            // Add using for target namespace if different from current namespace
            if (ma.TargetNamespace != ma.Namespace && ma.TargetNamespace != "GlobalNamespace")
            {
                sourceText.AppendLine($"using {ma.TargetNamespace};");
            }

            sourceText.AppendLine();
            sourceText.AppendLine($"namespace {ma.Namespace};");
            sourceText.AppendLine();
            sourceText.AppendLine($"partial class {className}");
            sourceText.AppendLine("{");
            sourceText.IncreaseIndent();

            // Get all properties including inherited ones
            var allSourceProperties = MappingOptions.GetAllProperties(ma.SemanticModel, ma.ClassDeclarationSyntax);
            var allTargetProperties = MappingOptions.GetAllProperties(ma.SemanticModel, ma.Target);

            var matchingFields = allSourceProperties
                .Where(prop => allTargetProperties
                    .Any(targetProp => targetProp.Name == prop.Name))
                .ToList();
            

            if (ma.AttributeName.Contains("MapFrom"))
            {
                var missingFields = allSourceProperties
                    .Where(prop => allTargetProperties.All(targetProp => targetProp.Name != prop.Name))
                    .ToList();

                var isMissing = missingFields.Any();
                if (isMissing)
                {
                    // create a subclass inside
                    sourceText.AppendLine();
                    sourceText.AppendLine("///<summary>");
                    sourceText.AppendLine("/// The following properties were not mapped because they do not exist in the target class");
                    sourceText.AppendLine("///</summary>");
                    {
                        using var block = sourceText.BeginBlock($"internal class {ma.TargetName}UnmappedProperties");
                        foreach (var prop in missingFields)
                        {
                            var location = prop.PropertySymbol.Locations.FirstOrDefault();
                            if (location != null && location.IsInSource)
                            {
                                var lineSpan = location.GetMappedLineSpan();
                                sourceText.AppendLine($"/// <summary>");
                                sourceText.AppendLine($"/// Found at {lineSpan.Path.Substring(lineSpan.Path.LastIndexOf('/') + 1)} at {lineSpan.StartLinePosition.Line + 1}");
                                sourceText.AppendLine($"/// </summary>");
                            }
                            sourceText.AppendLine($"public {prop.TypeFullName.Replace("global::", "")} {prop.Name} {{ get; set; }}");
                        }
                    }
                    sourceText.AppendLine();
                    sourceText.AppendLine($"private static partial {ma.TargetName}UnmappedProperties Get{ma.TargetName}UnmappedProperties(IMapper mapper,  {ma.TargetFullName} source);");

                }
                sourceText.AppendLine();
                {
                    
                    sourceText.AppendLine("/// <summary>");
                    sourceText.AppendLine("/// Mapping method generated by TenJames.CompMap");
                    sourceText.AppendLine("/// </summary>");
                    using var mapFromBlock = sourceText.BeginBlock($"public static {className} MapFrom(IMapper mapper, {ma.TargetFullName} source)");

                    if (isMissing)
                    {
                        sourceText.AppendLine("// Note: Some properties were not mapped due to missing counterparts in the target class.");
                        sourceText.AppendLine($"var unmapped = Get{ma.TargetName}UnmappedProperties(mapper, source);");
                    }
                
                    sourceText.AppendLine($"return new {className}");
                    sourceText.AppendLine("{");
                    sourceText.IncreaseIndent();
                    foreach (var prop in matchingFields)
                    {
                        var targetProp = allTargetProperties
                            .FirstOrDefault(p => p.Name == prop.Name);

                        if (targetProp != null && prop.TypeFullName != targetProp.TypeFullName)
                        {
                            // Type mismatch, use mapper
                            sourceText.AppendLine($"{prop.Name} = mapper.Map<{prop.TypeFullName.Replace("global::", "")}>(source.{prop.Name}),");
                        }
                        else
                        {
                            sourceText.AppendLine($"{prop.Name} = source.{prop.Name},");
                        }
                    }

                    foreach (var prop in missingFields)
                    {
                        sourceText.AppendLine($"{prop.Name} = unmapped.{prop.Name},");
                    }
                    sourceText.DecreaseIndent();
                    sourceText.AppendLine("};");
                    
                }
            }
            else if (ma.AttributeName.Contains("MapTo"))
            {
                var missingFields = allTargetProperties
                    .Where(prop => allSourceProperties.All(targetProp => targetProp.Name != prop.Name))
                    .ToList();

                var isMissing = missingFields.Any();
                if (isMissing)
                {
                    // create a subclass inside
                    sourceText.AppendLine();
                    sourceText.AppendLine("///<summary>");
                    sourceText.AppendLine("/// The following properties were not mapped because they do not exist in the target class");
                    sourceText.AppendLine("///</summary>");
                    {
                        using var block = sourceText.BeginBlock($"internal class {ma.TargetName}UnmappedProperties");
                        foreach (var prop in missingFields)
                        {
                            var location = prop.PropertySymbol.Locations.FirstOrDefault();
                            if (location != null && location.IsInSource)
                            {
                                var lineSpan = location.GetMappedLineSpan();
                                sourceText.AppendLine($"/// <summary>");
                                sourceText.AppendLine($"/// Found at {lineSpan.Path.Substring(lineSpan.Path.LastIndexOf('/') + 1)} at {lineSpan.StartLinePosition.Line + 1}");
                                sourceText.AppendLine($"/// </summary>");
                            }
                            sourceText.AppendLine($"public {prop.TypeFullName.Replace("global::", "")} {prop.Name} {{ get; set; }}");
                        }
                    }
                    sourceText.AppendLine();
                    sourceText.AppendLine($"private static partial {ma.TargetName}UnmappedProperties Get{ma.TargetName}UnmappedProperties(IMapper mapper,  {ma.ClassName} source);");

                }
                sourceText.AppendEmptyLine();

                sourceText.AppendLine("/// <summary>");
                sourceText.AppendLine("/// Mapping method generated by TenJames.CompMap");
                sourceText.AppendLine("/// </summary>");
                using var mapToBlock = sourceText.BeginBlock(
                $"public {ma.TargetFullName} MapTo(IMapper mapper)"
                );
                if (isMissing)
                {
                    sourceText.AppendLine("var unmapped = Get" + ma.TargetName + "UnmappedProperties(mapper, this);");
                }
                sourceText.AppendLine($"var target = new {ma.TargetFullName}() {{");
                sourceText.IncreaseIndent();
                foreach (var prop in matchingFields)
                {
                    // Get the corresponding target property to check type
                    var targetProp = allTargetProperties
                        .FirstOrDefault(p => p.Name == prop.Name);

                    if (targetProp != null && prop.TypeFullName != targetProp.TypeFullName)
                    {
                        // Type mismatch, use mapper
                        sourceText.AppendLine($" {prop.Name} = mapper.Map<{targetProp.TypeFullName.Replace("global::", "")}>(this.{prop.Name}),");
                    }
                    else
                    {
                        sourceText.AppendLine($" {prop.Name} = this.{prop.Name},");
                    }
                }
                if (isMissing)
                {
                    foreach (var prop in missingFields)
                    {
                        sourceText.AppendLine($" {prop.Name} = unmapped.{prop.Name},");
                    }
                }
                sourceText.DecreaseIndent();
                sourceText.AppendLine("};");
                sourceText.AppendLine("return target;");
            }

            sourceText.DecreaseIndent();
            sourceText.AppendLine("}");

            context.AddSource($"{className}.g.cs", SourceText.From(sourceText.ToString(), Encoding.UTF8));
        }
    }
}