namespace Rake.Core.Twitch;

public record TwitchMedia(
    string Title,
    long Length,
    IReadOnlyList<string> ThumbnailUrls,
    TwitchOwner Owner,
    TwitchGame Game,
    int Views,
    DateTime CreatedAt,
    IReadOnlyList<TwitchVideoStream> Streams,
    IReadOnlyList<TwitchMediaChapter> Chapters,
    string Description = ""
)
{
    /// <summary>
    /// Returns the description with duplicate newlines normalized.
    /// </summary>
    public string NormalizedDescription =>
        Description.Replace("  \n", "\n").Replace("\n\n", "\n").TrimEnd();
}
