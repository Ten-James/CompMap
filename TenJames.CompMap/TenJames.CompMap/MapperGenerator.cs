namespace TenJames.CompMap;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Mapper generator that creates mapping methods based on attributes
/// </summary>
[Generator]
internal class MapperGenerator : IIncrementalGenerator
{

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
            predicate: (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax,
            transform: (ctx, _) => GetTypeDeclarationForSourceGen(ctx))
            .Where(t => t is not null);

        // Generate the source code.
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
        (ctx, t) => GenerateCode(ctx, t.Left, t.Right!));
    }

    private static MappingOptions? GetTypeDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

        // Go through all attributes of the type.
        foreach (var attributeSyntax in typeDeclarationSyntax.AttributeLists.SelectMany(attributeListSyntax =>
                     attributeListSyntax.Attributes))
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
            {
                continue; // if we can't get the symbol, ignore it
            }

            var attributeName = attributeSymbol.ContainingType.ToDisplayString();

            if (AttributeDefinitions.GetAllAttributes().Select(x => x.Name).Any(x => attributeName.Contains(x)))
            {
                return MappingOptions.Create(context, typeDeclarationSyntax);
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the type has AutoPropertyChain attribute
    /// </summary>
    private static bool HasAutoPropertyChain(TypeDeclarationSyntax typeDecl) => typeDecl.AttributeLists
        .SelectMany(al => al.Attributes)
        .Any(a => a.Name.ToString().Contains("AutoPropertyChain"));

    /// <summary>
    /// Recursively finds a property chain in the target that matches the source property name.
    /// For example: CategoryParentName -> Category.Parent.Name (any depth)
    /// </summary>
    private static (string PropertyChain, string TypeFullName)? FindPropertyChainRecursive(
        string flattenedName,
        INamedTypeSymbol typeSymbol,
        int maxDepth = 10)
    {
        if (maxDepth <= 0 || string.IsNullOrEmpty(flattenedName))
        {
            return null;
        }

        var properties = MappingOptions.GetAllPropertiesFromSymbol(typeSymbol);

        // First, check for exact match at this level
        var exactMatch = properties.FirstOrDefault(p =>
            string.Equals(p.Name, flattenedName, StringComparison.Ordinal));
        if (exactMatch != null)
        {
            return (exactMatch.Name, exactMatch.TypeFullName);
        }

        // Try to find a property that is a prefix of the flattened name
        // Sort by length descending to prefer longer matches first (more specific)
        var candidateProps = properties
            .Where(p => flattenedName.StartsWith(p.Name, StringComparison.Ordinal)
                        && flattenedName.Length > p.Name.Length)
            .OrderByDescending(p => p.Name.Length)
            .ToList();

        foreach (var prop in candidateProps)
        {
            var remainingName = flattenedName.Substring(prop.Name.Length);

            // Get the type of this property to search nested properties
            var propType = prop.PropertySymbol.Type as INamedTypeSymbol;
            if (propType == null)
            {
                continue;
            }

            // Recursively search in the nested type
            var nestedResult = FindPropertyChainRecursive(remainingName, propType, maxDepth - 1);
            if (nestedResult != null)
            {
                return ($"{prop.Name}.{nestedResult.Value.PropertyChain}", nestedResult.Value.TypeFullName);
            }
        }

        return null;
    }

    private static void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<MappingOptions> mappingOptions)
    {
        // generate partial classes/records with mapping methods
        foreach (var ma in mappingOptions)
        {
            var className = ma.TypeDeclarationSyntax.Identifier.Text;
            var typeKeyword = ma.IsRecord ? "record" : "class";
            var hasAutoPropertyChain = HasAutoPropertyChain(ma.TypeDeclarationSyntax);

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
            sourceText.AppendLine($"partial {typeKeyword} {className}");
            sourceText.AppendLine("{");
            sourceText.IncreaseIndent();

            // Get all properties including inherited ones
            var allSourceProperties = MappingOptions.GetAllProperties(ma.SemanticModel, ma.TypeDeclarationSyntax);

            // Get target properties from the symbol (works for both same-compilation and external assemblies)
            var allTargetProperties = MappingOptions.GetAllPropertiesFromSymbol(ma.TargetSymbol);

            var matchingFields = allSourceProperties
                .Where(prop => allTargetProperties
                    .Any(targetProp => targetProp.Name == prop.Name))
                .ToList();


            if (ma.AttributeName.Contains("MapFrom"))
            {
                var missingFields = allSourceProperties
                    .Where(prop => allTargetProperties.All(targetProp => targetProp.Name != prop.Name))
                    .ToList();

                // Track property chain mappings for auto property chain feature
                var propertyChainMappings = new Dictionary<string, (string Chain, string TypeFullName)>();

                // If AutoPropertyChain is enabled, try to find property chains for missing fields
                if (hasAutoPropertyChain)
                {
                    var stillMissingFields = new List<PropertyInfo>();
                    foreach (var prop in missingFields)
                    {
                        var chain = FindPropertyChainRecursive(prop.Name, ma.TargetSymbol);
                        if (chain != null)
                        {
                            propertyChainMappings[prop.Name] = chain.Value;
                        }
                        else
                        {
                            stillMissingFields.Add(prop);
                        }
                    }
                    missingFields = stillMissingFields;
                }

                var isMissing = missingFields.Any();
                if (isMissing)
                {
                    // create a subclass inside
                    sourceText.AppendLine();
                    sourceText.AppendLine("///<summary>");
                    sourceText.AppendLine(
                    "/// The following properties were not mapped because they do not exist in the target class");
                    sourceText.AppendLine("///</summary>");
                    {
                        using var block = sourceText.BeginBlock($"internal class {ma.TargetName}UnmappedProperties");
                        foreach (var prop in missingFields)
                        {
                            // Add property documentation
                            sourceText.AppendLine($"/// <summary>");
                            sourceText.AppendLine(
                            $"/// Property: {prop.Name} of type {prop.TypeFullName.Replace("global::", "")}");
                            sourceText.AppendLine($"/// </summary>");
                            sourceText.AppendLine(
                            $"public {prop.TypeFullName.Replace("global::", "")} {prop.Name} {{ get; set; }}");
                        }
                    }
                    sourceText.AppendLine();
                    sourceText.AppendLine(
                    $"private static partial {ma.TargetName}UnmappedProperties Get{ma.TargetName}UnmappedProperties(IMapper mapper,  {ma.TargetFullName} source);");

                }
                sourceText.AppendLine();
                {

                    sourceText.AppendLine("/// <summary>");
                    sourceText.AppendLine("/// Mapping method generated by TenJames.CompMap");
                    sourceText.AppendLine("/// </summary>");
                    using var mapFromBlock =
                        sourceText.BeginBlock(
                        $"public static {className} MapFrom(IMapper mapper, {ma.TargetFullName} source)");

                    if (isMissing)
                    {
                        sourceText.AppendLine(
                        "// Note: Some properties were not mapped due to missing counterparts in the target class.");
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
                            sourceText.AppendLine(
                            $"{prop.Name} = mapper.Map<{prop.TypeFullName.Replace("global::", "")}>(source.{prop.Name}),");
                        }
                        else
                        {
                            sourceText.AppendLine($"{prop.Name} = source.{prop.Name},");
                        }
                    }

                    // Add property chain mappings (auto-resolved from nested properties)
                    foreach (var chainMapping in propertyChainMappings)
                    {
                        var sourcePropName = chainMapping.Key;
                        var targetChain = chainMapping.Value.Chain;
                        var sourceProp = allSourceProperties.First(p => p.Name == sourcePropName);

                        if (sourceProp.TypeFullName != chainMapping.Value.TypeFullName)
                        {
                            // Type mismatch, use mapper
                            sourceText.AppendLine(
                            $"{sourcePropName} = mapper.Map<{sourceProp.TypeFullName.Replace("global::", "")}>(source.{targetChain}),");
                        }
                        else
                        {
                            sourceText.AppendLine($"{sourcePropName} = source.{targetChain},");
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

                // Track property chain mappings for auto property chain feature
                var propertyChainMappings = new Dictionary<string, (string Chain, string TypeFullName)>();

                // If AutoPropertyChain is enabled, try to find property chains for missing fields
                // For MapTo, we look for source property chains that map to flattened target properties
                if (hasAutoPropertyChain)
                {
                    var sourceSymbol = ma.SemanticModel.GetDeclaredSymbol(ma.TypeDeclarationSyntax) as INamedTypeSymbol;
                    if (sourceSymbol != null)
                    {
                        var stillMissingFields = new List<PropertyInfo>();
                        foreach (var prop in missingFields)
                        {
                            // For MapTo: target has "CategoryName", source might have "Category.Name"
                            var chain = FindPropertyChainRecursive(prop.Name, sourceSymbol);
                            if (chain != null)
                            {
                                propertyChainMappings[prop.Name] = chain.Value;
                            }
                            else
                            {
                                stillMissingFields.Add(prop);
                            }
                        }
                        missingFields = stillMissingFields;
                    }
                }

                var isMissing = missingFields.Any();
                if (isMissing)
                {
                    // create a subclass inside
                    sourceText.AppendLine();
                    sourceText.AppendLine("///<summary>");
                    sourceText.AppendLine(
                    "/// The following properties were not mapped because they do not exist in the target class");
                    sourceText.AppendLine("///</summary>");
                    {
                        using var block = sourceText.BeginBlock($"internal class {ma.TargetName}UnmappedProperties");
                        foreach (var prop in missingFields)
                        {
                            // Add property documentation
                            sourceText.AppendLine($"/// <summary>");
                            sourceText.AppendLine(
                            $"/// Property: {prop.Name} of type {prop.TypeFullName.Replace("global::", "")}");
                            sourceText.AppendLine($"/// </summary>");
                            sourceText.AppendLine(
                            $"public {prop.TypeFullName.Replace("global::", "")} {prop.Name} {{ get; set; }}");
                        }
                    }
                    sourceText.AppendLine();
                    sourceText.AppendLine(
                    $"private static partial {ma.TargetName}UnmappedProperties Get{ma.TargetName}UnmappedProperties(IMapper mapper,  {ma.ClassName} source);");

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
                        sourceText.AppendLine(
                        $" {prop.Name} = mapper.Map<{targetProp.TypeFullName.Replace("global::", "")}>(this.{prop.Name}),");
                    }
                    else
                    {
                        sourceText.AppendLine($" {prop.Name} = this.{prop.Name},");
                    }
                }

                // Add property chain mappings (auto-resolved from nested properties)
                foreach (var chainMapping in propertyChainMappings)
                {
                    var targetPropName = chainMapping.Key;
                    var sourceChain = chainMapping.Value.Chain;
                    var targetProp = allTargetProperties.First(p => p.Name == targetPropName);

                    if (targetProp.TypeFullName != chainMapping.Value.TypeFullName)
                    {
                        sourceText.AppendLine(
                        $" {targetPropName} = mapper.Map<{targetProp.TypeFullName.Replace("global::", "")}>(this.{sourceChain}),");
                    }
                    else
                    {
                        sourceText.AppendLine($" {targetPropName} = this.{sourceChain},");
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
