using RhoMicro.CodeAnalysis;

namespace Rake.SourceGenerators.Attributes;

[GenerateFactory]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed partial class ViewModelLocatorAttribute : Attribute
{
    public string ViewModelSuffix { get; set; } = "ViewModel";
    public Type[] BaseTypes { get; set; } = [];
}
