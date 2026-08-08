using CliWrap.Builders;

namespace Rake.Core.Twitch;

internal static class ArgumentsBuilderExtensions
{
    extension(ArgumentsBuilder builder)
    {
        public ArgumentsBuilder Add(string key, string value) => builder.Add(key).Add(value);

        public ArgumentsBuilder Add(string key, int value) => builder.Add(key).Add(value);

        public ArgumentsBuilder Add(string key, long value) => builder.Add(key).Add(value);

        public ArgumentsBuilder AddIf(bool condition, string value)
        {
            if (condition)
            {
                builder = builder.Add(value);
            }
            return builder;
        }

        public ArgumentsBuilder AddIf(bool condition, string key, string value)
        {
            if (condition)
            {
                builder = builder.Add(key).Add(value);
            }
            return builder;
        }

        public ArgumentsBuilder AddIfNotNullOrWhiteSpace(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder = builder.Add(value);
            }

            return builder;
        }

        public ArgumentsBuilder AddIfNotNullOrWhiteSpace(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder = builder.Add(key).Add(value);
            }

            return builder;
        }
    }
}
