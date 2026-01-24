namespace TenJames.CompMap;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class MappingOptions {
    public ClassDeclarationSyntax ClassDeclarationSyntax { get; set; }
    public string ClassName => ClassDeclarationSyntax.Identifier.Text;
    public string AttributeName { get; set; }
    public string Namespace { get; set; }

    // Target can be either from source (same compilation) or from metadata (external assembly)
    public ClassDeclarationSyntax? TargetSyntax { get; set; }
    public INamedTypeSymbol TargetSymbol { get; set; }

    public string TargetName => TargetSymbol.Name;
    public string TargetNamespace { get; set; }
    public string TargetFullName => string.IsNullOrEmpty(TargetNamespace) || TargetNamespace == "GlobalNamespace"
        ? TargetName
        : $"{TargetNamespace}.{TargetName}";
    public SemanticModel SemanticModel { get; set; }

    /// <summary>
    /// Gets all properties including inherited ones from a class declaration (when source is available)
    /// </summary>
    public static List<PropertyInfo> GetAllProperties(SemanticModel semanticModel, ClassDeclarationSyntax classDecl)
    {
        var symbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
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
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var ns = classDeclarationSyntax.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
        var fileScoped = classDeclarationSyntax.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>();

        var namespaceName = ns != null
            ? ns.Name.ToString()
            : fileScoped != null
                ? fileScoped.Name.ToString()
                : "GlobalNamespace";


        foreach (var attributeSyntax in classDeclarationSyntax.AttributeLists.SelectMany(attributeListSyntax => attributeListSyntax.Attributes))
        {
            var attributeName = attributeSyntax.Name.ToString();
            if (AttributeDefinitions.GetAllAttributes().Select(x => x.Name).Any(x => attributeName.Contains(x)))
            {
                // Get the target type symbol
                INamedTypeSymbol? targetSymbol = null;
                ClassDeclarationSyntax? targetSyntax = null;

                if (attributeSyntax.ArgumentList?.Arguments.First().Expression is TypeOfExpressionSyntax typeOfExpression)
                {
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(typeOfExpression.Type);
                    targetSymbol = symbolInfo.Symbol as INamedTypeSymbol;

                    // Try to get syntax if available (same compilation)
                    if (targetSymbol != null)
                    {
                        var syntaxRef = targetSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        targetSyntax = syntaxRef?.GetSyntax() as ClassDeclarationSyntax;
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

                return new MappingOptions {
                    ClassDeclarationSyntax = classDeclarationSyntax,
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
