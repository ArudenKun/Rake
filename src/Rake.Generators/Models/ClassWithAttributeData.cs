using System;

namespace Rake.Generators.Models;

/// <summary>
/// Container for 2 Nodes: The target class and target attribute
/// </summary>
/// <param name="type"></param>
/// <param name="attributeData"></param>
internal readonly struct ClassWithAttributeData(
    TypeDeclarationSyntax type,
    INamedTypeSymbol typeSymbol,
    AttributeData attributeData
) : IEquatable<ClassWithAttributeData>
{
    public TypeDeclarationSyntax Type { get; } = type;
    public INamedTypeSymbol TypeSymbol { get; } = typeSymbol;
    public AttributeData AttributeData { get; } = attributeData;

    public static bool operator ==(ClassWithAttributeData left, ClassWithAttributeData right) =>
        left.Equals(right);

    public static bool operator !=(ClassWithAttributeData left, ClassWithAttributeData right) =>
        !(left == right);

    public override bool Equals(object? obj) =>
        obj switch
        {
            ClassWithAttributeData classWithAttributeData => Equals(classWithAttributeData),
            _ => false
        };

    public bool Equals(ClassWithAttributeData other) => Type == other.Type;

    public override int GetHashCode() => Type.GetHashCode();
}
