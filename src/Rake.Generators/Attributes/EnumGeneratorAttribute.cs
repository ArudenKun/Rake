using System;

namespace Rake.Generators.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class EnumGeneratorAttribute(Type enumType) : Attribute
{
    public Type EnumType { get; } = enumType;
}
