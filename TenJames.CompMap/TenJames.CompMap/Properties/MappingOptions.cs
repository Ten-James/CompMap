using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TenJames.CompMap.Properties;

public class MappingOptions {
    public ClassDeclarationSyntax ClassDeclarationSyntax { get; set; }
    public string ClassName => ClassDeclarationSyntax.Identifier.Text;
    public string AttributeName { get; set; }
    public string Namespace { get; set; }
    public ClassDeclarationSyntax Target { get; set; }
    public string TargetName => Target.Identifier.Text;
    public string TargetNamespace { get; set; }
    public string TargetFullName => string.IsNullOrEmpty(TargetNamespace) ? TargetName : $"{TargetNamespace}.{TargetName}";
    public SemanticModel SemanticModel { get; set; }

    /// <summary>
    /// Gets all properties including inherited ones from a class declaration
    /// </summary>
    public static List<PropertyInfo> GetAllProperties(SemanticModel semanticModel, ClassDeclarationSyntax classDecl)
    {
        var properties = new List<PropertyInfo>();
        var symbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (symbol == null) return properties;

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
                var targetClass = attributeSyntax.ArgumentList?.Arguments.First().Expression switch {
                    TypeOfExpressionSyntax typeOfExpression => context.SemanticModel.GetSymbolInfo(typeOfExpression.Type).Symbol
                        ?.DeclaringSyntaxReferences.First().GetSyntax() as ClassDeclarationSyntax,
                    _ => null
                } ?? throw new InvalidOperationException("Target type could not be determined.");

                // Get target namespace
                var targetNs = targetClass.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
                var targetFileScoped = targetClass.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>();

                var targetNamespace = targetNs != null
                    ? targetNs.Name.ToString()
                    : targetFileScoped != null
                        ? targetFileScoped.Name.ToString()
                        : "GlobalNamespace";

                return new MappingOptions {
                    ClassDeclarationSyntax = classDeclarationSyntax,
                    AttributeName = attributeName,
                    Namespace = namespaceName,
                    Target = targetClass,
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