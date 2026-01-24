namespace TenJames.CompMap;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MappingOptions
{
    public TypeDeclarationSyntax TypeDeclarationSyntax { get; set; }

    public string ClassName => TypeDeclarationSyntax.Identifier.Text;

    public bool IsRecord => TypeDeclarationSyntax is RecordDeclarationSyntax;

    public string AttributeName { get; set; }

    public string Namespace { get; set; }

    // Target can be either from source (same compilation) or from metadata (external assembly)
    public TypeDeclarationSyntax? TargetSyntax { get; set; }

    public INamedTypeSymbol TargetSymbol { get; set; }

    public string TargetName => TargetSymbol.Name;

    public string TargetNamespace { get; set; }

    public string TargetFullName => string.IsNullOrEmpty(TargetNamespace) || TargetNamespace == "GlobalNamespace"
        ? TargetName
        : $"{TargetNamespace}.{TargetName}";

    public SemanticModel SemanticModel { get; set; }

    /// <summary>
    /// Gets all properties including inherited ones from a type declaration (when source is available)
    /// </summary>
    public static List<PropertyInfo> GetAllProperties(SemanticModel semanticModel, TypeDeclarationSyntax typeDecl)
    {
        var symbol = semanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
        return symbol == null ? new List<PropertyInfo>() : GetAllPropertiesFromSymbol(symbol);
    }

    /// <summary>
    /// Gets all properties including inherited ones from a type symbol (works for external assemblies)
    /// </summary>
    public static List<PropertyInfo> GetAllPropertiesFromSymbol(INamedTypeSymbol symbol)
    {
        var properties = new List<PropertyInfo>();

        // Walk up the inheritance chain to get all properties
        var currentType = symbol;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            // Get properties declared on this type
            var typeProperties = currentType.GetMembers().OfType<IPropertySymbol>();

            foreach (var prop in typeProperties)
            {
                // Skip EqualityContract property which is compiler-generated for records
                if (prop.Name == "EqualityContract")
                {
                    continue;
                }

                // Avoid duplicates (overridden properties)
                if (!properties.Any(p => p.Name == prop.Name))
                {
                    properties.Add(new PropertyInfo
                    {
                        Name = prop.Name,
                        TypeFullName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        PropertySymbol = prop
                    });
                }
            }

            // Move to base type
            currentType = currentType.BaseType;
        }

        return properties;
    }

    public static MappingOptions? Create(
        GeneratorSyntaxContext context,
        TypeDeclarationSyntax typeDeclarationSyntax)
    {
        var ns = typeDeclarationSyntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
        var fileScoped = typeDeclarationSyntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>();

        var namespaceName = ns != null
            ? ns.Name.ToString()
            : fileScoped != null
                ? fileScoped.Name.ToString()
                : "GlobalNamespace";


        foreach (var attributeSyntax in typeDeclarationSyntax.AttributeLists.SelectMany(attributeListSyntax =>
                     attributeListSyntax.Attributes))
        {
            var attributeName = attributeSyntax.Name.ToString();
            if (AttributeDefinitions.GetAllAttributes().Select(x => x.Name).Any(x => attributeName.Contains(x)))
            {
                // Get the target type symbol
                INamedTypeSymbol? targetSymbol = null;
                TypeDeclarationSyntax? targetSyntax = null;

                if (attributeSyntax.ArgumentList?.Arguments.First().Expression is TypeOfExpressionSyntax
                    typeOfExpression)
                {
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(typeOfExpression.Type);
                    targetSymbol = symbolInfo.Symbol as INamedTypeSymbol;

                    // Try to get syntax if available (same compilation)
                    if (targetSymbol != null)
                    {
                        var syntaxRef = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        targetSyntax = syntaxRef?.GetSyntax() as TypeDeclarationSyntax;
                    }
                }

                if (targetSymbol == null)
                {
                    throw new InvalidOperationException("Target type could not be determined.");
                }

                // Get target namespace from the symbol
                var targetNamespace = targetSymbol.ContainingNamespace.IsGlobalNamespace
                    ? "GlobalNamespace"
                    : targetSymbol.ContainingNamespace.ToDisplayString();

                return new MappingOptions
                {
                    TypeDeclarationSyntax = typeDeclarationSyntax,
                    AttributeName = attributeName,
                    Namespace = namespaceName,
                    TargetSyntax = targetSyntax,
                    TargetSymbol = targetSymbol,
                    TargetNamespace = targetNamespace,
                    SemanticModel = context.SemanticModel
                };
            }
        }

        return null;
    }
}

public class PropertyInfo
{
    public string Name { get; set; } = string.Empty;

    public string TypeFullName { get; set; } = string.Empty;

    public IPropertySymbol PropertySymbol { get; set; } = null!;
}
