using System.Text.Json.Serialization;

namespace Rake.Core.Models;

public record Format(
    [property: JsonPropertyName("format_id")] string FormatId,
    [property: JsonPropertyName("format_note")] string FormatNote,
    [property: JsonPropertyName("ext")] string Ext, //
    [property: JsonPropertyName("protocol")] string Protocol,
    [property: JsonPropertyName("acodec")] string ACodec,
    [property: JsonPropertyName("vcodec")] string VCodec,
    [property: JsonPropertyName("url")] string Url, //
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("fps")] double Fps,
    [property: JsonPropertyName("rows")] int Rows,
    [property: JsonPropertyName("columns")] int Columns,
    [property: JsonPropertyName("fragments")] IReadOnlyList<Fragment> Fragments,
    [property: JsonPropertyName("resolution")] string Resolution,
    [property: JsonPropertyName("aspect_ratio")] double AspectRatio,
    [property: JsonPropertyName("http_headers")] HttpHeaders HttpHeaders,
    [property: JsonPropertyName("audio_ext")] string AudioExt,
    [property: JsonPropertyName("video_ext")] string VideoExt,
    [property: JsonPropertyName("vbr")] int Vbr,
    [property: JsonPropertyName("abr")] double Abr,
    [property: JsonPropertyName("format")] string FormatString,
    [property: JsonPropertyName("manifest_url")] string ManifestUrl,
    [property: JsonPropertyName("tbr")] double? Tbr,
    [property: JsonPropertyName("has_drm")] bool? HasDrm,
    [property: JsonPropertyName("dynamic_range")] string DynamicRange,
    [property: JsonPropertyName("quality")] int? Quality
);
