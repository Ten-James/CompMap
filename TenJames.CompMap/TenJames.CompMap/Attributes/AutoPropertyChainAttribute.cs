using System;

namespace TenJames.CompMap.Attributes
{
/// <summary>
/// Enables automatic property chain mapping. Maps flattened properties like CategoryName to nested properties like Category.Name.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class AutoPropertyChainAttribute : Attribute
{
}
}
