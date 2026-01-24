namespace TenJames.CompMap;

using System.Collections.Generic;

/// <summary>
/// Information about a mapping attribute.
/// </summary>
internal class AttributeDefinition
{
    /// <summary>
    /// Name of the attribute.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the attribute.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Arguments for the attribute.
    /// </summary>
    public IList<ArgumentDefinition> Arguments { get; set; }
}

/// <summary>
/// Information about an argument for a mapping attribute.
/// </summary>
internal class ArgumentDefinition
{
    /// <summary>
    /// Name of the argument.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of the argument.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Default value or description of the argument.
    /// </summary>
    public string Value { get; set; }
}

/// <summary>
/// Static class containing predefined attribute definitions.
/// </summary>
internal static class AttributeDefinitions
{
    private static readonly AttributeDefinition MapFrom = new()
    {
        Name = "MapFrom",
        Description = "Indicates that the decorated class can be mapped from the specified source type.",
        Arguments = new List<ArgumentDefinition>
        {
            new()
            {
                Name = "sourceType",
                Type = "Type",
                Value = "The source type to map from."
            }
        }
    };


    private static readonly AttributeDefinition MapTo = new()
    {
        Name = "MapTo",
        Description = "Indicates that the decorated class can be mapped to the specified destination type.",
        Arguments = new List<ArgumentDefinition>
        {
            new()
            {
                Name = "destinationType",
                Type = "Type",
                Value = "The destination type to map to."
            }
        }
    };

    private static readonly AttributeDefinition AutoPropertyChain = new()
    {
        Name = "AutoPropertyChain",
        Description =
            "Enables automatic property chain mapping. Maps flattened properties like CategoryName to nested properties like Category.Name.",
        Arguments = new List<ArgumentDefinition>()
    };

    /// <summary>
    /// Retrieves all mapping attributes.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<AttributeDefinition> GetAllAttributes()
    {
        yield return MapFrom;
        yield return MapTo;
    }

    /// <summary>
    /// Retrieves all modifier attributes.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<AttributeDefinition> GetAllModifierAttributes()
    {
        yield return AutoPropertyChain;
    }
}
