using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Rake.SourceGenerators.Extensions;

internal static class StringExtensions
{
    private const string SplitRegexStr =
        @"[_\W]+|(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=\d)(?=[A-Za-z])";
    private static readonly Regex SplitRegex = new(
        SplitRegexStr,
        RegexOptions.Compiled | RegexOptions.ExplicitCapture
    );

    private enum Case
    {
        Title,
        Camel,
        Pascal,
        Underscore,
        Kebab,
        Lower,
    }

    public static string Titlelize(this string source) => source.SafeName(Case.Title);

    // Humanizer-style extension methods
    public static string Underscore(this string source) => source.SafeName(Case.Underscore);

    public static string Pascalize(this string source) => source.SafeName(Case.Pascal);

    public static string Camelize(this string source) => source.SafeName(Case.Camel);

    public static string Kebaberize(this string source) => source.SafeName(Case.Kebab);

    public static string Lower(this string source) => source.SafeName(Case.Lower);

    private static string SafeName(this string source, Case @case)
    {
        if (string.IsNullOrEmpty(source))
            return source;

        return @case switch
        {
            Case.Title => SafeCase(" ", UppercaseFirstChar),
            Case.Camel => SafeCase("", LowercaseFirstWord),
            Case.Pascal => SafeCase("", UppercaseFirstChar),
            Case.Underscore => SafeCase("_", LowercaseAllChars),
            Case.Kebab => SafeCase("-", LowercaseAllChars),
            Case.Lower => source.ToLowerInvariant(),
            _ => throw new NotImplementedException($"Unknown {nameof(Case)}: {@case}"),
        };

        string SafeCase(string spacer, Func<string, int, char> firstChar)
        {
            var parts = SplitRegex
                .Split(source)
                .Where(x => x is not "")
                .Select((x, i) => firstChar(x, i) + OtherChars(x));
            var result = string.Join(spacer, parts);
            return char.IsDigit(source[0]) ? $"_{result}" : result;

            static string OtherChars(string x) => x[1..].ToLowerInvariant();
        }

        static char UppercaseFirstChar(string x, int _ = 0) => char.ToUpperInvariant(x[0]);

        static char LowercaseAllChars(string x, int _ = 0) => char.ToLowerInvariant(x[0]);

        // ReSharper disable once UnusedParameter.Local
        static char LowercaseFirstChar(string x, int _ = 0) => char.ToLowerInvariant(x[0]);

        static char LowercaseFirstWord(string x, int i) =>
            i is 0 ? LowercaseFirstChar(x) : UppercaseFirstChar(x);
    }

    public static string AddPrefix(this string source, string prefix) =>
        source.StartsWith(prefix, StringComparison.Ordinal) ? source : $"{prefix}{source}";

    public static string AddSuffix(this string source, string suffix) =>
        source.EndsWith(suffix, StringComparison.Ordinal) ? source : $"{source}{suffix}";

    public static string TrimPrefix(this string source, string prefix) =>
        source.StartsWith(prefix, StringComparison.Ordinal) ? source[prefix.Length..] : source;

    public static string TrimSuffix(this string source, string suffix) =>
        source.EndsWith(suffix, StringComparison.Ordinal) ? source[..^suffix.Length] : source;

    public static string Truncate(this string source, int maxChars) =>
        source.Length <= maxChars ? source : source[..maxChars];

    public static string? NullIfEmpty(this string source) => source is "" ? null : source;

    public static string Format(
        this object[] source,
        Func<string, string> format,
        string sep = ", "
    ) => source.Format(sep, format);

    public static string Format(
        this object[] source,
        string sep = ", ",
        Func<string, string>? format = null
    )
    {
        var x = string.Join(sep, source);
        return format?.Invoke(x) ?? x;
    }

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    public static T[]? NullIfEmpty<T>(this IEnumerable<T>? source) =>
        source?.Any() is null or false ? null : [.. source];

    public static string Join(this IEnumerable<object> source, string sep) =>
        string.Join(sep, source);

    /// <summary>
    /// Returns the original string if it is not null or white space; otherwise, returns the fallback value.
    /// </summary>
    public static string IfNullOrWhiteSpace(this string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value ?? string.Empty;
    }
}
