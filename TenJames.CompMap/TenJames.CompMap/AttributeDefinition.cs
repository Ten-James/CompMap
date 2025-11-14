using System;
using System.Collections.Generic;

namespace TenJames.CompMap;

public class AttributeDefinition {
    public string Name { get; set; }
    public string Description { get; set; }
    public IList<ArgumentDefinition> Arguments { get; set; }
}
public class ArgumentDefinition {
    public string Name { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
}

public static class AttributeDefinitions {
    public readonly static AttributeDefinition MapFrom = new AttributeDefinition {
        Name = "MapFrom",
        Description = "Indicates that the decorated class can be mapped from the specified source type.",
        Arguments = new List<ArgumentDefinition> {
            new ArgumentDefinition {
                Name = "sourceType",
                Type = "Type",
                Value = "The source type to map from."
            }
        }
    };

    public readonly static AttributeDefinition MapTo = new AttributeDefinition {
        Name = "MapTo",
        Description = "Indicates that the decorated class can be mapped to the specified destination type.",
        Arguments = new List<ArgumentDefinition> {
            new ArgumentDefinition {
                Name = "destinationType",
                Type = "Type",
                Value = "The destination type to map to."
            }
        }
    };

    public static IEnumerable<AttributeDefinition> GetAllAttributes()
    {
        yield return MapFrom;
        yield return MapTo;
    }
}