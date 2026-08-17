using Rake.SourceGenerators.Core.Internals;

namespace Rake.SourceGenerators.Core;

public sealed class SwitchCaseBuilder : IBuilder
{
    internal SwitchBuilder Parent { get; }
    internal string Case { get; }
    private Action<ICodeWriter>? _content;
    private string? _returnValue;
    private bool _useBraces;

    public SwitchCaseBuilder(SwitchBuilder parent, string @case)
    {
        Parent = parent;
        Case = @case;
    }

    /// <summary>
    /// Configures whether this case block should be enclosed within curly braces.
    /// </summary>
    public SwitchCaseBuilder WithBraces(bool useBraces = true)
    {
        _useBraces = useBraces;
        return this;
    }

    /// <summary>
    /// Sets a code block content delegate for the case statement arm.
    /// </summary>
    public SwitchBuilder WithContent(Action<ICodeWriter>? contentDelegate)
    {
        _content = contentDelegate;
        return Parent;
    }

    /// <summary>
    /// Configures the case as a switch expression arm (e.g. "case => value,").
    /// </summary>
    public SwitchBuilder WithExpression(string returnValue = "null")
    {
        _content = w => w.AppendLine($"{Case} => {returnValue},");
        return Parent;
    }

    /// <summary>
    /// Configures a return value for statement-based cases (emits "return value;").
    /// </summary>
    public SwitchBuilder WithReturnValue(string returnValue)
    {
        _returnValue = returnValue;
        return Parent;
    }

    void IBuilder.Write(in CodeWriter writer)
    {
        if (Parent.Expression)
        {
            _content?.Invoke(writer);
            return;
        }

        // Handle 'default' label versus 'case <value>:'
        var label = Case.Equals("default", StringComparison.OrdinalIgnoreCase)
            ? "default:"
            : $"case {Case}:";
        writer.AppendLine(label);

        if (_useBraces)
        {
            writer.AppendLine("{");
            writer.IncreaseIndent();
        }

        writer.IncreaseIndent();
        _content?.Invoke(writer);

        // Emit 'return' or 'break' control flow
        if (!string.IsNullOrWhiteSpace(_returnValue))
        {
            writer.AppendLine($"return {_returnValue};");
        }
        else if (_content is null)
        {
            writer.AppendLine("break;");
        }

        writer.DecreaseIndent();

        if (_useBraces)
        {
            writer.DecreaseIndent();
            writer.AppendLine("}");
        }
    }
}
