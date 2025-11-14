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
            sourceText.AppendLine();
            sourceText.AppendLine($"namespace {ma.Namespace};");
            sourceText.AppendLine();
            sourceText.AppendLine($"partial class {className}");
            sourceText.AppendLine("{");
            sourceText.IncreaseIndent();
            var matchingFields = ma.ClassDeclarationSyntax.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => ma.Target != null
                               && ma.Target.Members
                                   .OfType<PropertyDeclarationSyntax>()
                                   .Any(targetProp => targetProp.Identifier.Text == prop.Identifier.Text))
                .ToList();
            
            
            if (ma.AttributeName.Contains("MapFrom"))
            {
                var missingFields = ma.ClassDeclarationSyntax.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(prop => ma.Target != null
                                   && ma.Target.Members.OfType<PropertyDeclarationSyntax>().All(targetProp => targetProp.Identifier.Text != prop.Identifier.Text))
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
                            var location = prop.GetLocation().GetMappedLineSpan();
                            sourceText.AppendLine($"/// <summary>");
                            sourceText.AppendLine($"/// Found at {location.Path.Substring(location.Path.LastIndexOf('/'))} at {location.StartLinePosition.Line + 1}");
                            sourceText.AppendLine($"/// </summary>");
                            sourceText.AppendLine($"public {
                                string.Join("",prop.Modifiers.Where(x => !x.ToFullString().Contains("public")).Select(x => x.ToFullString()))
                            }{prop.Type.ToFullString().Trim()} {prop.Identifier.Text} {{ get; set; }}");
                        }
                    }
                    sourceText.AppendLine();
                    sourceText.AppendLine($"private static partial {ma.TargetName}UnmappedProperties Get{ma.TargetName}UnmappedProperties(IMapper mapper,  {ma.Target.Identifier.Text} source);");

                }
                sourceText.AppendLine();
                {
                    
                    sourceText.AppendLine("/// <summary>");
                    sourceText.AppendLine("/// Mapping method generated by TenJames.CompMap");
                    sourceText.AppendLine("/// </summary>");
                    using var mapFromBlock = sourceText.BeginBlock($"public static {className} MapFrom(IMapper mapper, {ma.Target?.Identifier.Text} source)");

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
                        if (prop.Type.ToFullString() != ma.Target?.Members
                                .OfType<PropertyDeclarationSyntax>()
                                .FirstOrDefault(p => p.Identifier.Text == prop.Identifier.Text)?
                                .Type.ToFullString())
                        {
                            // Type mismatch, use mapper
                            sourceText.AppendLine($"{prop.Identifier.Text} = mapper.Map<{prop.Type.ToFullString()}>(source.{prop.Identifier.Text}),");
                        }
                        else
                        {
                            sourceText.AppendLine($"{prop.Identifier.Text} = source.{prop.Identifier.Text},");
                        }
                    }
                
                    foreach (var prop in missingFields)
                    {
                        sourceText.AppendLine($"{prop.Identifier.Text} = unmapped.{prop.Identifier.Text},");
                    }
                    sourceText.DecreaseIndent();
                    sourceText.AppendLine("};");
                    
                }
            }
            else if (ma.AttributeName.Contains("MapTo"))
            {
                var missingFields = ma.Target.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(prop => ma.ClassDeclarationSyntax.Members.OfType<PropertyDeclarationSyntax>().All(targetProp => targetProp.Identifier.Text != prop.Identifier.Text))
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
                            var location = prop.GetLocation().GetMappedLineSpan();
                            sourceText.AppendLine($"/// <summary>");
                            sourceText.AppendLine($"/// Found at {location.Path.Substring(location.Path.LastIndexOf('/'))} at {location.StartLinePosition.Line + 1}");
                            sourceText.AppendLine($"/// </summary>");
                            sourceText.AppendLine($"public {
                                string.Join("",prop.Modifiers.Where(x => !x.ToFullString().Contains("public")).Select(x => x.ToFullString()))
                            }{prop.Type.ToFullString().Trim()} {prop.Identifier.Text} {{ get; set; }}");
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
                $"public {ma.TargetName} MapTo(IMapper mapper)"
                );
                if (isMissing)
                {
                    sourceText.AppendLine("var unmapped = Get" + ma.TargetName + "UnmappedProperties(mapper, this);");
                }
                sourceText.AppendLine($"var target = new {ma.TargetName}() {{");
                sourceText.IncreaseIndent();
                foreach (var prop in matchingFields)
                {
                    sourceText.AppendLine($" {prop.Identifier.Text} = this.{prop.Identifier.Text},");
                }
                if (isMissing)
                {
                    foreach (var prop in missingFields)
                    {
                        sourceText.AppendLine($" {prop.Identifier.Text} = unmapped.{prop.Identifier.Text},");
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