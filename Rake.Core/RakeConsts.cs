namespace Rake.Core;

public static class RakeConsts
{
    public const string DbTablePrefix = "App";
    public const string? DbSchema = null;

    public const bool IsDebug
#if DEBUG
    = true;
#else
    = false;
#endif
    public const string Name = "Rake";
}
