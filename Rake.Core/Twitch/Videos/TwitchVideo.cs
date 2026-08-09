namespace Rake.Core.Twitch.Videos;

public record TwitchVideo(
    string Title,
    long DurationSeconds,
    IReadOnlyList<string> ThumbnailUrls,
    TwitchOwner Owner,
    TwitchGame Game,
    int Views,
    DateTime CreatedAt,
    IReadOnlyList<TwitchVideoQuality> Qualities,
    IReadOnlyList<TwitchVideoChapter> Chapters,
    string Description = ""
)
{
    /// <summary>
    /// Returns the description with duplicate newlines normalized.
    /// </summary>
    public string NormalizedDescription =>
        Description.Replace("  \n", "\n").Replace("\n\n", "\n").TrimEnd();
}
