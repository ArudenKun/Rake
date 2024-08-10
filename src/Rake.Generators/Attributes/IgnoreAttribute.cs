using System;

namespace Rake.Generators.Attributes;

[AttributeUsage(
    AttributeTargets.Class
        | AttributeTargets.Field
        | AttributeTargets.Property
        | AttributeTargets.Struct
)]
public sealed class IgnoreAttribute : Attribute;
