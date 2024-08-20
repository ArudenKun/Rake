using System.Text.Json.Serialization;

namespace Rake.Core.Models;

public record RequestedDownload(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("format_id")] string FormatId,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("filesize_approx")] int FilesizeApprox,
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("resolution")] string Resolution,
    [property: JsonPropertyName("dynamic_range")] string DynamicRange,
    [property: JsonPropertyName("aspect_ratio")] double AspectRatio,
    [property: JsonPropertyName("http_headers")] HttpHeaders HttpHeaders,
    [property: JsonPropertyName("video_ext")] string VideoExt,
    [property: JsonPropertyName("audio_ext")] string AudioExt,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("__write_download_archive")] bool WriteDownloadArchive
);
