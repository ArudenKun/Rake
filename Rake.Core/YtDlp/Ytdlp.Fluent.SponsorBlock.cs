namespace Rake.Core.YtDlp;

/// <summary>
/// Fluent configuration methods for Ytdlp.
/// These methods return a new instance of Ytdlp with the specified option added, allowing for chaining multiple configuration calls in a fluent manner.
/// </summary>
public sealed partial class YtDlp
{
    // ==================================================================================================================
    // SPONSORBLOCK OPTIONS
    // ==================================================================================================================

    /// <summary>
    /// SponsorBlock categories to create chapters for, separated by commas.
    /// Available categories are sponsor, intro, outro, selfpromo, preview, filler, interaction, music_offtopic, hook, poi_highlight, chapter, all and default (=all).
    /// You can prefix the category with a "-" to exclude it. E.g. SponsorBlockMark("all,-preview)
    /// </summary>
    /// <param name="categories"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSponsorblockMark(string categories = "all") =>
        new YtDlp(this, sponsorBlockMark: categories);

    /// <summary>
    /// SponsorBlock categories to be removed from the video file, separated by commas.
    /// If a category is present in both mark and remove, remove takes precedence. Working and available categories are the same as for WithSponsorblockMark()
    /// </summary>
    /// <param name="categories"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSponsorblockRemove(string categories = "all") =>
        new YtDlp(this, sponsorBlockRemove: categories);

    /// <summary>
    /// Output template for SponsorBlock chapter titles (used with <see cref="WithSponsorblockMark(string)"/>).
    /// Available fields: start_time, end_time, category, categories, name, category_names.
    /// Defaults to "[SponsorBlock]: %(category_names)l"
    /// </summary>
    /// <param name="template"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSponsorblockChapterTitle(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
            template = "[SponsorBlock]: %(category_names)l";

        return new YtDlp(this, sponsorBlockChapterTitle: template.Trim());
    }

    /// <summary>
    /// Disable both WithSponsorblockMark() and WithSponsorblockRemove() options and do not use any sponsorblock features
    /// </summary>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithNoSponsorblock() => AddFlag("--no-sponsorblock");

    /// <summary>
    /// SponsorBlock API location, defaults to https://sponsor.ajay.app
    /// </summary>
    /// <param name="url"></param>
    /// <returns>A new <see cref="Builder.Ytdlp"/> instance.</returns>
    public YtDlp WithSponsorblockApi(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            url = "https://sponsor.ajay.app";

        return AddOption("--sponsorblock-api", url.Trim());
    }
}
