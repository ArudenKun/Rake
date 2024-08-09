using System;

namespace Rake.Generators.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreAttribute : Attribute;
