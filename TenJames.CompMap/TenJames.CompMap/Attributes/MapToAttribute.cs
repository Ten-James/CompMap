using System;

namespace TenJames.CompMap.Attributes
{
/// <summary>
/// Indicates that the decorated class can be mapped to the specified destination type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class MapToAttribute (
    Type destinationType // The destination type to map to.
): Attribute{
}
}
