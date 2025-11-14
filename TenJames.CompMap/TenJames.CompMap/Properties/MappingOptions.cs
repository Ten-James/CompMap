using System;
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
                return new MappingOptions {
                    ClassDeclarationSyntax = classDeclarationSyntax,
                    AttributeName = attributeName,
                    Namespace = namespaceName,
                    Target = attributeSyntax.ArgumentList?.Arguments.First().Expression switch {
                        TypeOfExpressionSyntax typeOfExpression => context.SemanticModel.GetSymbolInfo(typeOfExpression.Type).Symbol
                            ?.DeclaringSyntaxReferences.First().GetSyntax() as ClassDeclarationSyntax,
                        _ => null
                    } ?? throw new InvalidOperationException("Target type could not be determined.")
                };
            }
        }

        return null;
    }
}