using Rake.SourceGenerators.Builder.Internals;

namespace Rake.SourceGenerators.Builder;

public sealed class SwitchBuilder
{
    internal ICodeWriter Writer { get; }
    internal string SwitchOn { get; }
    internal bool Expression { get; }
    private readonly List<SwitchCaseBuilder> _switchCases = [];

    internal SwitchBuilder(ICodeWriter writer, string switchOn, bool expression)
    {
        Writer = writer;
        SwitchOn = switchOn;
        Expression = expression;
    }

    /// <summary>
    /// Adds a new case arm to the switch block. Pass <c>"default"</c> or <c>"_"</c> to construct a fallback label.
    /// </summary>
    /// <param name="case">
    /// The target match expression or pattern (e.g., <c>"5"</c>, <c>"string s"</c>).
    /// Use <c>"default"</c> (for classic switches) or <c>"_"</c> (for switch expressions) as a fallback case.
    /// </param>
    /// <returns>The <see cref="SwitchCaseBuilder"/> instance to fluently configure content, return values, or block scoping.</returns>
    public SwitchCaseBuilder AddCase(string @case)
    {
        var builder = new SwitchCaseBuilder(this, @case);
        _switchCases.Add(builder);
        return builder;
    }

    /// <summary>
    /// Adds a default fallback case arm (<c>"default"</c> for classic switches, or <c>"_"</c> for switch expressions).
    /// </summary>
    public SwitchCaseBuilder AddDefault() => AddCase(Expression ? "_" : "default");

    public ICodeWriter Close()
    {
        if (Writer is CodeWriter codeWriter)
        {
            if (Expression)
            {
                WriteExpressionSwitchCase(codeWriter);
            }
            else
            {
                WriteClassicSwitchCase(codeWriter);
            }
        }

        return Writer;
    }

    private void WriteClassicSwitchCase(CodeWriter writer)
    {
        using (writer.Block($"switch ({SwitchOn})"))
        {
            foreach (IBuilder @case in _switchCases)
            {
                @case.Write(writer);
            }
        }
    }

    private void WriteExpressionSwitchCase(CodeWriter writer)
    {
        writer.AppendLine($"{SwitchOn} switch");
        writer.AppendLine("{");
        writer.IncreaseIndent();

        foreach (IBuilder @case in _switchCases)
        {
            @case.Write(writer);
        }

        writer.DecreaseIndent();
        writer.AppendLine("};");
    }
}
