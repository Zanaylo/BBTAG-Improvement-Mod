using System.Text.Json.Serialization;

namespace BBTAG_IM.Core.Updates;

internal sealed class ReleaseResponse
{
    [JsonPropertyName("tag_name")]
    public string? Tag { get; set; }

    [JsonPropertyName("html_url")]
    public string? Page { get; set; }

    [JsonPropertyName("assets")]
    public List<ReleaseAsset>? Assets { get; set; }
}

internal sealed class ReleaseAsset
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("browser_download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class TreeResponse
{
    [JsonPropertyName("truncated")]
    public bool Truncated { get; set; }

    [JsonPropertyName("tree")]
    public List<TreeEntry>? Entries { get; set; }
}

internal sealed class TreeEntry
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("sha")]
    public string? Sha { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }
}

[JsonSerializable(typeof(ReleaseResponse))]
[JsonSerializable(typeof(TreeResponse))]
internal sealed partial class GitHubJson : JsonSerializerContext;
