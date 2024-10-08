using System;

namespace Rake.Generators.Attributes;

/// <summary>
/// Generates an interface for the decorated class/struct.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
internal sealed class AutoInterfaceAttribute : Attribute
{
    /// <summary>
    /// <para>The name of the generated interface.</para>
    /// <para>Default is "I{ClassName}"</para>
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// <para>The modifier(s) for the interface.</para>
    /// <para>Deault is "public partial"</para>
    /// </summary>
    public string Modifier { get; init; } = "";

    /// <summary>
    /// <para>The namespace declaration for the interface.</para>
    /// <para>If empty string, no namespace directive will be used (global namespace).<br />
    /// Default (if not present) it will be mapped to the same namespace as the namespace of the class/struct.</para>
    /// </summary>
    public string Namespace { get; init; } = "";

    /// <summary>
    /// <para>interface inheritance: Name(s) of interfaces this interface will inherit.</para>
    /// <para>Default is Array.Empty</para>
    /// </summary>
    public Type[] Inheritance { get; init; } = [];

    /// <summary>
    /// <para>
    /// The Classes, structs or interfaces containing the generated interface.<br />
    /// e.g. ["public sealed partial class Example"] will wrap the interface with that expression.
    /// </para>
    /// <para>Default is Array.Empty</para>
    /// </summary>
    public string[] Nested { get; init; } = [];

    /// <summary>
    /// <para>If enabled, static members get accepted and are generating "static abstract" members.</para>
    /// <para>Default is false</para>
    /// </summary>
    public bool StaticMembers { get; init; }
}
