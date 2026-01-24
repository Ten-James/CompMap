using System;

namespace TenJames.CompMap.Attributes
{
/// <summary>
/// Indicates that the decorated class can be mapped from the specified source type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class MapFromAttribute (
    Type sourceType // The source type to map from.
): Attribute{
}
}
