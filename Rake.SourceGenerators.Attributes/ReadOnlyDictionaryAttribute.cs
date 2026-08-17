namespace Rake.SourceGenerators.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ReadOnlyDictionaryAttribute : Attribute
{
    public bool IncludeJsonPropertyNameAttribute { get; set; } = false;
    public bool IncludeUnderscore { get; set; } = false;
    public bool IncludePascalize { get; set; } = false;
    public bool IncludeCamelize { get; set; } = false;
    public bool IncludeKebaberize { get; set; } = false;
    public bool IncludeUpper { get; set; } = false;
    public bool IncludeLower { get; set; } = false;
}
